namespace Miniclip.Core.EventSourcing.EventStoreDB;

public sealed record EventMetadata(Guid CorrelationId, Guid CausationId, string? TraceParent, string? TraceState);
