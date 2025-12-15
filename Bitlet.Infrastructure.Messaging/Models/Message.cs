using System;

namespace Bitlet.Infrastructure.Messaging.Models;

public abstract class Message(Guid id)
{
    public readonly Guid Id = id;

    protected Message() : this(Guid.NewGuid()) {}
}
