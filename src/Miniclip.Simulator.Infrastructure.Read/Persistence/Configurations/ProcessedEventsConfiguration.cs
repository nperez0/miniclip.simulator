using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Configurations;

public class ProcessedEventsConfiguration : IEntityTypeConfiguration<ProcessedEventModel>
{
    public void Configure(EntityTypeBuilder<ProcessedEventModel> builder)
    {
        builder.ToTable("ProcessedEvents");
        builder.HasKey(x => new { x.EventId, x.ConsumerGroup });
        builder.Property(x => x.EventId).HasMaxLength(36).IsRequired();
        builder.Property(x => x.ConsumerGroup).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired();
    }
}
