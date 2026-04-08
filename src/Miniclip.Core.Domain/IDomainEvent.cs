namespace Miniclip.Core.Domain;

public interface IDomainEvent
{
    Guid AggregateId { get; }
}
