using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Core.Engine;
using TTRPG.Shared.Components;

namespace TTRPG.Rules.Pathfinder
{
    public class PathfinderResolver : ICombatResolver
    {
        public CombatResult ResolveAttack(World world, CombatRequest request)
        {
            var result = new CombatResult();
            var attacker = request.Attacker;
            var defender = request.Defender;

            // 1. Validate Entities exist and have required components
            if (!world.IsAlive(attacker) || !world.IsAlive(defender))
            {
                result.CombatLog = "Invalid Target.";
                return result;
            }

            // 2. Gather Data (The "Read" Phase)
            // We default to 10/0 if components are missing (Safety check)
            int attackBonus = 0;
            if (world.Has<Attributes>(attacker))
            {
                var attrs = world.Get<Attributes>(attacker);
                attackBonus = attrs.GetModifier(attrs.Strength); // Melee uses STR
            }

            int armorClass = 10;
            if (world.Has<DerivedStats>(defender))
            {
                armorClass = world.Get<DerivedStats>(defender).ArmorClass;
            }

            // 3. The Roll (The "Math" Phase)
            int roll = Dice.Roll("1d20");
            int totalHit = roll + attackBonus;

            // 4. Resolution (Hit or Miss)
            if (totalHit >= armorClass)
            {
                result.IsHit = true;

                // Roll Damage (Simulated "1d8 + STR")
                int damageRoll = Dice.Roll("1d8");
                result.TotalDamage = damageRoll + attackBonus;

                result.CombatLog = $"Rolled {roll} + {attackBonus} = {totalHit} (vs AC {armorClass}) -> HIT! for {result.TotalDamage} dmg.";

                // 5. Generate State Changes (The "Write" Instruction)
                result.Changes.Add(new StateChange
                {
                    Target = defender,
                    Component = "Health",
                    Field = "Current",
                    Type = ChangeType.Subtract,
                    Amount = result.TotalDamage
                });
            }
            else
            {
                result.IsHit = false;
                result.CombatLog = $"Rolled {roll} + {attackBonus} = {totalHit} (vs AC {armorClass}) -> MISS.";
            }

            return result;
        }
    }
}