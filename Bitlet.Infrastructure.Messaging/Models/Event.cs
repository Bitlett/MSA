using System;

namespace Bitlet.Infrastructure.Messaging.Models;

public class Event(Guid id) : Message(id);
