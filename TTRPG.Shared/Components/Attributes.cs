namespace TTRPG.Shared.Components
{
    public struct Attributes
    {
        public int Strength;
        public int Dexterity;
        public int Constitution;
        public int Intelligence;
        public int Wisdom;
        public int Charisma;

        // Helper to calculate modifier: (Score - 10) / 2
        public int GetModifier(int score)
        {
            return (score - 10) / 2;
        }
    }
}