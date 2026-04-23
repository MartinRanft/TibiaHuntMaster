using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Imbuement
{
    public class ImbuementRecipeEntityConfig : IEntityTypeConfiguration<ImbuementRecipeEntity>
    {
        public void Configure(EntityTypeBuilder<ImbuementRecipeEntity> builder)
        {
            builder.ToTable("ImbuementRecipes");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Type).IsRequired(); // Enum als Int
            builder.Property(x => x.Tier).IsRequired(); // Enum als Int
            builder.Property(x => x.BaseFee).IsRequired();

            // 1:n zu Ingredients
            builder.HasMany(x => x.Ingredients)
                   .WithOne(i => i.Recipe)
                   .HasForeignKey(i => i.ImbuementRecipeId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}