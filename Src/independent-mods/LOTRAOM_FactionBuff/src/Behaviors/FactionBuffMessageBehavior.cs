// ═══════════════════════════════════════════════════════════════════════════
//  LOTRAOM_FactionBuff  ·  FactionBuffMessageBehavior.cs  (v6)
//
//  Message pipeline (in order, once per in-game day):
//    1.  Season transition   — per-culture, all four seasons, fires on entry day only
//    2.  Streak tracking     — updates counters before combined fires
//    3.  Combined / Streak   — exits early; skips 4–12 when in Peak or Dire state
//    4.  Food                — tier-specific pool + scale-aware suffix
//    5.  Prisoner            — three tiers, alignment-coloured
//    6.  Hero conflict/synergy — cross-alignment tension or named-lord boost (throttled)
//    7.  Terrain             — home/foreign, fires only on terrain change
//    8.  Army ally presence  — fires only when ally set changes
//    9.  Battle victory      — fires once on next tick after OnPlayerBattleEndEvent
//   10.  Political status    — fires on change between Ruler / Vassal / Independent
//   11.  Renown milestone    — fires once when crossing a tier boundary upward
//   12.  Wound state         — fires every WOUND_INTERVAL_DAYS while player is wounded
//
//  Persisted: _lastMessageDay, _lastKnownSeasonInt, _peakStreak, _direStreak,
//             _lastKnownTerrainInt, _lastArmyCultureHash, _lastHeroConflictDay,
//             _lastPoliticalStatusInt, _lastRenownTierInt,
//             _pendingVictory, _lastWoundedDay
//
//  Bug fixes vs v5:
//    · Terrain fires on change only (persisted _lastKnownTerrainInt)
//    · Army ally fires on set change only (persisted _lastArmyCultureHash via XOR hash)
//    · Hero conflict throttled to once per HERO_CONFLICT_INTERVAL days
//    · GetCurrentSeason() uses long cast before modulo (precision fix for high campaign age)
// ═══════════════════════════════════════════════════════════════════════════
using System;
using LOTRAOM.FactionBuff.Data;
using LOTRAOM.FactionBuff.Evaluation;
using LOTRAOM.FactionBuff.UI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace LOTRAOM.FactionBuff.Behaviors
{
    public sealed class FactionBuffMessageBehavior : CampaignBehaviorBase
    {
        // ── Persisted state ───────────────────────────────────────────────────
        private double _lastMessageDay        = -1.0;
        private int    _lastKnownSeasonInt    = -1;
        private int    _peakStreak            =  0;
        private int    _direStreak            =  0;
        private int    _lastKnownTerrainInt   = -1;   // TerrainContext as int; -1 = unset
        private int    _lastArmyCultureHash   =  0;   // XOR hash of current ally set
        private double _lastHeroConflictDay   = -1.0; // throttle hero conflict to N days
        private int    _lastPoliticalStatusInt = -1;  // PoliticalStatus as int; -1 = unset
        private int    _lastRenownTierInt     = -1;   // RenownTier as int; -1 = seeding
        private bool   _pendingVictory        = false;
        private double _lastWoundedDay        = -1.0;
        private double _lastStrategicSummaryDay = -999.0;
        private int    _doctrineModeInt       = (int)FactionDoctrineMode.Balanced;
        private bool   _doctrineEffectsEnabled = true;
        private bool   _doctrineReportsEnabled = true;

        private const int WOUND_INTERVAL_DAYS       = 3;
        private const int HERO_CONFLICT_INTERVAL    = 7; // don't nag every day about alignment

        // ── Event registration ────────────────────────────────────────────────
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, OnDailyTickParty);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            if (starter == null || !FactionBuffSettings.DoctrineControlMenusEnabled) return;

            TryAddDoctrineMenuOptions(starter, "camp");
            TryAddDoctrineMenuOptions(starter, "town");
            TryAddDoctrineMenuOptions(starter, "castle");
            TryAddDoctrineMenuOptions(starter, "village");
        }

        private void TryAddDoctrineMenuOptions(CampaignGameStarter starter, string menuId)
        {
            try
            {
                starter.AddGameMenuOption(
                    menuId,
                    "lotraom_factionbuff_status_" + menuId,
                    "Open faction doctrine board",
                    args => HasPlayerFactionProfile(),
                    args => OpenDoctrineBoardOrFallback(),
                    false,
                    999,
                    false,
                    null);

                starter.AddGameMenuOption(
                    menuId,
                    "lotraom_factionbuff_mode_" + menuId,
                    "Change faction doctrine stance",
                    args => HasPlayerFactionProfile(),
                    args => CycleDoctrineMode(),
                    false,
                    1000,
                    false,
                    null);

                starter.AddGameMenuOption(
                    menuId,
                    "lotraom_factionbuff_effects_" + menuId,
                    "Toggle doctrine effects",
                    args => HasPlayerFactionProfile(),
                    args => ToggleDoctrineEffects(),
                    false,
                    1001,
                    false,
                    null);

                starter.AddGameMenuOption(
                    menuId,
                    "lotraom_factionbuff_reports_" + menuId,
                    "Toggle doctrine reports",
                    args => HasPlayerFactionProfile(),
                    args => ToggleDoctrineReports(),
                    false,
                    1002,
                    false,
                    null);
            }
            catch
            {
                // Some campaign menus are absent in certain game modes or mod lists.
            }
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (mapEvent == null || !mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner) return;
            if (mapEvent.WinningSide == mapEvent.PlayerSide) _pendingVictory = true;
        }

        // ── Core per-party tick ───────────────────────────────────────────────
        private void OnDailyTickParty(MobileParty party)
        {
            double today = Math.Floor(CampaignTime.Now.ToDays);
            if (today <= _lastMessageDay) return;
            if (!IsPlayerParty(party)) return;

            CultureObject? culture = party.Party?.Culture;
            if (culture == null) return;

            string cultureId = culture.StringId;
            if (!FactionData.Profiles.TryGetValue(cultureId, out CultureProfile profile))
            {
#if DEBUG
                Display($"[FactionBuff DEBUG] No profile for culture '{cultureId}'.", 0xFFFF8800u);
#endif
                return;
            }

            FactionExtras? extras = null;
            FactionExtrasData.Extras.TryGetValue(cultureId, out extras);

            PartyBuffState state         = DailyStateEvaluator.Evaluate(party, profile);
            int currentSeasonInt         = (int)state.CurrentSeason;
            int currentTerrainInt        = (int)state.Terrain;
            int currentAllyHash          = ComputeAllyHash(state.ArmyAllyCultureIds);
            int currentPoliticalInt      = (int)state.Political;
            int currentRenownInt         = (int)state.Renown;

            // ─ 1. Season transition ───────────────────────────────────────────
            if (currentSeasonInt != _lastKnownSeasonInt)
            {
                _lastKnownSeasonInt = currentSeasonInt;
                ShowSeasonTransition(profile, state.CurrentSeason);
            }

            // ─ 2. Streak tracking ─────────────────────────────────────────────
            if      (state.IsCombinedPeak) { _peakStreak++; _direStreak = 0; }
            else if (state.IsCombinedDire) { _direStreak++; _peakStreak = 0; }
            else                           { _peakStreak = 0; _direStreak = 0; }

            FactionBuffStrategicSnapshot snapshot =
                FactionBuffStrategicScorer.Create(profile, state, party, _peakStreak, _direStreak);
            FactionBuffPublisher.Update(snapshot);

            if (FactionBuffSettings.DoctrineReportsEnabled &&
                today >= _lastStrategicSummaryDay + FactionBuffSettings.DoctrineReportIntervalDays)
            {
                _lastStrategicSummaryDay = today;
                ShowStrategicSummary(snapshot);
            }

            // ─ 3. Combined / streak — exits early ────────────────────────────
            if (state.IsCombinedPeak || state.IsCombinedDire)
            {
                bool streak = state.IsCombinedPeak
                    ? _peakStreak >= FactionData.STREAK_THRESHOLD
                    : _direStreak >= FactionData.STREAK_THRESHOLD;

                if (streak) ShowStreakMessage(profile, state.IsCombinedPeak);
                else        ShowCombinedMessage(profile, state);

                _lastMessageDay = today;
                return;
            }

            // ─ 4. Food ────────────────────────────────────────────────────────
            if (state.Food != FoodTier.None)
                ShowFoodMessage(profile, state.Food, state.Scale, party.MemberRoster.TotalManCount);

            // ─ 5. Prisoner ────────────────────────────────────────────────────
            ShowPrisonerMessage(profile, state.Prisoner);

            // ─ 6. Hero conflict / synergy (conflict throttled) ────────────────
            if (state.HeroConflict && today >= _lastHeroConflictDay + HERO_CONFLICT_INTERVAL)
            {
                _lastHeroConflictDay = today;
                ShowHeroConflictMessage(IsGoodAlignedCulture(party.LeaderHero?.Culture?.StringId));
            }
            else if (state.HeroSynergy)
            {
                ShowHeroSynergyMessage(party.LeaderHero?.Name?.ToString());
            }

            // ─ 7. Terrain — only on change ────────────────────────────────────
            if (currentTerrainInt != _lastKnownTerrainInt)
            {
                _lastKnownTerrainInt = currentTerrainInt;
                if (state.Terrain != TerrainContext.Default)
                    ShowTerrainMessage(profile, state.Terrain);
            }

            // ─ 8. Army ally — only on set change ─────────────────────────────
            if (currentAllyHash != _lastArmyCultureHash)
            {
                _lastArmyCultureHash = currentAllyHash;
                foreach (string id in state.ArmyAllyCultureIds)
                    ShowArmyAllyMessage(id);
            }

            // ─ 9. Battle victory ──────────────────────────────────────────────
            if (_pendingVictory)
            {
                _pendingVictory = false;
                if (extras != null) ShowVictoryMessage(extras);
            }

            // ─ 10. Political status change ────────────────────────────────────
            if (currentPoliticalInt != _lastPoliticalStatusInt)
            {
                bool isFirstSeed = _lastPoliticalStatusInt == -1;
                _lastPoliticalStatusInt = currentPoliticalInt;
                if (!isFirstSeed && extras != null)
                    ShowPoliticalMessage(extras, state.Political);
            }

            // ─ 11. Renown milestone (upward only) ─────────────────────────────
            if (_lastRenownTierInt == -1)
            {
                _lastRenownTierInt = currentRenownInt; // seed silently on first eval
            }
            else if (currentRenownInt > _lastRenownTierInt)
            {
                _lastRenownTierInt = currentRenownInt;
                if (extras != null) ShowRenownMessage(extras, state.Renown);
            }

            // ─ 12. Wound state (throttled) ────────────────────────────────────
            if (state.IsPlayerWounded)
            {
                if (today >= _lastWoundedDay + WOUND_INTERVAL_DAYS)
                {
                    _lastWoundedDay = today;
                    if (extras != null) ShowWoundedMessage(extras);
                }
            }
            else
            {
                _lastWoundedDay = -1.0; // reset so it re-fires promptly if re-wounded
            }

            _lastMessageDay = today;
        }

        // ── Message helpers ───────────────────────────────────────────────────

        private static string? PickKey(string[] keys)
        {
            if (keys == null || keys.Length == 0) return null;
            int day = Campaign.Current != null ? (int)Math.Floor(CampaignTime.Now.ToDays) : 0;
            return keys[Math.Abs(day) % keys.Length];
        }

        private static void ShowFoodMessage(CultureProfile profile, FoodTier tier,
                                             PartyScale scale, int memberCount)
        {
            string? key = PickKey(profile.Food.KeysForTier(tier));
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text == null) return;

            uint colour = tier == FoodTier.Scarce             ? FactionData.COLOUR_FOOD_SCARCE
                        : profile.Alignment == Alignment.Evil ? FactionData.COLOUR_FOOD_EVIL
                        :                                        FactionData.COLOUR_FOOD_GOOD;

            string tierSuffix = "";
            if (tier == FoodTier.Scarce)
            { TextObject t = GameTexts.FindText(FactionData.KEY_TIER_SCARCE); if (t != null) tierSuffix = " " + t; }
            else if (tier == FoodTier.Surplus)
            { TextObject t = GameTexts.FindText(FactionData.KEY_TIER_SURPLUS); if (t != null) tierSuffix = " " + t; }

            // Scale suffix intentionally hardcoded — terse numeric bracket, not a localised string.
            Display(text.ToString() + tierSuffix + BuildScaleSuffix(tier, scale, memberCount), colour);
        }

        private static string BuildScaleSuffix(FoodTier tier, PartyScale scale, int n)
        {
            if (tier == FoodTier.Scarce)
            {
                if (scale == PartyScale.Horde)    return $" [{n} men — supply crisis]";
                if (scale == PartyScale.Host)     return $" [{n} men — rations critical]";
                if (scale == PartyScale.Skirmish) return  " [small company — lean days]";
            }
            else if (tier == FoodTier.Surplus && scale == PartyScale.Horde)
                return $" [{n} men — abundantly supplied]";
            return "";
        }

        private static void ShowPrisonerMessage(CultureProfile profile, PrisonerState state)
        {
            string[] pool; uint colour;
            switch (state)
            {
                case PrisonerState.High:
                    pool = profile.Prisoner.HighKeys;
                    colour = IsGoodPrisonerState(profile.Alignment, state) ? FactionData.COLOUR_PRI_BUFF : FactionData.COLOUR_PRI_DEB;
                    break;
                case PrisonerState.Normal:
                    pool = profile.Prisoner.NormalKeys; colour = FactionData.COLOUR_PRI_NORMAL; break;
                case PrisonerState.Low:
                    pool = profile.Prisoner.LowKeys;
                    colour = IsGoodPrisonerState(profile.Alignment, state) ? FactionData.COLOUR_PRI_BUFF : FactionData.COLOUR_PRI_DEB;
                    break;
                default: return;
            }
            string? key = PickKey(pool);
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), colour);
        }

        private static void ShowCombinedMessage(CultureProfile profile, PartyBuffState s)
        {
            string[] pool = s.IsCombinedPeak ? profile.Combined.PeakKeys : profile.Combined.DireKeys;
            uint colour   = s.IsCombinedPeak ? FactionData.COLOUR_COMBINED_PEAK : FactionData.COLOUR_COMBINED_DIRE;
            string? key = PickKey(pool);
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), colour);
        }

        private static void ShowStreakMessage(CultureProfile profile, bool isPeak)
        {
            string[] pool = isPeak ? profile.Streak.PeakKeys : profile.Streak.DireKeys;
            uint colour   = isPeak ? FactionData.COLOUR_STREAK_PEAK : FactionData.COLOUR_STREAK_DIRE;
            string? key = PickKey(pool);
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), colour);
        }

        private static void ShowSeasonTransition(CultureProfile profile, Season season)
        {
            string? key = PickKey(profile.Season.GetKeys(season));
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), FactionData.COLOUR_SEASON_TRANS);
        }

        private static void ShowHeroConflictMessage(bool heroIsGood)
        {
            string baseKey = heroIsGood ? FactionData.KEY_HERO_TENSION_GOOD_IN_EVIL
                                        : FactionData.KEY_HERO_TENSION_EVIL_IN_GOOD;
            int day = Campaign.Current != null ? (int)Math.Floor(CampaignTime.Now.ToDays) : 0;
            TextObject? text = GameTexts.FindText($"{baseKey}_{Math.Abs(day) % 3}");
            if (text != null) Display(text.ToString(), FactionData.COLOUR_HERO_TENSION);
        }

        private static void ShowHeroSynergyMessage(string? heroName)
        {
            int day = Campaign.Current != null ? (int)Math.Floor(CampaignTime.Now.ToDays) : 0;
            TextObject? template = GameTexts.FindText($"{FactionData.KEY_HERO_SYNERGY_NAMED}_{Math.Abs(day) % 3}");
            if (template == null) return;
            if (!string.IsNullOrEmpty(heroName)) template.SetTextVariable("HERO", heroName);
            Display(template.ToString(), FactionData.COLOUR_HERO_SYNERGY);
        }

        private static void ShowTerrainMessage(CultureProfile profile, TerrainContext terrain)
        {
            string[]? pool; uint colour;
            if      (profile.Terrain.IsHome(terrain))    { pool = profile.Terrain.HomeKeys;    colour = FactionData.COLOUR_TERRAIN_HOME; }
            else if (profile.Terrain.IsForeign(terrain)) { pool = profile.Terrain.ForeignKeys; colour = FactionData.COLOUR_TERRAIN_AWAY; }
            else return;
            if (pool == null || pool.Length == 0) return;
            string? key = PickKey(pool);
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), colour);
        }

        private static void ShowArmyAllyMessage(string allyCultureId)
        {
            int day = Campaign.Current != null ? (int)Math.Floor(CampaignTime.Now.ToDays) : 0;
            TextObject? text = GameTexts.FindText($"lotr_army_ally_{allyCultureId}_{Math.Abs(day) % 3}");
            if (text != null) Display(text.ToString(), FactionData.COLOUR_ARMY_ALLY);
        }

        private static void ShowVictoryMessage(FactionExtras extras)
        {
            string? key = PickKey(extras.Victory.Keys);
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), FactionExtrasData.COLOUR_VICTORY);
        }

        private static void ShowPoliticalMessage(FactionExtras extras, PoliticalStatus status)
        {
            string? key = PickKey(extras.Political.GetKeys(status));
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), FactionExtrasData.COLOUR_POLITICAL);
        }

        private static void ShowRenownMessage(FactionExtras extras, RenownTier tier)
        {
            string? key = PickKey(extras.Renown.GetKeys(tier));
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), FactionExtrasData.COLOUR_RENOWN);
        }

        private static void ShowWoundedMessage(FactionExtras extras)
        {
            string? key = PickKey(extras.Wound.Keys);
            TextObject? text = key != null ? GameTexts.FindText(key) : null;
            if (text != null) Display(text.ToString(), FactionExtrasData.COLOUR_WOUNDED);
        }

        private static void ShowStrategicSummary(FactionBuffStrategicSnapshot snapshot)
        {
            if (snapshot == null) return;

            uint colour = snapshot.IsPeak ? FactionData.COLOUR_COMBINED_PEAK
                        : snapshot.IsDire ? FactionData.COLOUR_COMBINED_DIRE
                        : snapshot.StrategicScore >= 75 ? FactionData.COLOUR_STREAK_PEAK
                        : snapshot.StrategicScore < 40 ? FactionData.COLOUR_COMBINED_DIRE
                        : FactionData.COLOUR_ARMY_ALLY;

            string moraleSign = snapshot.EstimatedMoraleDelta >= 0f ? "+" : string.Empty;
            Display(
                "[Faction Doctrine] " + snapshot.CultureName +
                " | Mode " + snapshot.DoctrineLabel +
                " | " + snapshot.ReadinessBand +
                " | " + snapshot.StatusLabel + " " + snapshot.StrategicScore + "/100" +
                " | morale " + moraleSign + snapshot.EstimatedMoraleDelta.ToString("0.0") +
                " x" + snapshot.MoraleMultiplier.ToString("0.00") +
                " | effects " + (snapshot.EffectsEnabled ? "on" : "off") +
                " | Food " + snapshot.Food +
                " | Prisoners " + snapshot.Prisoner +
                " | " + snapshot.Season + "/" + snapshot.Terrain +
                " | " + snapshot.Political + "/" + snapshot.Renown,
                colour);

            Display(
                "[Doctrine Orders] " + snapshot.DoctrineDirective +
                " | Counsel " + snapshot.AdviceLine +
                " | " + snapshot.SupplyLine +
                " | " + snapshot.PrisonerLine +
                " | " + snapshot.MomentumLine,
                colour);
        }

        private void OpenDoctrineBoardOrFallback()
        {
            if (!DoctrineBoardScreen.TryOpen(this))
                ShowDoctrineStatusPanel();
        }

        public DoctrineBoardSnapshot BuildDoctrineBoardSnapshot()
        {
            FactionBuffStrategicSnapshot? snapshot = BuildCurrentSnapshot() ?? FactionBuffPublisher.LastSnapshot;
            DoctrineBoardSnapshot board = new DoctrineBoardSnapshot
            {
                TitleText = "Faction Doctrine",
                ModeButtonText = "Stance: " + FactionBuffSettings.ModeLabel,
                EffectsButtonText = "Effects: " + FactionBuffSettings.EffectsLabel,
                ReportsButtonText = "Reports: " + FactionBuffSettings.ReportsLabel,
                CadenceText = "Reports " + FactionBuffSettings.ReportsLabel +
                              " | every " + FactionBuffSettings.DoctrineReportIntervalDays + " days"
            };

            if (snapshot == null)
            {
                board.SignalText = "No doctrine snapshot has been evaluated yet.";
                board.ContextText = "Let campaign time advance with the player party active.";
                board.EmptyText = "Faction doctrine has no current party state.";
                board.ShowEmptyText = true;
                board.ScoreText = "--/100";
                board.ReadinessText = "awaiting state";
                board.MoraleText = "morale --";
                board.SummaryLineOne = "No culture profile available";
                board.SummaryLineTwo = "No supply or prisoner signal";
                board.SummaryLineThree = "No strategic directive";
                board.DirectiveText = "Open again after a daily tick or move with the player party.";
                return board;
            }

            string moraleSign = snapshot.EstimatedMoraleDelta >= 0f ? "+" : string.Empty;
            board.SignalText = snapshot.CultureName + " | " + snapshot.StatusLabel + " " +
                               snapshot.StrategicScore + "/100 | " + snapshot.ReadinessBand;
            board.ContextText = snapshot.Political + " | " + snapshot.Renown + " | " +
                                snapshot.Season + " / " + snapshot.Terrain;
            board.ScoreText = snapshot.StrategicScore + "/100";
            board.ReadinessText = snapshot.ReadinessBand;
            board.MoraleText = moraleSign + snapshot.EstimatedMoraleDelta.ToString("0.0") +
                               " x" + snapshot.MoraleMultiplier.ToString("0.00");
            board.SummaryLineOne = "Doctrine: " + snapshot.DoctrineLabel +
                                   " | effects " + (snapshot.EffectsEnabled ? "active" : "suspended");
            board.SummaryLineTwo = "Supply: " + snapshot.SupplyLine;
            board.SummaryLineThree = "Prisoners: " + snapshot.PrisonerLine;
            board.DirectiveText = snapshot.DoctrineDirective + " | " + snapshot.AdviceLine;
            board.IsPeak = snapshot.IsPeak || snapshot.StrategicScore >= 80;
            board.IsDire = snapshot.IsDire || snapshot.StrategicScore < 40;
            board.IsReady = !board.IsPeak && !board.IsDire;

            AddSignal(board, "Supply", snapshot.Food.ToString(), snapshot.SupplyLine,
                snapshot.Food == FoodTier.Surplus || snapshot.Food == FoodTier.Stocked,
                snapshot.Food == FoodTier.Scarce,
                snapshot.Food == FoodTier.None);
            AddSignal(board, "Prisoners", snapshot.Prisoner.ToString(), snapshot.PrisonerLine,
                !snapshot.PrisonerLine.Contains("burdensome") && !snapshot.PrisonerLine.Contains("thin"),
                snapshot.Prisoner == PrisonerState.Normal,
                snapshot.PrisonerLine.Contains("burdensome") || snapshot.PrisonerLine.Contains("thin"));
            AddSignal(board, "Momentum", snapshot.StatusLabel, snapshot.MomentumLine,
                snapshot.IsPeak || snapshot.StrategicScore >= 65,
                snapshot.StrategicScore >= 40 && snapshot.StrategicScore < 65,
                snapshot.IsDire || snapshot.StrategicScore < 40);
            AddSignal(board, "Ground", snapshot.Terrain.ToString(), snapshot.Season + " / " + snapshot.Terrain,
                snapshot.StrategicScore >= 65,
                snapshot.StrategicScore >= 40 && snapshot.StrategicScore < 65,
                snapshot.StrategicScore < 40);
            AddSignal(board, "Authority", snapshot.Political.ToString(), snapshot.Political + " / " + snapshot.Renown,
                snapshot.Political == PoliticalStatus.Ruler || snapshot.Political == PoliticalStatus.Vassal,
                snapshot.Political == PoliticalStatus.Independent,
                false);
            AddSignal(board, "Effects", snapshot.EffectsEnabled ? "active" : "suspended",
                "morale multiplier x" + snapshot.MoraleMultiplier.ToString("0.00"),
                snapshot.EffectsEnabled,
                !snapshot.EffectsEnabled,
                false);

            return board;
        }

        private FactionBuffStrategicSnapshot? BuildCurrentSnapshot()
        {
            MobileParty? party = MobileParty.MainParty;
            CultureObject? culture = party?.Party?.Culture;
            if (party == null || culture == null)
                return null;

            if (!FactionData.Profiles.TryGetValue(culture.StringId, out CultureProfile profile))
                return null;

            PartyBuffState state = DailyStateEvaluator.Evaluate(party, profile);
            FactionBuffStrategicSnapshot snapshot =
                FactionBuffStrategicScorer.Create(profile, state, party, _peakStreak, _direStreak);
            FactionBuffPublisher.Update(snapshot);
            return snapshot;
        }

        private static void AddSignal(
            DoctrineBoardSnapshot board,
            string name,
            string value,
            string detail,
            bool good,
            bool watch,
            bool bad)
        {
            board.Signals.Add(new DoctrineSignalSnapshot
            {
                Name = name,
                ValueText = value,
                DetailText = detail,
                StatusText = bad ? "risk" : (watch ? "watch" : "good"),
                IsGood = good && !bad,
                IsWatch = watch && !bad,
                IsBad = bad
            });
        }

        public void CycleDoctrineModeFromBoard()
        {
            FactionBuffSettings.CycleDoctrineMode();
            _doctrineModeInt = (int)FactionBuffSettings.DoctrineMode;
            Display(
                "[Faction Doctrine] Stance set to " + FactionBuffSettings.ModeLabel +
                " (morale x" + FactionBuffSettings.MoraleMultiplier.ToString("0.00") + ").",
                FactionData.COLOUR_ARMY_ALLY);
        }

        public void ToggleDoctrineEffectsFromBoard()
        {
            FactionBuffSettings.ToggleEffects();
            _doctrineEffectsEnabled = FactionBuffSettings.EffectsEnabled;
            Display(
                "[Faction Doctrine] Morale effects " + FactionBuffSettings.EffectsLabel + ".",
                FactionBuffSettings.EffectsEnabled ? FactionData.COLOUR_STREAK_PEAK : FactionData.COLOUR_COMBINED_DIRE);
        }

        public void ToggleDoctrineReportsFromBoard()
        {
            FactionBuffSettings.ToggleReports();
            _doctrineReportsEnabled = FactionBuffSettings.DoctrineReportsEnabled;
            Display(
                "[Faction Doctrine] Strategic reports " + FactionBuffSettings.ReportsLabel + ".",
                FactionData.COLOUR_ARMY_ALLY);
        }

        public void ShowStrategicSummaryFromBoard()
        {
            FactionBuffStrategicSnapshot? snapshot = BuildCurrentSnapshot() ?? FactionBuffPublisher.LastSnapshot;
            if (snapshot != null)
                ShowStrategicSummary(snapshot);
            else
                Display("[Faction Doctrine] No doctrine snapshot is available yet.", FactionData.COLOUR_COMBINED_DIRE);
        }

        private void CycleDoctrineMode()
        {
            FactionBuffSettings.CycleDoctrineMode();
            _doctrineModeInt = (int)FactionBuffSettings.DoctrineMode;
            Display(
                "[Faction Doctrine] Stance set to " + FactionBuffSettings.ModeLabel +
                " (morale x" + FactionBuffSettings.MoraleMultiplier.ToString("0.00") + ").",
                FactionData.COLOUR_ARMY_ALLY);
            ShowDoctrineStatusPanel();
        }

        private void ToggleDoctrineEffects()
        {
            FactionBuffSettings.ToggleEffects();
            _doctrineEffectsEnabled = FactionBuffSettings.EffectsEnabled;
            Display(
                "[Faction Doctrine] Morale effects " + FactionBuffSettings.EffectsLabel + ".",
                FactionBuffSettings.EffectsEnabled ? FactionData.COLOUR_STREAK_PEAK : FactionData.COLOUR_COMBINED_DIRE);
            ShowDoctrineStatusPanel();
        }

        private void ToggleDoctrineReports()
        {
            FactionBuffSettings.ToggleReports();
            _doctrineReportsEnabled = FactionBuffSettings.DoctrineReportsEnabled;
            Display(
                "[Faction Doctrine] Strategic reports " + FactionBuffSettings.ReportsLabel + ".",
                FactionData.COLOUR_ARMY_ALLY);
        }

        private static void ShowDoctrineStatusPanel()
        {
            FactionBuffStrategicSnapshot? snapshot = FactionBuffPublisher.LastSnapshot;
            string body = snapshot != null
                ? BuildDoctrineStatusBody(snapshot)
                : "No faction doctrine has been evaluated yet. Let the campaign clock advance with the player party active.";

            InformationManager.ShowInquiry(
                new InquiryData(
                    "Faction Doctrine",
                    body,
                    true,
                    false,
                    "Done",
                    string.Empty,
                    null,
                    null),
                false,
                false);
        }

        private static string BuildDoctrineStatusBody(FactionBuffStrategicSnapshot snapshot)
        {
            string moraleSign = snapshot.EstimatedMoraleDelta >= 0f ? "+" : string.Empty;
            return
                "Culture: " + snapshot.CultureName + Environment.NewLine +
                "Doctrine stance: " + snapshot.DoctrineLabel + Environment.NewLine +
                "Doctrine effects: " + (snapshot.EffectsEnabled ? "active" : "suspended") +
                " (morale x" + snapshot.MoraleMultiplier.ToString("0.00") + ")" + Environment.NewLine +
                "Report cadence: " + FactionBuffSettings.ReportsLabel +
                " / every " + FactionBuffSettings.DoctrineReportIntervalDays + " days" + Environment.NewLine +
                "Readiness: " + snapshot.ReadinessBand + Environment.NewLine +
                "Strategic status: " + snapshot.StatusLabel + " " + snapshot.StrategicScore + "/100" + Environment.NewLine +
                "Estimated morale: " + moraleSign + snapshot.EstimatedMoraleDelta.ToString("0.0") + Environment.NewLine +
                "Order: " + snapshot.DoctrineDirective + Environment.NewLine +
                "Counsel: " + snapshot.AdviceLine + Environment.NewLine +
                "Supply: " + snapshot.SupplyLine + Environment.NewLine +
                "Prisoners: " + snapshot.PrisonerLine + Environment.NewLine +
                "Momentum: " + snapshot.MomentumLine + Environment.NewLine +
                "Context: " + snapshot.Food + " food, " + snapshot.Prisoner + " prisoners, " +
                snapshot.Season + "/" + snapshot.Terrain + ", " +
                snapshot.Political + "/" + snapshot.Renown;
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private static void Display(string msg, uint argb)
            => InformationManager.DisplayMessage(new InformationMessage(msg, Color.FromUint(argb)));

        private static bool HasPlayerFactionProfile()
        {
            CultureObject? culture = MobileParty.MainParty?.Party?.Culture;
            return culture != null && FactionData.Profiles.ContainsKey(culture.StringId);
        }

        /// <summary>XOR hash of ally culture ID set — changes when set changes.</summary>
        private static int ComputeAllyHash(string[] ids)
        {
            int h = 0;
            if (ids != null) foreach (string id in ids) h ^= id?.GetHashCode() ?? 0;
            return h;
        }

        private static bool IsGoodAlignedCulture(string? id)
            => id != null && FactionData.Profiles.TryGetValue(id, out var p) && p.Alignment == Alignment.Good;

        private static bool IsGoodPrisonerState(Alignment a, PrisonerState s)
            => (a == Alignment.Evil && s == PrisonerState.High)
            || (a == Alignment.Good && s == PrisonerState.Low);

        private static bool IsPlayerParty(MobileParty party) => party.IsMainParty;

        public override void SyncData(IDataStore dataStore)
        {
            _doctrineModeInt = (int)FactionBuffSettings.DoctrineMode;
            _doctrineEffectsEnabled = FactionBuffSettings.EffectsEnabled;
            _doctrineReportsEnabled = FactionBuffSettings.DoctrineReportsEnabled;

            dataStore.SyncData("FactionBuff_lastMessageDay",    ref _lastMessageDay);
            dataStore.SyncData("FactionBuff_lastKnownSeason",   ref _lastKnownSeasonInt);
            dataStore.SyncData("FactionBuff_peakStreak",        ref _peakStreak);
            dataStore.SyncData("FactionBuff_direStreak",        ref _direStreak);
            dataStore.SyncData("FactionBuff_lastTerrain",       ref _lastKnownTerrainInt);
            dataStore.SyncData("FactionBuff_lastAllyHash",      ref _lastArmyCultureHash);
            dataStore.SyncData("FactionBuff_lastHeroConflict",  ref _lastHeroConflictDay);
            dataStore.SyncData("FactionBuff_lastPolitical",     ref _lastPoliticalStatusInt);
            dataStore.SyncData("FactionBuff_lastRenown",        ref _lastRenownTierInt);
            dataStore.SyncData("FactionBuff_pendingVictory",    ref _pendingVictory);
            dataStore.SyncData("FactionBuff_lastWoundedDay",    ref _lastWoundedDay);
            dataStore.SyncData("FactionBuff_lastStrategicSummaryDay", ref _lastStrategicSummaryDay);
            dataStore.SyncData("FactionBuff_doctrineMode",      ref _doctrineModeInt);
            dataStore.SyncData("FactionBuff_doctrineEffects",   ref _doctrineEffectsEnabled);
            dataStore.SyncData("FactionBuff_doctrineReports",   ref _doctrineReportsEnabled);

            FactionBuffSettings.ApplyPersisted(
                _doctrineModeInt,
                _doctrineEffectsEnabled,
                _doctrineReportsEnabled);
        }
    }
}
