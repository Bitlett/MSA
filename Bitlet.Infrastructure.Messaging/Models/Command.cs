using System;

namespace Bitlet.Infrastructure.Messaging.Models;

public class Command(Guid id) : Message(id);
