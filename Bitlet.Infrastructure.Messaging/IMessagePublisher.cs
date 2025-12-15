using System.Threading.Tasks;
using Bitlet.Infrastructure.Messaging.Models;

namespace Bitlet.Infrastructure.Messaging;

public interface IMessagePublisher
{
    /// <summary>
    /// Publish a message.
    /// </summary>
    /// <param name="messageType">Type of the message.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="routingKey">The routingkey to use (RabbitMQ specific).</param>
    Task PublishMessageAsync(Message message, string routingKey);
}
