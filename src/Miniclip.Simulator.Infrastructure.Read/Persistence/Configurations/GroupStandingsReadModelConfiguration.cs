using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Configurations;

/// <summary>
/// EF Core configuration for GroupStandingsReadModel.
/// Denormalized table for fast standings queries.
/// </summary>
public class GroupStandingsReadModelConfiguration : IEntityTypeConfiguration<GroupStandingsReadModel>
{
    public void Configure(EntityTypeBuilder<GroupStandingsReadModel> builder)
    {
        builder.ToTable("GroupStandings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.GroupId)
            .IsRequired();

        builder.Property(x => x.GroupName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Position)
            .IsRequired();

        builder.Property(x => x.TeamId)
            .IsRequired();

        builder.Property(x => x.TeamName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.TeamStrength)
            .IsRequired();

        builder.Property(x => x.MatchesPlayed)
            .IsRequired();

        builder.Property(x => x.Wins)
            .IsRequired();

        builder.Property(x => x.Draws)
            .IsRequired();

        builder.Property(x => x.Losses)
            .IsRequired();

        builder.Property(x => x.GoalsFor)
            .IsRequired();

        builder.Property(x => x.GoalsAgainst)
            .IsRequired();

        builder.Property(x => x.GoalDifference)
            .IsRequired();

        builder.Property(x => x.Points)
            .IsRequired();

        builder.Property(x => x.QualifiesForKnockout)
            .IsRequired();

        builder.Property(x => x.LastUpdated)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        // Indexes for fast queries
        builder.HasIndex(x => x.GroupId)
            .HasDatabaseName("IX_GroupStandings_GroupId");

        builder.HasIndex(x => new { x.GroupId, x.Position })
            .HasDatabaseName("IX_GroupStandings_GroupId_Position");

        builder.HasIndex(x => new { x.GroupId, x.TeamId })
            .HasDatabaseName("IX_GroupStandings_GroupId_TeamId")
            .IsUnique();
    }
}
