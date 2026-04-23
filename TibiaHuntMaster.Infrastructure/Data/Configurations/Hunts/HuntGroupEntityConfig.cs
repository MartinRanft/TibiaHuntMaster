using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Hunts
{
    public class HuntGroupEntityConfig : IEntityTypeConfiguration<HuntGroupEntity>
    {
        public void Configure(EntityTypeBuilder<HuntGroupEntity> builder)
        {
            builder.ToTable("HuntGroups");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CreatedAt)
                   .HasConversion(new DateTimeOffsetToBinaryConverter());

            // 1:n Beziehung
            builder.HasMany(g => g.Sessions)
                   .WithOne(s => s.Group)
                   .HasForeignKey(s => s.HuntGroupId)
                   .OnDelete(DeleteBehavior.SetNull); // Wenn Gruppe gelöscht wird, bleiben Hunts erhalten!
        }
    }
}