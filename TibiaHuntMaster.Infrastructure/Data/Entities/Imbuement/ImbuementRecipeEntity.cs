using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement
{
    public enum ImbuementTier
    {
        Basic = 1,
        Intricate = 2,
        Powerful = 3
    }

    public enum ImbuementType
    {
        Vampirism,
        Void,
        Strike,
        Skill,
        Protection,
        Utility
    }

    [Table("ImbuementRecipes")]
    public sealed class ImbuementRecipeEntity
    {
        [Key]public int Id { get; set; }

        public string Name { get; set; } = string.Empty; // z.B. "Powerful Void"

        public ImbuementType Type { get; set; }

        public ImbuementTier Tier { get; set; }

        // Deine fixen Fees (7.5k, 60k, 250k)
        public long BaseFee { get; set; }

        public List<ImbuementIngredientEntity> Ingredients { get; set; } = new();
    }

    [Table("ImbuementIngredients")]
    public sealed class ImbuementIngredientEntity
    {
        [Key]public int Id { get; set; }

        public int ImbuementRecipeId { get; set; }

        public ImbuementRecipeEntity Recipe { get; set; } = null!;

        public int ItemId { get; set; } // Verweis auf deine Items Tabelle

        [ForeignKey(nameof(ItemId))]public ItemEntity Item { get; set; } = null!;

        public int Amount { get; set; } // z.B. 25
    }
}