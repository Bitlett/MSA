using Bitlet.Infrastructure.Messaging.Models;

namespace Bitlet.ExampleAPI.Commands;

public class CreateExampleModel(Guid modelId) : Command(Guid.NewGuid())
{
    public Guid ModelId { get; init; } = modelId;
}
