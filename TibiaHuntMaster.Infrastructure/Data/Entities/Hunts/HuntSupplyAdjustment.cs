namespace TibiaHuntMaster.Infrastructure.Data.Entities.Hunts
{
    public enum SupplyAdjustmentType
    {
        Addition,
        Deduction
    }

    public sealed class HuntSupplyAdjustment
    {
        public int Id { get; set; }

        public int HuntSessionId { get; set; }

        public string Name { get; set; } = string.Empty; // z.B. "Prismatic Ring Correction"

        public long Value { get; set; } // Der Goldwert

        public SupplyAdjustmentType Type { get; set; } // Addieren oder Abziehen?
    }
}