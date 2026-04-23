using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Imbuement
{
    [Table("ImbuementProfiles")]
    public sealed class ImbuementProfileEntity
    {
        [Key]public int Id { get; set; }

        public int CharacterId { get; set; }

        public CharacterEntity Character { get; set; } = null!;

        // Nutzt der User Blank Scrolls? (+25k Fee pro Item)
        public bool UseBlankScrolls { get; set; }

        // Liste der aktiven Imbuements auf dem Char
        public List<CharacterActiveImbuement> ActiveImbuements { get; set; } = new();

        // <--- HIER HAT ES GEFEHLT:
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    }

    [Table("CharacterActiveImbuements")]
    public sealed class CharacterActiveImbuement
    {
        [Key]public int Id { get; set; }

        public int ImbuementProfileId { get; set; }

        public ImbuementProfileEntity ImbuementProfile { get; set; } = null!; // Navigation

        public int ImbuementRecipeId { get; set; }

        [ForeignKey(nameof(ImbuementRecipeId))]
        public ImbuementRecipeEntity Recipe { get; set; } = null!;

        // Anzahl (z.B. 2x Void)
        public int Count { get; set; }
    }
}