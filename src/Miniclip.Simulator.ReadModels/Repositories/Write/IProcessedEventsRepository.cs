namespace Miniclip.Simulator.ReadModels.Repositories.Write;

public interface IProcessedEventsRepository
{
    Task<bool> ContainsAsync(string eventId, string consumerGroup, CancellationToken cancellationToken);
    void Add(string eventId, string consumerGroup);
}
