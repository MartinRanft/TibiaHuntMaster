using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TibiaHuntMaster.Infrastructure.Data.Entities.TibiaData;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Character
{
    public enum GoalType
    {
        Level, // Ziel: Level 600
        Gold, // Ziel: 100kk Balance
        Bestiary // Ziel: 2500 Kills (Zukunftsmusik)
    }

    [Table("CharacterGoals")]
    public sealed class CharacterGoalEntity
    {
        [Key]public int Id { get; set; }

        public int CharacterId { get; set; }

        [ForeignKey(nameof(CharacterId))]public CharacterEntity Character { get; set; } = null!;

        [MaxLength(100)]public string Title { get; set; } = string.Empty; // z.B. "Falcon Greaves Fund"

        public GoalType Type { get; set; }

        // Das Ziel (z.B. Level 600 oder Goldbetrag 30.000.000)
        public long TargetValue { get; set; }

        // Startwert beim Anlegen (damit wir den Fortschrittsbalken relativ malen können)
        // z.B. Start bei Level 500, Ziel 600.
        public long StartValue { get; set; }

        // Manueller Fortschritt (z.B. Gold durch Market-Verkäufe), der auf Hunt-Profit draufgerechnet wird
        public long ManualProgressOffset { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsCompleted { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}