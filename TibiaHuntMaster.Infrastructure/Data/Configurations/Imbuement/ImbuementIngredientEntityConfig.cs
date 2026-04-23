using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement;

namespace TibiaHuntMaster.Infrastructure.Data.Configurations.Imbuement
{
    public class ImbuementIngredientEntityConfig : IEntityTypeConfiguration<ImbuementIngredientEntity>
    {
        public void Configure(EntityTypeBuilder<ImbuementIngredientEntity> builder)
        {
            builder.ToTable("ImbuementIngredients");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount).IsRequired();

            // Link zum Item (TibiaWiki Item)
            builder.HasOne(x => x.Item)
                   .WithMany() // Item hat keine Nav-Property zurück zu Ingredients nötig
                   .HasForeignKey(x => x.ItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}