using Arch.Core;

namespace TTRPG.Core.Engine
{
    public class CombatRequest
    {
        public Entity Attacker { get; set; }
        public Entity Defender { get; set; }

        // Future proofing:
        // public string WeaponId { get; set; }
        // public bool IsSneakAttack { get; set; }
    }
}