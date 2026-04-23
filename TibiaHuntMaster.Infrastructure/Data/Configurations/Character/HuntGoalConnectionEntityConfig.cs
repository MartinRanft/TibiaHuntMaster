using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Character;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Character
{
    public class HuntGoalConnectionEntityConfig : IEntityTypeConfiguration<HuntGoalConnectionEntity>
    {
        public void Configure(EntityTypeBuilder<HuntGoalConnectionEntity> builder)
        {
            builder.ToTable("HuntGoalConnections");
            builder.HasOne(x => x.Goal).WithMany().HasForeignKey(x => x.CharacterGoalId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.SoloSession).WithMany().HasForeignKey(x => x.HuntSessionId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.TeamSession).WithMany().HasForeignKey(x => x.TeamHuntSessionId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}