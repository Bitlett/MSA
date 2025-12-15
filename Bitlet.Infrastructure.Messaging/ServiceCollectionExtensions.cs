using Bitlet.Infrastructure.Messaging.Configurations;
using Bitlet.Infrastructure.Messaging.Exceptions;
using Bitlet.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Bitlet.Infrastructure.Messaging;

public static class ServiceCollectionExtensions
{
    public static void UseRabbitMQMessagePublisher(this IServiceCollection services, IConfiguration config)
    {
        IConfigurationSection configSection = config.GetSection("RabbitMQ");
        if (!configSection.Exists())
            throw new InvalidConfigurationException("Required config-section 'RabbitMQ' not found.");
        
        Configuration configuration = new Configuration(configSection);
        
        services.AddTransient<IMessagePublisher>(_ => new RabbitMQMessagePublisher(configuration));
    }
    
    public static void UseRabbitMQMessageSubscriber(this IServiceCollection services, IConfiguration config)
    {
        IConfigurationSection configSection = config.GetSection("RabbitMQ");
        if (!configSection.Exists())
            throw new InvalidConfigurationException("Required config-section 'RabbitMQ' not found.");
        
        SubscriberConfiguration subscriberConfiguration = new SubscriberConfiguration(configSection);
        
        // Inject IServiceScopeFactory from the service provider
        services.AddHostedService<IMessageSubscriber>(serviceProvider =>
        {
            var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            return new RabbitMQMessageSubscriber(scopeFactory, subscriberConfiguration);
        });
    }
}
