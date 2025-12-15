using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bitlet.Infrastructure.Messaging.Attributes;
using Bitlet.Infrastructure.Messaging.Configurations;
using Bitlet.Infrastructure.Messaging.Models;
using Bitlet.Infrastructure.Messaging.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace Bitlet.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQMessageSubscriber : IMessageSubscriber
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly MessageTypeRegistry _messageTypeRegistry;
    private readonly SubscriberConfiguration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private string? _consumerTag;

    public RabbitMQMessageSubscriber(IServiceScopeFactory serviceScopeFactory, SubscriberConfiguration configuration)
        : this(serviceScopeFactory, configuration, new MessageTypeRegistry())
    { }

    public RabbitMQMessageSubscriber(
        IServiceScopeFactory serviceScopeFactory,
        SubscriberConfiguration configuration,
        MessageTypeRegistry messageTypeRegistry)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _messageTypeRegistry = messageTypeRegistry;
        _configuration = configuration;
        _connection = null;
        _channel = null;
        _consumer = null;
        _consumerTag = null;

        var logMessage = new StringBuilder();
        logMessage.AppendLine("Create RabbitMQMessageHandler instance using config:");
        logMessage.AppendLine($" - Host: {_configuration.Host}");
        logMessage.AppendLine($" - VirtualHost: {_configuration.VirtualHost}");
        logMessage.AppendLine($" - Port: {_configuration.Port}");
        logMessage.AppendLine($" - Username: {_configuration.Username}");
        logMessage.AppendLine($" - Password: {new string('*', _configuration.Password.Length)}");
        logMessage.AppendLine($" - Exchange: {_configuration.Exchange}");
        logMessage.AppendLine($" - Queue: {_configuration.Queue}");
        logMessage.AppendLine($" - RoutingKey: {_configuration.RoutingKey}");
        Log.Information(logMessage.ToString());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Policy
            .Handle<Exception>()
            .WaitAndRetry(9, r => TimeSpan.FromSeconds(5), (ex, ts) => { Log.Error("Error connecting to RabbitMQ. Retrying in 5 sec."); })
            .Execute(async () =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.Host,
                    VirtualHost = _configuration.VirtualHost,
                    UserName = _configuration.Username,
                    Password = _configuration.Password,
                    Port = _configuration.Port
                };
                
                _connection = await factory.CreateConnectionAsync(cancellationToken);
                
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
                await _channel.ExchangeDeclareAsync(_configuration.Exchange, "fanout", durable: true, autoDelete: false, cancellationToken: cancellationToken);
                await _channel.QueueDeclareAsync(_configuration.Queue, durable: true, autoDelete: false, exclusive: false, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(_configuration.Queue, _configuration.Exchange, _configuration.RoutingKey, cancellationToken: cancellationToken);
                
                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.ReceivedAsync += OnConsumerReceived;
                
                _consumerTag = await _channel.BasicConsumeAsync(_configuration.Queue, false, _consumer, cancellationToken: cancellationToken);
            });
    }

    private async Task OnConsumerReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            // Get message type
            object? messageTypeObject = ea.BasicProperties.Headers?["MessageType"];
            if (messageTypeObject == null)
            {
                Log.Warning("Message missing MessageType header");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }
            
            string messageTypeName = Encoding.UTF8.GetString((byte[])messageTypeObject);
            Type? messageType = _messageTypeRegistry.GetType(messageTypeName);
            if (messageType == null)
            {
                Log.Debug("Unknown message type {MessageType}, ignoring", messageTypeName);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false); // Ack it - it's not for us
                return;
            }

            // Get message body
            string body = Encoding.UTF8.GetString(ea.Body.Span);
            JObject? messageJson = MessageSerializer.Deserialize(body);
            if (messageJson == null)
            {
                Log.Error("Malformed message body");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false); // Don't requeue bad data
                return;
            }
            
            // Create message instance
            object? messageObject = messageJson.ToObject(messageType);
            if (messageObject is not Message message)
            {
                Log.Error("Message body was not derived from MessageType {Type}", messageType.Name);
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }
            
            // Handle message (now properly awaited)
            if (await OnMessageReceivedAsync(message)) await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            else await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing message");
            await _channel!.BasicNackAsync(ea.DeliveryTag, false, true); // Requeue on unexpected error
        }
    }

    private async Task<bool> OnMessageReceivedAsync(Message message)
    {
        // Get message handler
        using IServiceScope messageHandlerScope = _serviceScopeFactory.CreateScope();
        IMessageHandler? messageHandler = messageHandlerScope.ServiceProvider.GetService<IMessageHandler>();
        if (messageHandler == null)
        {
            Log.Warning("Failed to create IMessageHandler instance");
            return false;
        }
        
        // Get the actual runtime type of the message
        Type messageType = message.GetType();
        
        // Find matching handler method
        foreach (var method in messageHandler.GetType().GetMethods(
                     BindingFlags.Public |
                     BindingFlags.NonPublic |
                     BindingFlags.Instance))
        {
            // Make sure this method has the MessageHandler Attribute
            if (!method.IsDefined(typeof(MessageHandlerAttribute))) continue;
            
            // Get parameter type
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != 1) continue;
            
            Type handlerMessageType = parameters[0].ParameterType;
            
            // Check if the actual message type matches this handler's expected type
            if (!handlerMessageType.IsAssignableFrom(messageType))
                continue; // This handler doesn't handle this message type
            
            // Validate return type
            bool isAsync = method.ReturnType == typeof(Task<bool>);
            bool isSync = method.ReturnType == typeof(bool);
            
            if (!isAsync && !isSync)
                throw new InvalidOperationException(
                    $"[MessageHandler] Method {method.Name} must return bool or Task<bool>");

            // Invoke the handler
            try
            {
                object? result = method.Invoke(messageHandler, [message]);
                if (isAsync) return await (Task<bool>) result!;
                return (bool) result!;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error invoking handler {Method} for message type {Type}", 
                    method.Name, messageType.Name);
                throw;
            }
        }

        Log.Warning("No handler found for message type {Type}", messageType.Name);
        return false;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection == null)
        {
            return;
        }
    
        if (_channel != null)
        {
            if (_consumerTag != null)
            {
                await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken);
            }
        
            await _channel.CloseAsync(200, "Goodbye", cancellationToken: cancellationToken);
        }
        
        await _connection.CloseAsync(cancellationToken: cancellationToken);
    }
}
