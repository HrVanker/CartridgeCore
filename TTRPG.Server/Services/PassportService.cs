using Arch.Core;
using Arch.Core.Extensions;
using TTRPG.Shared.Components; // Needs access to specific components to read them
using TTRPG.Shared.DTOs;

namespace TTRPG.Server.Services
{
    public class PassportService
    {
        // EXPORT: Convert Active Entity -> Passport DTO
        public Passport CreatePassport(Entity entity, string campaignId)
        {
            var passport = new Passport
            {
                SourceCampaignId = campaignId,
                // In a real scenario, we'd have a NameComponent. 
                // For now, we'll placeholder it or grab from a component if exists.
                CharacterName = "Unknown Traveler"
            };

            // 1. Extract Stats (if present)
            if (entity.Has<Attributes>())
            {
                var stats = entity.Get<Attributes>();
                passport.RawIntValues["Stats_Strength"] = stats.Strength;
                passport.RawIntValues["Stats_Dexterity"] = stats.Dexterity;
            }

            // 2. Extract Health (if present)
            if (entity.Has<Health>())
            {
                var health = entity.Get<Health>();
                passport.RawIntValues["Health_Current"] = health.Current;
                passport.RawIntValues["Health_Max"] = health.Max;
            }

            // 3. Extract Position (as an example of logic-agnostic storage)
            if (entity.Has<Position>())
            {
                var pos = entity.Get<Position>();
                passport.RawIntValues["World_X"] = pos.X;
                passport.RawIntValues["World_Y"] = pos.Y;
            }

            return passport;
        }
    }
}