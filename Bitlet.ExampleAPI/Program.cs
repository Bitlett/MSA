using Bitlet.ExampleAPI;
using Bitlet.ExampleAPI.DataAccess;
using Bitlet.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// Add Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// --- Database Context ---
var connectionString = $"server={Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "sql-server"}," +
                       $"{Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "1434"};" +
                       $"user id={Environment.GetEnvironmentVariable("DATABASE_USERNAME") ?? "sa"};" +
                       $"password={Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "database_password"};" +
                       $"database={Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "ExampleAPI"};" +
                       "trustServerCertificate=true;";

builder.Services.AddDbContext<ExampleDbContext>(options => options.UseSqlServer(connectionString));


// --- RabbitMQ ---
builder.Services.UseRabbitMQMessagePublisher(builder.Configuration);

builder.Services.UseRabbitMQMessageSubscriber(builder.Configuration);
builder.Services.AddTransient<IMessageHandler, ExampleMessageHandler>();


// --- Controllers ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// --- CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


// --- Swagger ---
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Example API",
        Version = "v1"
    });
});


// --- Health checks ---
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ExampleDbContext>();


// --- App ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseStaticFiles();
app.UseCors("AllowFrontend");

app.MapControllers();

using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ExampleDbContext>().Migrate();
}

app.UseHealthChecks("/hc");

app.UseHttpsRedirection();

app.Run();
