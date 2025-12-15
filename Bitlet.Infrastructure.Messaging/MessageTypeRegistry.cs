using System;
using System.Collections.Generic;
using System.Reflection;
using Bitlet.Infrastructure.Messaging.Models;

namespace Bitlet.Infrastructure.Messaging;

public sealed class MessageTypeRegistry
{
    public readonly Dictionary<string, Type?> _nameToTypeMapper;

    public MessageTypeRegistry() : this(Assembly.GetEntryAssembly()) {}

    public MessageTypeRegistry(params Assembly[] assemblies)
    {
        _nameToTypeMapper = new Dictionary<string, Type?>();
        
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                // Ignore non-messages
                if (!typeof(Message).IsAssignableFrom(type) || type.IsAbstract) continue;

                // Store bidirectional mapping
                _nameToTypeMapper[type.Name] = type;
            }
        }
    }

    public Type? GetType(string name) => _nameToTypeMapper.GetValueOrDefault(name, null);
}
