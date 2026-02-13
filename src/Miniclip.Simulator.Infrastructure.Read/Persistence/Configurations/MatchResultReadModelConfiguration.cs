using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Configurations;

/// <summary>
/// EF Core configuration for MatchResultReadModel.
/// Denormalized table with embedded team data for fast match queries.
/// </summary>
public class MatchResultReadModelConfiguration : IEntityTypeConfiguration<MatchResultReadModel>
{
    public void Configure(EntityTypeBuilder<MatchResultReadModel> builder)
    {
        builder.ToTable("MatchResults");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.GroupId)
            .IsRequired();

        builder.Property(x => x.GroupName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.HomeTeamId)
            .IsRequired();

        builder.Property(x => x.HomeTeamName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.HomeTeamStrength)
            .IsRequired();

        builder.Property(x => x.AwayTeamId)
            .IsRequired();

        builder.Property(x => x.AwayTeamName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AwayTeamStrength)
            .IsRequired();

        builder.Property(x => x.HomeScore)
            .IsRequired();

        builder.Property(x => x.AwayScore)
            .IsRequired();

        builder.Property(x => x.Round)
            .IsRequired();

        builder.Property(x => x.IsPlayed)
            .IsRequired();

        builder.Property(x => x.PlayedAt)
            .IsRequired(false);

        builder.Property(x => x.Result)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.GoalDifference)
            .IsRequired();

        // Indexes for fast queries
        builder.HasIndex(x => x.GroupId)
            .HasDatabaseName("IX_MatchResults_GroupId");

        builder.HasIndex(x => new { x.GroupId, x.Round })
            .HasDatabaseName("IX_MatchResults_GroupId_Round");

        builder.HasIndex(x => x.HomeTeamId)
            .HasDatabaseName("IX_MatchResults_HomeTeamId");

        builder.HasIndex(x => x.AwayTeamId)
            .HasDatabaseName("IX_MatchResults_AwayTeamId");
    }
}
