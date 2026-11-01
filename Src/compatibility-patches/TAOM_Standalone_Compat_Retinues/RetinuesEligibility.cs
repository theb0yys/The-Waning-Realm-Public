using Retinues.Behaviors.Agents;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace TaomRetinuesCompat
{
    internal static class RetinuesEligibility
    {
        internal static bool IsManagedCombatTroop(AgentBuildData data)
        {
            if (data == null || !AgentSpawnResolver.IsCombatMission) return false;
            CharacterObject character = data.AgentCharacter as CharacterObject ?? data.AgentOrigin?.Troop as CharacterObject;
            if (character == null || character.IsHero) return false;
            WCharacter troop = WBase<WCharacter, CharacterObject>.Get(character);
            return troop != null && (troop.IsCustom || troop.IsEdited);
        }
    }
}
