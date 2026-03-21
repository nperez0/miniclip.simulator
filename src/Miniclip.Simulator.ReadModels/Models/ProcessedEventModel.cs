namespace Miniclip.Simulator.ReadModels.Models;

public class ProcessedEventModel
{
    public required string EventId { get; init; }
    public required string ConsumerGroup { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
