using Xunit;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Core.Engine;
using TTRPG.Shared.Components;
using TTRPG.Rules.Pathfinder; // Ensure you have this namespace available

namespace TTRPG.Tests
{
    public class CombatResolverTests
    {
        [Fact]
        public void ResolveAttack_ShouldCalculateHitAndDamage()
        {
            // Arrange
            var world = World.Create();
            var rules = new PathfinderResolver();

            // Attacker: High Strength (Str 18 -> +4 Mod)
            var attacker = world.Create(new Attributes { Strength = 18 });

            // Defender: Low AC (AC 10)
            var defender = world.Create(new DerivedStats { ArmorClass = 10 });

            var request = new CombatRequest
            {
                Attacker = attacker,
                Defender = defender
            };

            // Act
            // We run multiple times to ensure the random dice eventually hit
            // (With +4 vs AC 10, we need to roll a 6 or higher to hit, which is 75% chance)
            CombatResult result = null;
            for (int i = 0; i < 10; i++)
            {
                result = rules.ResolveAttack(world, request);
                if (result.IsHit) break;
            }

            // Assert
            Assert.True(result.IsHit, "Attacker with +4 should eventually hit AC 10");
            Assert.True(result.TotalDamage > 4, "Damage should be 1d8 + 4 (Min 5)");
            Assert.Single(result.Changes);
            Assert.Equal("Health", result.Changes[0].Component);
            Assert.Equal(ChangeType.Subtract, result.Changes[0].Type);

            // Cleanup
            World.Destroy(world);
        }
    }
}