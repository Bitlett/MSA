using Bitlet.ExampleService.Events;
using Bitlet.ExampleService.Models;

namespace Bitlet.ExampleService.Mappers;

public static class Mappers
{
    public static ExampleModel MapToExampleModel(this ExampleModelCreated e) => new ExampleModel
    {
        Id = e.ModelId
    };
}
