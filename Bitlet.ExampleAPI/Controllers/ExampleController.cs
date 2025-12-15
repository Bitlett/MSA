using Bitlet.ExampleAPI.Commands;
using Bitlet.ExampleAPI.DataAccess;
using Bitlet.ExampleAPI.Events;
using Bitlet.ExampleAPI.Mappers;
using Bitlet.ExampleAPI.Models;
using Bitlet.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Bitlet.ExampleAPI.Controllers;

[Route("/")]
public class ProductsController(ExampleDbContext dbContext, IMessagePublisher messagePublisher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await dbContext.ExampleModels.ToListAsync());
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] CreateExampleModel command)
    {
        // Business Rule: Make sure there isn't already a model with this ID
        ExampleModel? existingModel = await dbContext.ExampleModels.FirstOrDefaultAsync(m => m.Id == command.ModelId);
        if (existingModel != null) return Conflict();

        // Map the command to an event
        ExampleModelCreated e = command.MapToExampleModelCreated();

        // Publish the event
        await messagePublisher.PublishMessageAsync(e, "");

        // Return OK
        return Ok();
    }
}
