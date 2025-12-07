using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Core.Engine;
using TTRPG.Shared.Components;

namespace TTRPG.Rules.Pathfinder
{
    public class PathfinderUI : IUIProvider
    {
        public Dictionary<string, string> GetInspectionDetails(World world, Entity viewer, Entity target)
        {
            var info = new Dictionary<string, string>();

            // 1. Basic Info (Visible to Everyone)
            info["Entity ID"] = target.Id.ToString();

            // 2. Health Check
            if (world.Has<Health>(target))
            {
                var hp = world.Get<Health>(target);
                // Logic: Maybe only show exact HP if you are close? 
                // For now, show exact.
                info["Health"] = $"{hp.Current}/{hp.Max}";
            }

            // 3. Stat Analysis (Simulated "Class" Logic)
            // If the Viewer has High Intelligence (>12), they can analyze stats.
            if (world.Has<Stats>(viewer))
            {
                var viewerStats = world.Get<Stats>(viewer);
                if (viewerStats.Intelligence > 12)
                {
                    if (world.Has<Stats>(target))
                    {
                        var targetStats = world.Get<Stats>(target);
                        info["Analysis"] = $"Target has {targetStats.Strength} STR";
                    }
                    else
                    {
                        info["Analysis"] = "Target has no stats.";
                    }
                }
            }

            return info;
        }
    }
}