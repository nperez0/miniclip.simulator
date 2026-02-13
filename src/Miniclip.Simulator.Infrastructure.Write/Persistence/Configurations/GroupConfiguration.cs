using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Miniclip.Simulator.Domain.Aggregates.Groups.Entities;
using Miniclip.Simulator.Domain.Aggregates.Teams.Entities;

namespace Miniclip.Simulator.Infrastructure.Write.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("Groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedNever();

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Capacity)
            .IsRequired();

        builder.HasMany(g => g.Teams)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "GroupTeams",
                j => j.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey("TeamId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Group>()
                    .WithMany()
                    .HasForeignKey("GroupId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("GroupId", "TeamId");
                    j.ToTable("GroupTeams");
                });

        builder.OwnsMany(g => g.Matches, matches =>
        {
            matches.ToTable("Matches");
            
            matches.WithOwner()
                .HasForeignKey("GroupId");

            matches.HasKey("GroupId", "Id");

            matches.Property(m => m.Id)
                .HasColumnName("MatchId")
                .ValueGeneratedNever();

            matches.Property(m => m.Round)
                .IsRequired();

            matches.Property(m => m.HomeScore)
                .IsRequired();

            matches.Property(m => m.AwayScore)
                .IsRequired();

            matches.Property(m => m.IsPlayed)
                .IsRequired();

            matches.HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey("HomeTeamId")
                .OnDelete(DeleteBehavior.Restrict);

            matches.HasOne(m => m.AwayTeam)
                .WithMany()
                .HasForeignKey("AwayTeamId")
                .OnDelete(DeleteBehavior.Restrict);

            matches.Navigation(m => m.HomeTeam).IsRequired();
            matches.Navigation(m => m.AwayTeam).IsRequired();
        });
    }
}
