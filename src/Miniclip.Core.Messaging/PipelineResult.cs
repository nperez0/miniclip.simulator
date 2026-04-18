namespace Miniclip.Core.Messaging;

public readonly record struct PipelineResult(bool IsSuccess, bool ShouldDeadLetter, string? ErrorMessage);
