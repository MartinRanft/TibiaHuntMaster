namespace TibiaHuntMaster.Core.Characters
{
    public sealed class EquipmentSet
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; } = "Default Set";

        public Dictionary<EquipmentSlot, ItemWithImbuements> Slots { get; set; } = new();
    }

    public enum EquipmentSlot
    {
        Head,
        Amulet,
        Armor,
        Legs,
        Boots,
        Ring,
        Backpack,
        RightHand,
        LeftHand
    }

    public sealed class ItemWithImbuements
    {
        public string ItemId { get; set; } = string.Empty;

        public List<string> Imbuements { get; set; } = new();
    }
}