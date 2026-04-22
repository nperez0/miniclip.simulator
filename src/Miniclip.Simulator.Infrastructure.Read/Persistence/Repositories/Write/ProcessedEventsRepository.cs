using Miniclip.Simulator.ReadModels.Repositories.Write;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Repositories.Write;

public class ProcessedEventsRepository(SimulatorReadDbContext context) : IProcessedEventsRepository
{
    public async Task<bool> ContainsAsync(
        string eventId,
        string consumerGroup,
        CancellationToken cancellationToken)
        => await context.Set<ProcessedEventModel>()
            .AnyAsync(e => e.EventId == eventId && e.ConsumerGroup == consumerGroup, cancellationToken);

    public void Add(string eventId, string consumerGroup)
        => context.Set<ProcessedEventModel>().Add(
            new ProcessedEventModel { EventId = eventId, ConsumerGroup = consumerGroup });
}
