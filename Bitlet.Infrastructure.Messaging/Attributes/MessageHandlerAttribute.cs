namespace Bitlet.Infrastructure.Messaging.Attributes;

using System;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class MessageHandlerAttribute : Attribute
{
}
