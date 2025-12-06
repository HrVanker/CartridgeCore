using System;
using TTRPG.Core;
using TTRPG.Core.Engine;

namespace TTRPG.Rules.Pathfinder
{
    public class PathfinderRules : IRuleset
    {
        public string Name => "Pathfinder Core Rules";
        public string Version => "1.0.0";

        public void Register(object world)
        {
            // This is where we will eventually register Systems (Phase 2.2)
            Console.WriteLine($"[Rules] {Name} v{Version} initialized!");
        }

        // This links the Interface (Core) to your Logic (Resolver)
        public ICombatResolver GetResolver()
        {
            return new PathfinderResolver();
        }
    }
}