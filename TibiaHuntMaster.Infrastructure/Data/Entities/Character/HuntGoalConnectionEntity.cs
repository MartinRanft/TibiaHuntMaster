using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using TibiaHuntMaster.Infrastructure.Data.Entities.Hunts;

namespace TibiaHuntMaster.Infrastructure.Data.Entities.Character
{
    [Table("HuntGoalConnections")]
    public sealed class HuntGoalConnectionEntity
    {
        [Key]public int Id { get; set; }

        public int CharacterGoalId { get; set; }

        [ForeignKey(nameof(CharacterGoalId))]public CharacterGoalEntity Goal { get; set; } = null!;

        public int? HuntSessionId { get; set; }

        [ForeignKey(nameof(HuntSessionId))]public HuntSessionEntity? SoloSession { get; set; }

        public int? TeamHuntSessionId { get; set; }

        [ForeignKey(nameof(TeamHuntSessionId))]
        public TeamHuntSessionEntity? TeamSession { get; set; }

        public bool IsFinisher { get; set; }
    }
}