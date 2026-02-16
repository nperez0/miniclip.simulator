using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Miniclip.Simulator.ReadModels.Models;

namespace Miniclip.Simulator.Infrastructure.Read.Persistence.Configurations;

public class MatchResultReadModelConfiguration : IEntityTypeConfiguration<MatchResultModel>
{
    public void Configure(EntityTypeBuilder<MatchResultModel> builder)
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

        builder.Property(x => x.MatchId)
            .IsRequired();

        builder.Property(x => x.Round)
            .IsRequired();

        builder.Property(x => x.IsPlayed)
            .IsRequired();

        builder.Property(x => x.HomeTeamId)
            .IsRequired();

        builder.Property(x => x.HomeTeamName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.HomeScore)
            .IsRequired();

        builder.Property(x => x.AwayTeamId)
            .IsRequired();

        builder.Property(x => x.AwayTeamName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AwayScore)
            .IsRequired();

        builder.Property(x => x.PlayedAt);

        // Indexes for fast queries
        builder.HasIndex(x => x.GroupId)
            .HasDatabaseName("IX_MatchResults_GroupId");

        builder.HasIndex(x => x.MatchId)
            .HasDatabaseName("IX_MatchResults_MatchId")
            .IsUnique();

        builder.HasIndex(x => new { x.GroupId, x.Round })
            .HasDatabaseName("IX_MatchResults_GroupId_Round");

        // For head-to-head queries
        builder.HasIndex(x => new { x.GroupId, x.HomeTeamId, x.AwayTeamId })
            .HasDatabaseName("IX_MatchResults_GroupId_Teams");
    }
}
