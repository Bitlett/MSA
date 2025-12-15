using Bitlet.Infrastructure.Messaging.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Bitlet.Infrastructure.Messaging.Configurations;

public class Configuration(IConfigurationSection configSection)
{
    private const ushort DefaultPort = 5672;
    private const string DefaultVirtualHost = "/";

    public string Host { get; } = DetermineHost(configSection);
    public string VirtualHost { get; } = DetermineVirtualHost(configSection);
    public ushort Port { get; } = DeterminePort(configSection);
    public string Username { get; } = DetermineUsername(configSection);
    public string Password { get; } = DeterminePassword(configSection);
    public string Exchange { get; } = DetermineExchange(configSection);
    
    private static string DetermineHost(IConfigurationSection configSection)
    {
        string? host = configSection["Host"];
        
        if (string.IsNullOrEmpty(host))
        {
            throw new InvalidConfigurationException($"Required config-setting 'Host' not found.");
        }

        return host;
    }

    private static string DetermineVirtualHost(IConfigurationSection configSection)
    {
        string? virtualHost = configSection["VirtualHost"];

        return string.IsNullOrEmpty(virtualHost) ? DefaultVirtualHost : virtualHost;
    }

    private static ushort DeterminePort(IConfigurationSection configSection)
    {
        string? port = configSection["Port"];
        
        if (string.IsNullOrEmpty(port))
        {
            return DefaultPort;
        }
        
        if (ushort.TryParse(port, out ushort result))
        {
            return result;
        }
        
        throw new InvalidConfigurationException("Unable to parse config-setting 'Port' into an integer.");
    }
    private static string DetermineUsername(IConfigurationSection configSection)
    {
        string? username = configSection["Username"];
        
        if (string.IsNullOrEmpty(username))
        {
            throw new InvalidConfigurationException($"Required config-setting 'Username' not found.");
        }

        return username;
    }

    private static string DeterminePassword(IConfigurationSection configSection)
    {
        string? password = configSection["Password"];
        
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidConfigurationException($"Required config-setting 'Password' not found.");
        }

        return password;
    }

    private static string DetermineExchange(IConfigurationSection configSection)
    {
        string? exchange = configSection["Exchange"];
        
        if (string.IsNullOrEmpty(exchange))
        {
            throw new InvalidConfigurationException($"Required config-setting 'Exchange' not found.");
        }

        return exchange;
    }
}
