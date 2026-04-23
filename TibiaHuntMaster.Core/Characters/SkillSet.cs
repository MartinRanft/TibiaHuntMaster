namespace TibiaHuntMaster.Core.Characters
{
    public sealed class SkillSet
    {
        public int MagicLevel { get; set; }

        public int Fist { get; set; }

        public int Club { get; set; }

        public int Sword { get; set; }

        public int Axe { get; set; }

        public int Distance { get; set; }

        public int Shielding { get; set; }

        public int Fishing { get; set; }

        public override string ToString()
        {
            return $"ML {MagicLevel} | Melee {Sword}/{Axe}/{Club} | Dist {Distance} | Shield {Shielding}";
        }
    }
}