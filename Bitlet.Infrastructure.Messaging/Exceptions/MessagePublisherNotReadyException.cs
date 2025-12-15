using System;

namespace Bitlet.Infrastructure.Messaging.Exceptions;

public class MessagePublisherNotReadyException : Exception
{
    public MessagePublisherNotReadyException()
    {
    }

    public MessagePublisherNotReadyException(string message) : base(message)
    {
    }

    public MessagePublisherNotReadyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
