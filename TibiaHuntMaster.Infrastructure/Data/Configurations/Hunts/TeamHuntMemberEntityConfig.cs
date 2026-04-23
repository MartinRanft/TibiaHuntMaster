using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class TeamHuntMemberEntityConfig : IEntityTypeConfiguration<TeamHuntMemberEntity>
    {
        public void Configure(EntityTypeBuilder<TeamHuntMemberEntity> builder)
        {
            builder.ToTable("TeamHuntMembers");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            // Beziehung zurück zur Session (wurde schon in der SessionConfig definiert, 
            // aber hier die "Gegenseite" explizit zu machen schadet nicht)
            builder.HasOne(m => m.Session)
                   .WithMany(s => s.Members)
                   .HasForeignKey(m => m.TeamHuntSessionId);
        }
    }
}