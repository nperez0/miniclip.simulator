namespace Miniclip.Core.EventSourcing;

public readonly record struct CommittedEvent(
    IDomainEvent Event,
    Guid AggregateId,
    string AggregateType,
    Guid EventId,
    DateTimeOffset OccurredOn,
    long AggregateVersion);