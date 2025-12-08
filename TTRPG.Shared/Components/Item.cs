namespace TTRPG.Shared.Components
{
    public struct Item
    {
        public string Id;           // e.g. "potion_healing"
        public string Name;         // e.g. "Health Potion"
        public string Description;  // e.g. "Restores 10 HP"
        public int Value;           // Gold value
        public float Weight;        // Encumbrance
        public string Icon;         // Texture name (e.g. "potion")
        public bool IsStackable;
    }
}