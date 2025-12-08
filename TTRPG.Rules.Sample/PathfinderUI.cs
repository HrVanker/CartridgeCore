using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Core.DTOs;
using TTRPG.Core.Engine;
using TTRPG.Shared.Components;

namespace TTRPG.Rules.Pathfinder
{
    public class PathfinderUI : IUIProvider
    {
        public Dictionary<string, string> GetInspectionDetails(World world, Entity viewer, Entity target)
        {
            var info = new Dictionary<string, string>();
            info["Entity ID"] = target.Id.ToString();
            if (world.Has<Health>(target))
            {
                var hp = world.Get<Health>(target);
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
        public CharacterSheetData GetCharacterSheet(World world, Entity entity)
        {
            var sheet = new CharacterSheetData();
            sheet.Name = "Unknown Hero"; // We'll add NameComponent later

            // 1. Attributes
            if (world.Has<Attributes>(entity))
            {
                var attrs = world.Get<Attributes>(entity);
                var list = new List<StatEntry>();

                list.Add(new StatEntry { Label = "STR", Value = $"{attrs.Strength} ({attrs.GetModifier(attrs.Strength):+0;-#})" });
                list.Add(new StatEntry { Label = "DEX", Value = $"{attrs.Dexterity} ({attrs.GetModifier(attrs.Dexterity):+0;-#})" });
                // Add others...

                sheet.Categories["Attributes"] = list;
            }

            // 2. Combat
            if (world.Has<DerivedStats>(entity))
            {
                var stats = world.Get<DerivedStats>(entity);
                var list = new List<StatEntry>();

                list.Add(new StatEntry { Label = "AC", Value = stats.ArmorClass.ToString() });
                list.Add(new StatEntry { Label = "Speed", Value = stats.Speed.ToString() });

                sheet.Categories["Combat"] = list;
            }

            return sheet;
        }
    }
}