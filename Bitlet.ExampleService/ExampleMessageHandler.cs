using Bitlet.ExampleService.DataAccess;
using Bitlet.ExampleService.Events;
using Bitlet.ExampleService.Mappers;
using Bitlet.ExampleService.Models;
using Bitlet.Infrastructure.Messaging;
using Bitlet.Infrastructure.Messaging.Attributes;
using Serilog;

namespace Bitlet.ExampleService;

public class ExampleMessageHandler(IServiceScopeFactory scopeFactory) : IMessageHandler
{
    [MessageHandler]
    public async Task<bool> Handle(ExampleModelCreated e)
    {
        // Create TransferDbContext instance
        using IServiceScope dbScope = scopeFactory.CreateScope();
        ExampleDbContext? dbContext = dbScope.ServiceProvider.GetService<ExampleDbContext>();
        if (dbContext == null)
        {
            Log.Warning("WARNING: Failed to create ExampleDbContext instance");
            return false;
        }

        // Map Event to Instance
        ExampleModel exampleModel = e.MapToExampleModel();

        // Store the new model
        dbContext.ExampleModels.Add(exampleModel);
        await dbContext.SaveChangesAsync();
        
        // Log the change
        Log.Information("Created ExampleModel: {@ExampleModel}", exampleModel);

        // ACK event
        return true;
    }
}
