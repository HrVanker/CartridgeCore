using Arch.Core;

namespace TTRPG.Core.Engine
{
    public enum ChangeType
    {
        Set,        // Overwrite the value (e.g., Status = "Poisoned")
        Add,        // Add to value (e.g., Gold += 50)
        Subtract    // Subtract from value (e.g., Health -= 5)
    }

    public struct StateChange
    {
        public Entity Target;       // Who is being changed?
        public string Component;    // Name of the component (e.g., "Health")
        public string Field;        // Name of the field (e.g., "Current")
        public object Amount;       // The value to apply
        public ChangeType Type;     // How to apply it
    }
}