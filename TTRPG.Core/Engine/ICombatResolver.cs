using Arch.Core;

namespace TTRPG.Core.Engine
{
    public interface ICombatResolver
    {
        /// <summary>
        /// Pure function: Reads the World state and calculates the outcome of an attack.
        /// Does NOT modify the World directly.
        /// </summary>
        CombatResult ResolveAttack(World world, Entity attacker, Entity defender);
    }
}