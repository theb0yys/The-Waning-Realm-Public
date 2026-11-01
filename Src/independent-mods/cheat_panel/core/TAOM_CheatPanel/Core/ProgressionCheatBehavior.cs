using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TAOM_CheatPanel.Settings;

namespace TAOM_CheatPanel.Runtime
{
    internal sealed class ProgressionCheatBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, ReconcileEnable);
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnSkillGained);
            CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkOpened);
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnLevelGained);
        }

        public override void SyncData(IDataStore dataStore) { }

        private static bool Enabled => CheatPanelSettings.Instance.Enabled && CheatPanelSettings.Instance.TakeBothPerks;

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ReconcileEnable();
        }

        private void ReconcileEnable()
        {
            // Catalog-sized, main-hero-only reconciliation also catches a toggle
            // off/on between hourly callbacks and native perk resets.
            if (Enabled) GrantEarnedPairs(Hero.MainHero, null);
        }

        private void OnSkillGained(Hero hero, SkillObject skill, int change, bool shouldNotify)
        {
            if (hero != null && hero == Hero.MainHero && Enabled) GrantEarnedPairs(hero, skill);
        }

        private static void GrantEarnedPairs(Hero hero, SkillObject skill)
        {
            if (hero?.HeroDeveloper == null) return;
            foreach (PerkObject perk in PerkObject.All)
                if (perk != null && perk.AlternativePerk != null && (skill == null || perk.Skill == skill))
                    GrantIfEarned(hero, perk);
        }

        private void OnPerkOpened(Hero hero, PerkObject perk)
        {
            if (hero != null && hero == Hero.MainHero && Enabled && perk != null)
                GrantIfEarned(hero, perk.AlternativePerk);
        }

        private static void GrantIfEarned(Hero hero, PerkObject perk)
        {
            // Native sets the bit before publishing PerkOpened. Checking it also
            // terminates the nested event when adding the alternative perk.
            if (perk != null && perk.Skill != null && hero.HeroDeveloper != null &&
                hero.GetSkillValue(perk.Skill) >= perk.RequiredSkillValue && !hero.GetPerkValue(perk))
                hero.HeroDeveloper.AddPerk(perk);
        }

        private void OnLevelGained(Hero hero, bool shouldNotify)
        {
            CheatPanelSettings settings = CheatPanelSettings.Instance;
            if (hero == null || hero != Hero.MainHero || hero.HeroDeveloper == null || !settings.Enabled) return;
            hero.HeroDeveloper.UnspentAttributePoints = ProgressionRules.AddBonusPoints(
                hero.HeroDeveloper.UnspentAttributePoints, settings.ExtraAttributePointsPerLevel);
            hero.HeroDeveloper.UnspentFocusPoints = ProgressionRules.AddBonusPoints(
                hero.HeroDeveloper.UnspentFocusPoints, settings.ExtraFocusPointsPerLevel);
        }
    }
}
