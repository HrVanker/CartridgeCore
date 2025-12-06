using Arch.Core;

namespace TTRPG.Core.Engine
{
    public interface ICombatResolver
    {
        /// <summary>
        /// Pure function: Reads the World state and calculates the outcome of an attack.
        /// Does NOT modify the World directly.
        /// </summary>
        /// <param name="world">Read-only access to component data</param>
        /// <param name="request">The context of the attack (Who, Whom, How)</param>
        CombatResult ResolveAttack(World world, CombatRequest request);
    }
}