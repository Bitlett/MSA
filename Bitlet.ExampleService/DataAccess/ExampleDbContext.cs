using Bitlet.ExampleService.Models;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Bitlet.ExampleService.DataAccess;

public class ExampleDbContext(DbContextOptions<ExampleDbContext> options) : DbContext(options)
{
    public DbSet<ExampleModel> ExampleModels { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ExampleModel>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.ToTable("ExampleModels");
        });
    }

    public void Migrate()
    {
        Policy
            .Handle<Exception>()
            .WaitAndRetry(
                10,
                retryAttempt => TimeSpan.FromSeconds(10),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"[{GetType().Name}] Retry {retryCount}/10 after error: {exception.Message}");
                    if (exception.InnerException != null)
                    {
                        Console.WriteLine($"[{GetType().Name}] Inner exception: {exception.InnerException.Message}");
                    }
                })
            .Execute(() =>
            {
                Console.WriteLine($"[{GetType().Name}] Attempting to connect and migrate...");
                Database.Migrate();
                Console.WriteLine($"[{GetType().Name}] Migration completed successfully!");
            });
    }
}
