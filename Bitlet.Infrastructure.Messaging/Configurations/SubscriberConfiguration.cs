using Bitlet.Infrastructure.Messaging.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Bitlet.Infrastructure.Messaging.Configurations;

public class SubscriberConfiguration(IConfigurationSection configSection) : Configuration(configSection)
{
    public string Queue { get; } = DetermineQueue(configSection);
    public string RoutingKey { get; } = DetermineRoutingKey(configSection);

    private static string DetermineQueue(IConfigurationSection configSection)
    {
        string? queue = configSection["Queue"];
        
        if (string.IsNullOrEmpty(queue))
        {
            throw new InvalidConfigurationException($"Required config-setting 'Queue' not found.");
        }

        return queue;
    }

    private static string DetermineRoutingKey(IConfigurationSection configSection)
    {
        return configSection["RoutingKey"] ?? "";
    }
}
