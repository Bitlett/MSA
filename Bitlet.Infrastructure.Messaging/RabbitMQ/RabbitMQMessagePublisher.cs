using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bitlet.Infrastructure.Messaging.Configurations;
using Bitlet.Infrastructure.Messaging.Exceptions;
using Bitlet.Infrastructure.Messaging.Models;
using Bitlet.Infrastructure.Messaging.Serializers;
using Polly;
using RabbitMQ.Client;
using Serilog;

namespace Bitlet.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ implementation of the MessagePublisher.
/// </summary>
public sealed class RabbitMQMessagePublisher : IMessagePublisher, IDisposable
{
    public readonly Configuration _configuration;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQMessagePublisher(Configuration configuration) : this(configuration, new MessageTypeRegistry()) {}

    public RabbitMQMessagePublisher(Configuration configuration, MessageTypeRegistry messageTypeRegistry)
    {
        _configuration = configuration;
        _connection = null;
        _channel = null;

        var logMessage = new StringBuilder();
        logMessage.AppendLine("Create RabbitMQ message-publisher instance using config:");
        logMessage.AppendLine($" - Host: {_configuration.Host}");
        logMessage.AppendLine($" - VirtualHost: {_configuration.VirtualHost}");
        logMessage.AppendLine($" - Port: {_configuration.Port}");
        logMessage.AppendLine($" - UserName: {_configuration.Username}");
        logMessage.AppendLine($" - Password: {new string('*', _configuration.Password.Length)}");
        logMessage.Append($" - Exchange: {_configuration.Exchange}");
        Log.Information(logMessage.ToString());
    }
    
    private async Task EnsureConnectedAsync()
    {
        if (_channel != null) return;
        
        await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(9, r => TimeSpan.FromSeconds(5), (ex, ts) => { Log.Error("Error connecting to RabbitMQ. Retrying in 5 sec."); })
            .ExecuteAsync(async () =>
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.Host,
                    VirtualHost = _configuration.VirtualHost,
                    UserName = _configuration.Username,
                    Password = _configuration.Password,
                    Port = _configuration.Port,
                    AutomaticRecoveryEnabled = true
                };
                
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                
                await _channel.ExchangeDeclareAsync(_configuration.Exchange, "fanout", durable: true, autoDelete: false);
            });
    }

    /// <summary>
    /// Publish a message.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="routingKey">The routingkey to use (RabbitMQ specific).</param>
    public async Task PublishMessageAsync(Message message, string routingKey)
    {
        await EnsureConnectedAsync();
        
        // Serialize the message
        string data = MessageSerializer.Serialize(message);
        
        // Turn it into a raw bytestream
        byte[] body = Encoding.UTF8.GetBytes(data);
        
        // Construct amqp message properties
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { { "MessageType", message.GetType().Name } }
        };
        
        // Readiness check
        if (_channel == null)
            throw new MessagePublisherNotReadyException("RabbitMQ not ready!");
        
        // Send the message
        await _channel.BasicPublishAsync(_configuration.Exchange, routingKey, false, properties, body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
        _connection?.Dispose();
        _connection = null;
    }

    ~RabbitMQMessagePublisher()
    {
        Dispose();
    }
}
