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
            Console.WriteLine($"[Rules] {Name} v{Version} initialized!");
        }

        public ICombatResolver GetResolver()
        {
            return new PathfinderResolver();
        }

        public IUIProvider GetUI()
        {
            return new PathfinderUI();
        }
    }
}