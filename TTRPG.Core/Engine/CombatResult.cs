using System.Collections.Generic;

namespace TTRPG.Core.Engine
{
    public class CombatResult
    {
        // The Narrative Outcome
        public bool IsHit { get; set; }
        public bool IsCritical { get; set; }
        public string CombatLog { get; set; } = string.Empty; // e.g., "Hit for 5 damage!"

        // The Mathematical Outcome
        public int TotalDamage { get; set; }

        // The Instructions for the Server
        public List<StateChange> Changes { get; set; } = new List<StateChange>();
    }
}