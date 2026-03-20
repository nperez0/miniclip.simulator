using Miniclip.Core.Domain;

namespace Miniclip.Core.EventSourcing;

public sealed record EventEnvelope(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    long Version,
    DateTimeOffset OccurredOn,
    IDomainEvent Data);
