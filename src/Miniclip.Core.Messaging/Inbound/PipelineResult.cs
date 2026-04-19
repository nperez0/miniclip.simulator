namespace Miniclip.Core.Messaging.Inbound;

public readonly record struct PipelineResult(bool IsSuccess, bool ShouldDeadLetter, string? ErrorMessage);
