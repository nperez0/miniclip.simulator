using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Configurations;

/// <summary>
/// EF Core configuration for GroupOverviewReadModel.
/// Complete dashboard view with embedded standings and matches.
/// </summary>
public class GroupOverviewReadModelConfiguration : IEntityTypeConfiguration<GroupOverviewReadModel>
{
    public void Configure(EntityTypeBuilder<GroupOverviewReadModel> builder)
    {
        builder.ToTable("GroupOverviews");

        builder.HasKey(x => x.GroupId);

        builder.Property(x => x.GroupId)
            .ValueGeneratedNever();

        builder.Property(x => x.GroupName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Capacity)
            .IsRequired();

        builder.Property(x => x.TotalTeams)
            .IsRequired();

        builder.Property(x => x.TotalMatches)
            .IsRequired();

        builder.Property(x => x.PlayedMatches)
            .IsRequired();

        builder.Property(x => x.RemainingMatches)
            .IsRequired();

        builder.Property(x => x.IsComplete)
            .IsRequired();

        builder.Property(x => x.LastUpdated)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        // Owned collection: Standings (stored as JSON)
        builder.OwnsMany(x => x.Standings, standings =>
        {
            standings.ToJson();
            
            standings.Property(s => s.Position).IsRequired();
            standings.Property(s => s.TeamId).IsRequired();
            standings.Property(s => s.TeamName).IsRequired().HasMaxLength(200);
            standings.Property(s => s.Points).IsRequired();
            standings.Property(s => s.MatchesPlayed).IsRequired();
            standings.Property(s => s.Wins).IsRequired();
            standings.Property(s => s.Draws).IsRequired();
            standings.Property(s => s.Losses).IsRequired();
            standings.Property(s => s.GoalsFor).IsRequired();
            standings.Property(s => s.GoalsAgainst).IsRequired();
            standings.Property(s => s.GoalDifference).IsRequired();
            standings.Property(s => s.Qualifies).IsRequired();
        });

        // Owned collection: RecentMatches (stored as JSON)
        builder.OwnsMany(x => x.RecentMatches, matches =>
        {
            matches.ToJson();
            
            matches.Property(m => m.MatchId).IsRequired();
            matches.Property(m => m.HomeTeam).IsRequired().HasMaxLength(200);
            matches.Property(m => m.AwayTeam).IsRequired().HasMaxLength(200);
            matches.Property(m => m.HomeScore).IsRequired();
            matches.Property(m => m.AwayScore).IsRequired();
            matches.Property(m => m.Round).IsRequired();
            matches.Property(m => m.IsPlayed).IsRequired();
        });

        // Index for lookups
        builder.HasIndex(x => x.GroupId)
            .HasDatabaseName("IX_GroupOverviews_GroupId")
            .IsUnique();
    }
}
