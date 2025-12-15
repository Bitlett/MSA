using Bitlet.ExampleAPI.Commands;
using Bitlet.ExampleAPI.Events;
using Bitlet.ExampleAPI.Models;

namespace Bitlet.ExampleAPI.Mappers;

public static class Mappers
{
    public static ExampleModelCreated MapToExampleModelCreated(this CreateExampleModel command) => new ExampleModelCreated
    (
        Guid.NewGuid(),
        command.ModelId
    );

    public static ExampleModel MapToExampleModel(this ExampleModelCreated e) => new ExampleModel
    {
        Id = e.ModelId
    };
}
