using Bitlet.Infrastructure.Messaging.Models;

namespace Bitlet.ExampleService.Events;

public class ExampleModelCreated(Guid id, Guid modelId) : Event(id)
{
    public readonly Guid ModelId = modelId;
}
