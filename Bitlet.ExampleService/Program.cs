using Bitlet.ExampleService;
using Bitlet.ExampleService.DataAccess;
using Bitlet.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Serilog;


// Add Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();


IHost host = Host
    .CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // --- Database Context ---
        var connectionString = $"server={Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "sql-server"}," +
                               $"{Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "1434"};" +
                               $"user id={Environment.GetEnvironmentVariable("DATABASE_USERNAME") ?? "sa"};" +
                               $"password={Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "database_password"};" +
                               $"database={Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "ExampleService"};" +
                               "trustServerCertificate=true;";

        services.AddDbContext<ExampleDbContext>(options => options.UseSqlServer(connectionString));
        services.AddHealthChecks().AddDbContextCheck<ExampleDbContext>();


        // --- RabbitMQ ---
        services.UseRabbitMQMessageSubscriber(hostContext.Configuration);
        services.AddTransient<IMessageHandler, ExampleMessageHandler>();
    })
    .UseConsoleLifetime()
    .Build();


// Migrate DB
using (var scope = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ExampleDbContext>().Migrate();
}


// Run Host
await host.RunAsync();
