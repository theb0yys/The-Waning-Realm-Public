using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using ContextualMessages.Config;
using ContextualMessages.UI;
using TAOM_ContextualMessages;
using ContentMessageLibrary = TAOM_ContextualMessages.MessageLibrary;

namespace ContextualMessages
{
    public sealed class ContextualMessageBehavior : CampaignBehaviorBase
    {
        // ── Colors ─────────────────────────────────────────────────────────
        private static readonly Color ColGold   = new Color(1.00f, 0.85f, 0.40f);
        private static readonly Color ColGreen  = new Color(0.50f, 0.85f, 0.50f);
        private static readonly Color ColRed    = new Color(0.85f, 0.35f, 0.35f);
        private static readonly Color ColGrey   = new Color(0.72f, 0.72f, 0.72f);
        private static readonly Color ColAmber  = new Color(0.92f, 0.70f, 0.25f);
        private static readonly Color ColBlue   = new Color(0.45f, 0.70f, 1.00f);
        private static readonly Color ColSilver = new Color(0.85f, 0.90f, 1.00f);
        private static readonly Color ColDusk   = new Color(0.95f, 0.65f, 0.35f);
        private static readonly Color ColPurple = new Color(0.75f, 0.50f, 0.95f);  // NEW — tournaments

        private enum SignalStance
        {
            Quiet = 0,
            Balanced = 1,
            Heightened = 2
        }

        private enum SignalFocus
        {
            All = 0,
            War = 1,
            Realm = 2,
            Party = 3,
            Ambient = 4,
            Critical = 5
        }

        private enum SignalChannel
        {
            War = 0,
            Realm = 1,
            Party = 2,
            Ambient = 3,
            Tournament = 4
        }

        // ══════════════════════════════════════════════════════════════════
        // GLOBAL rate-limiter state
        // ══════════════════════════════════════════════════════════════════
        private float _lastGlobalShowHour  = -999f;
        private float _hourWindowStart     = -999f;
        private int   _messagesThisHour    = 0;
        private int   _bonusLinesThisEvent = 0;
        private int   _signalsShown        = 0;
        private int   _signalsSuppressed   = 0;
        private int   _signalStanceInt     = (int)SignalStance.Balanced;
        private int   _signalFocusInt      = (int)SignalFocus.All;

        // ── Per-category cooldown tracking (campaign hours) ───────────────
        private float _lastAmbientHour      = -999f;
        private float _lastTravelHour       = -999f;
        private float _lastMoraleHour       = -999f;
        private float _lastDawnHour         = -999f;
        private float _lastDuskHour         = -999f;
        private float _lastBattleHour       = -999f;
        private float _lastRaidHour         = -999f;
        private float _lastSiegeHour        = -999f;
        private float _lastHeroDeathHour    = -999f;
        private float _lastWarPeaceHour     = -999f;
        private float _lastSettlementHour   = -999f;
        private float _lastArmyHour         = -999f;
        private float _lastPrisonerHour     = -999f;
        private float _lastClanChangeHour   = -999f;
        private float _lastKingdomHour      = -999f;
        private float _lastTournamentHour   = -999f;  // NEW
        private float _lastTournamentNoticeHour = -999f;
        private float _lastFugitiveHour     = -999f;  // NEW
        private float _lastFoodHour         = -999f;  // NEW
        private float _lastWoundedHour      = -999f;  // NEW
        private float _lastWeeklyHour       = -999f;  // NEW
        private float _lastCompanionHour    = -999f;
        private float _lastCaravanHour      = -999f;
        private float _lastFamilyHour       = -999f;
        private float _lastHideoutHour      = -999f;
        private float _lastQuestHour        = -999f;
        private float _lastRouteArrivalHour = -999f;
        private float _lastBridgePulseHour  = -999f;

        // ── Season tracking ───────────────────────────────────────────────
        private int  _lastSeasonIndex       = -1;
        private bool _seasonFiredThisSeason = false;

        // ── Campaign mood tracking ────────────────────────────────────────
        private int   _campaignTension       = 0;
        private int   _playerMomentum        = 0;
        private int   _realmInstability      = 0;
        private int   _supplyPressure        = 0;
        private float _lastMoodDecayHour     = -999f;
        private bool  _campaignLoadMessageShown = false;

        private static float NowHour      => (float)CampaignTime.Now.ToHours;
        private static float NowHourOfDay => (float)(CampaignTime.Now.ToHours % 24.0);

        // ═══════════════════════════════════════════════════════════════════
        // CampaignBehaviorBase
        // ═══════════════════════════════════════════════════════════════════

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
            CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLevelledUp);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
            CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
            CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
            CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
            // Expanded 1.4.15 event surface verified against local TaleWorlds.CampaignSystem.dll.
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
            CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, OnCharacterBecameFugitive);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.TournamentStarted.AddNonSerializedListener(this, OnTournamentStarted);
            CampaignEvents.OnPlayerJoinedTournamentEvent.AddNonSerializedListener(this, OnPlayerJoinedTournament);
            CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnCompanionAdded);
            CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
            CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnBeforeHeroesMarried);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.OnPartyAddedToMapEventEvent.AddNonSerializedListener(this, OnPartyAddedToMap);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.OnCaravanTransactionCompletedEvent.AddNonSerializedListener(this, OnCaravanTransactionCompleted);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnAfterSettlementEntered);
            CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("cm_lastGlobalShowHour",   ref _lastGlobalShowHour);
            // BUG FIX: persist the hourly-window state so a save/load doesn't
            // hand the player a fresh MaxMessagesPerHour budget and cause a burst.
            dataStore.SyncData("cm_hourWindowStart",      ref _hourWindowStart);
            dataStore.SyncData("cm_messagesThisHour",     ref _messagesThisHour);
            dataStore.SyncData("cm_lastAmbientHour",      ref _lastAmbientHour);
            dataStore.SyncData("cm_lastTravelHour",       ref _lastTravelHour);
            dataStore.SyncData("cm_lastMoraleHour",       ref _lastMoraleHour);
            dataStore.SyncData("cm_lastDawnHour",         ref _lastDawnHour);
            dataStore.SyncData("cm_lastDuskHour",         ref _lastDuskHour);
            dataStore.SyncData("cm_lastBattleHour",       ref _lastBattleHour);
            dataStore.SyncData("cm_lastRaidHour",         ref _lastRaidHour);
            dataStore.SyncData("cm_lastSiegeHour",        ref _lastSiegeHour);
            dataStore.SyncData("cm_lastHeroDeathHour",    ref _lastHeroDeathHour);
            dataStore.SyncData("cm_lastWarPeaceHour",     ref _lastWarPeaceHour);
            dataStore.SyncData("cm_lastSettlementHour",   ref _lastSettlementHour);
            dataStore.SyncData("cm_lastArmyHour",         ref _lastArmyHour);
            dataStore.SyncData("cm_lastPrisonerHour",     ref _lastPrisonerHour);
            dataStore.SyncData("cm_lastClanChangeHour",   ref _lastClanChangeHour);
            dataStore.SyncData("cm_lastKingdomHour",      ref _lastKingdomHour);
            dataStore.SyncData("cm_lastSeasonIndex",      ref _lastSeasonIndex);
            dataStore.SyncData("cm_seasonFiredThisSeason", ref _seasonFiredThisSeason);
            // NEW
            dataStore.SyncData("cm_lastTournamentHour",  ref _lastTournamentHour);
            dataStore.SyncData("cm_lastTournamentNoticeHour", ref _lastTournamentNoticeHour);
            dataStore.SyncData("cm_lastFugitiveHour",    ref _lastFugitiveHour);
            dataStore.SyncData("cm_lastFoodHour",        ref _lastFoodHour);
            dataStore.SyncData("cm_lastWoundedHour",     ref _lastWoundedHour);
            dataStore.SyncData("cm_lastWeeklyHour",      ref _lastWeeklyHour);
            dataStore.SyncData("cm_lastCompanionHour",   ref _lastCompanionHour);
            dataStore.SyncData("cm_lastCaravanHour",     ref _lastCaravanHour);
            dataStore.SyncData("cm_lastFamilyHour",      ref _lastFamilyHour);
            dataStore.SyncData("cm_lastHideoutHour",     ref _lastHideoutHour);
            dataStore.SyncData("cm_lastQuestHour",       ref _lastQuestHour);
            dataStore.SyncData("cm_lastRouteArrivalHour", ref _lastRouteArrivalHour);
            dataStore.SyncData("cm_lastBridgePulseHour", ref _lastBridgePulseHour);
            dataStore.SyncData("cm_signalsShown",        ref _signalsShown);
            dataStore.SyncData("cm_signalsSuppressed",   ref _signalsSuppressed);
            dataStore.SyncData("cm_signalStance",        ref _signalStanceInt);
            dataStore.SyncData("cm_signalFocus",         ref _signalFocusInt);
            dataStore.SyncData("cm_campaignTension",     ref _campaignTension);
            dataStore.SyncData("cm_playerMomentum",      ref _playerMomentum);
            dataStore.SyncData("cm_realmInstability",    ref _realmInstability);
            dataStore.SyncData("cm_supplyPressure",      ref _supplyPressure);
            dataStore.SyncData("cm_lastMoodDecayHour",   ref _lastMoodDecayHour);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Global rate-limited Show
        // BUG FIX: bonus-line counter now increments AFTER a successful
        //          display, not before — previously the slot was consumed
        //          even when the global gap or hourly cap blocked output.
        // ═══════════════════════════════════════════════════════════════════

        private bool ShowRL(string text, Color color, bool isBonusLine = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                _signalsSuppressed++;
                return false;
            }

            float now = NowHour;

            // Check quota first — but do NOT consume the slot yet.
            if (isBonusLine && _bonusLinesThisEvent >= EffectiveMaxBonusLinesPerEvent())
            {
                _signalsSuppressed++;
                return false;
            }

            if (now - _hourWindowStart >= 1f)
            {
                _hourWindowStart  = now;
                _messagesThisHour = 0;
            }
            if (_messagesThisHour >= EffectiveMaxMessagesPerHour())
            {
                _signalsSuppressed++;
                return false;
            }

            if (now - _lastGlobalShowHour < EffectiveGlobalMessageGapHours())
            {
                _signalsSuppressed++;
                return false;
            }

            InformationManager.DisplayMessage(
                new InformationMessage(new TextObject(text).ToString(), color));

            _lastGlobalShowHour = now;
            _messagesThisHour++;
            _signalsShown++;
            if (isBonusLine) _bonusLinesThisEvent++;  // consume slot only on success
            return true;
        }

        private void BeginEvent() { _bonusLinesThisEvent = 0; }

        private SignalStance CurrentStance
        {
            get
            {
                return Enum.IsDefined(typeof(SignalStance), _signalStanceInt)
                    ? (SignalStance)_signalStanceInt
                    : SignalStance.Balanced;
            }
        }

        private SignalFocus CurrentFocus
        {
            get
            {
                return Enum.IsDefined(typeof(SignalFocus), _signalFocusInt)
                    ? (SignalFocus)_signalFocusInt
                    : SignalFocus.All;
            }
        }

        private int EffectiveMaxMessagesPerHour()
        {
            switch (CurrentStance)
            {
                case SignalStance.Quiet:
                    return Math.Max(1, MessageSettings.MaxMessagesPerHour - 2);
                case SignalStance.Heightened:
                    return Math.Max(1, MessageSettings.MaxMessagesPerHour + 2);
                default:
                    return Math.Max(1, MessageSettings.MaxMessagesPerHour);
            }
        }

        private int EffectiveMaxBonusLinesPerEvent()
        {
            switch (CurrentStance)
            {
                case SignalStance.Quiet:
                    return 0;
                case SignalStance.Heightened:
                    return MessageSettings.MaxBonusLinesPerEvent + 1;
                default:
                    return MessageSettings.MaxBonusLinesPerEvent;
            }
        }

        private float EffectiveGlobalMessageGapHours()
        {
            switch (CurrentStance)
            {
                case SignalStance.Quiet:
                    return MessageSettings.GlobalMessageGapHours * 2.0f;
                case SignalStance.Heightened:
                    return MessageSettings.GlobalMessageGapHours * 0.6f;
                default:
                    return MessageSettings.GlobalMessageGapHours;
            }
        }

        private bool ShouldAllowChannel(SignalChannel channel, bool urgent)
        {
            SignalFocus focus = CurrentFocus;
            if (focus == SignalFocus.All)
                return true;
            if (urgent && focus == SignalFocus.Critical)
                return true;

            switch (focus)
            {
                case SignalFocus.War:
                    return channel == SignalChannel.War || (urgent && channel == SignalChannel.Party);
                case SignalFocus.Realm:
                    return channel == SignalChannel.Realm || (urgent && channel == SignalChannel.War);
                case SignalFocus.Party:
                    return channel == SignalChannel.Party || channel == SignalChannel.Tournament;
                case SignalFocus.Ambient:
                    return channel == SignalChannel.Ambient;
                case SignalFocus.Critical:
                    return false;
                default:
                    return true;
            }
        }

        private void CycleSignalStance()
        {
            int count = Enum.GetValues(typeof(SignalStance)).Length;
            _signalStanceInt = (_signalStanceInt + 1) % count;
        }

        private void CycleSignalFocus()
        {
            int count = Enum.GetValues(typeof(SignalFocus)).Length;
            _signalFocusInt = (_signalFocusInt + 1) % count;
        }

        private void ShowControlToast()
        {
            InformationManager.DisplayMessage(new InformationMessage(
                "[Contextual Messages] Stance " + CurrentStance +
                " | Focus " + CurrentFocus +
                " | " + BuildMoodLabel(),
                ColSilver));
        }

        private void AdjustMood(int tension, int momentum, int realm, int supply)
        {
            _campaignTension = ClampScore(_campaignTension + tension);
            _playerMomentum = ClampScore(_playerMomentum + momentum);
            _realmInstability = ClampScore(_realmInstability + realm);
            _supplyPressure = ClampScore(_supplyPressure + supply);
        }

        private void DecayMood(float nowHour)
        {
            if (_lastMoodDecayHour > -900f && nowHour < _lastMoodDecayHour + 24f)
                return;

            _lastMoodDecayHour = nowHour;
            _campaignTension = DecayScore(_campaignTension, 2);
            _playerMomentum = DecayScore(_playerMomentum, 2);
            _realmInstability = DecayScore(_realmInstability, 1);
            _supplyPressure = DecayScore(_supplyPressure, 2);
        }

        private void UpdateSupplyPressure()
        {
            int pressure = 0;
            try
            {
                var party = MobileParty.MainParty;
                if (party != null)
                {
                    if (party.FoodChange < 0f)
                    {
                        float days = party.Food / (-party.FoodChange);
                        if (days <= 1f) pressure += 35;
                        else if (days <= MessageSettings.FoodWarningDaysThreshold) pressure += 20;
                    }

                    if (party.MemberRoster != null)
                    {
                        int total = party.MemberRoster.TotalManCount;
                        if (total > 0)
                        {
                            float wounded = (float)party.MemberRoster.TotalWounded / total;
                            if (wounded >= 0.5f) pressure += 35;
                            else if (wounded >= MessageSettings.WoundedThresholdFraction) pressure += 20;
                        }
                    }

                    if (party.Morale <= 30f)
                        pressure += 15;
                }
            }
            catch { }

            _supplyPressure = ClampScore(Math.Max(_supplyPressure, pressure));
        }

        private static int ClampScore(int value)
        {
            if (value < 0) return 0;
            if (value > 100) return 100;
            return value;
        }

        private static int DecayScore(int value, int amount)
        {
            if (value <= 0) return 0;
            return Math.Max(0, value - amount);
        }

        // ═══════════════════════════════════════════════════════════════════
        // Session
        // ═══════════════════════════════════════════════════════════════════

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                ModBridge.Reset();
                TryAddStatusMenuOption(starter, "town");
                TryAddStatusMenuOption(starter, "castle");
                TryAddStatusMenuOption(starter, "village");
                TryAddStatusMenuOption(starter, "camp");
                TryAddStatusMenuOption(starter, "encounter");
                TryAddSignalControlOptions(starter, "town");
                TryAddSignalControlOptions(starter, "castle");
                TryAddSignalControlOptions(starter, "village");
                TryAddSignalControlOptions(starter, "camp");
                TryAddSignalControlOptions(starter, "encounter");
                TryShowCampaignLoadMessage();
            }
            catch (Exception ex) { Log(ex, "OnSessionLaunched"); }
        }

        private void TryShowCampaignLoadMessage()
        {
            if (_campaignLoadMessageShown || !MessageSettings.Enabled || !MessageSettings.ShowAmbientMessages)
                return;

            _campaignLoadMessageShown = true;
            BeginEvent();

            ShowSessionLoadLine(ContentMessageLibrary.Pick(GetCultureLoadPool(ResolvePlayerCultureId())), ColGold);
            ShowSessionLoadLine(ContentMessageLibrary.Pick(ContentMessageLibrary.CampaignLoadMood), ColSilver);

            _lastGlobalShowHour = NowHour;
        }

        private void ShowSessionLoadLine(string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _signalsSuppressed++;
                return;
            }

            InformationManager.DisplayMessage(
                new InformationMessage(new TextObject(text).ToString(), color));
            _signalsShown++;
        }

        private static string[] GetCultureLoadPool(string cultureId)
        {
            string id = NormalizeCultureId(cultureId);

            if (id.Contains("gondor")) return ContentMessageLibrary.CampaignLoadGondor;
            if (id.Contains("rohan")) return ContentMessageLibrary.CampaignLoadRohan;
            if (id.Contains("rivendell") || id.Contains("imladris")) return ContentMessageLibrary.CampaignLoadRivendell;
            if (id.Contains("lothlorien") || id.Contains("lorien") || id.Contains("galadhrim")) return ContentMessageLibrary.CampaignLoadLothlorien;
            if (id.Contains("mirkwood") || id.Contains("woodlandrealm") || id.Contains("silvan")) return ContentMessageLibrary.CampaignLoadMirkwood;
            if (id.Contains("erebor") || id.Contains("durin") || id.Contains("khazad") || id.Contains("dwarf")) return ContentMessageLibrary.CampaignLoadErebor;
            if (id.Contains("dale") || id.Contains("bard")) return ContentMessageLibrary.CampaignLoadDale;
            if (id.Contains("shire") || id.Contains("hobbit")) return ContentMessageLibrary.CampaignLoadShire;
            if (id.Contains("mordor") || id.Contains("sauron")) return ContentMessageLibrary.CampaignLoadMordor;
            if (id.Contains("isengard") || id.Contains("uruk")) return ContentMessageLibrary.CampaignLoadIsengard;
            if (id.Contains("gundabad")) return ContentMessageLibrary.CampaignLoadGundabad;
            if (id.Contains("dolguldur")) return ContentMessageLibrary.CampaignLoadDolGuldur;
            if (id.Contains("umbar") || id.Contains("corsair")) return ContentMessageLibrary.CampaignLoadUmbar;
            if (id.Contains("harad") || id.Contains("haradrim")) return ContentMessageLibrary.CampaignLoadHarad;
            if (id.Contains("rhun") || id.Contains("easterling")) return ContentMessageLibrary.CampaignLoadRhun;
            if (id.Contains("dunland") || id.Contains("dunlending")) return ContentMessageLibrary.CampaignLoadDunland;

            return ContentMessageLibrary.CampaignLoadUnknownCulture;
        }

        private static string ResolvePlayerCultureId()
        {
            string id = TryReadCultureId(Hero.MainHero?.CharacterObject);
            if (!string.IsNullOrEmpty(id))
                return id;

            id = TryReadCultureId(MobileParty.MainParty?.Party);
            if (!string.IsNullOrEmpty(id))
                return id;

            return TryReadCultureId(Hero.MainHero?.MapFaction);
        }

        private static string TryReadCultureId(object source)
        {
            if (source == null)
                return "";

            try
            {
                var cultureProperty = source.GetType().GetProperty("Culture");
                if (cultureProperty == null)
                    return "";

                var culture = cultureProperty.GetValue(source, null);
                if (culture == null)
                    return "";

                var stringIdProperty = culture.GetType().GetProperty("StringId");
                return stringIdProperty?.GetValue(culture, null) as string ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string NormalizeCultureId(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return "";

            var sb = new StringBuilder(cultureId.Length);
            for (int i = 0; i < cultureId.Length; i++)
            {
                char c = char.ToLowerInvariant(cultureId[i]);
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private void TryAddStatusMenuOption(CampaignGameStarter starter, string menuId)
        {
            try
            {
                starter.AddGameMenuOption(
                    menuId,
                    "contextual_messages_status_" + menuId,
                    "Open Contextual Signal Board",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                        return true;
                    },
                    args => OpenSignalBoardOrFallback(),
                    isLeave: false,
                    index: 998);
            }
            catch (Exception ex)
            {
                Debug.Print("[ContextualMessages] Could not add status option to " + menuId + ": " + ex.Message);
            }
        }

        private void TryAddSignalControlOptions(CampaignGameStarter starter, string menuId)
        {
            try
            {
                starter.AddGameMenuOption(
                    menuId,
                    "contextual_messages_stance_" + menuId,
                    "Cycle Message Stance",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return MessageSettings.Enabled;
                    },
                    args =>
                    {
                        CycleSignalStance();
                        ShowControlToast();
                    },
                    isLeave: false,
                    index: 997);

                starter.AddGameMenuOption(
                    menuId,
                    "contextual_messages_focus_" + menuId,
                    "Cycle Message Focus",
                    args =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                        return MessageSettings.Enabled;
                    },
                    args =>
                    {
                        CycleSignalFocus();
                        ShowControlToast();
                    },
                    isLeave: false,
                    index: 996);
            }
            catch (Exception ex)
            {
                Debug.Print("[ContextualMessages] Could not add control options to " + menuId + ": " + ex.Message);
            }
        }

        internal void ShowStatusPanel()
        {
            try
            {
                InformationManager.ShowInquiry(new InquiryData(
                    "Contextual Messages",
                    BuildStatusPanelBody(),
                    isAffirmativeOptionShown: true,
                    isNegativeOptionShown: false,
                    affirmativeText: "Close",
                    negativeText: null,
                    affirmativeAction: null,
                    negativeAction: null),
                    pauseGameActiveState: true);
            }
            catch (Exception ex) { Log(ex, "ShowStatusPanel"); }
        }

        internal void OpenSignalBoardOrFallback()
        {
            try
            {
                if (ContextualSignalBoardScreen.TryOpen(this))
                    return;
            }
            catch (Exception ex)
            {
                Log(ex, "OpenSignalBoardOrFallback");
            }

            ShowStatusPanel();
        }

        public ContextualSignalBoardSnapshot BuildSignalBoardSnapshot()
        {
            float now = NowHour;
            DecayMood(now);
            UpdateSupplyPressure();

            int capacity = EffectiveMaxMessagesPerHour();
            bool enabled = MessageSettings.Enabled;
            bool capped = _messagesThisHour >= capacity;
            bool urgentFood = IsFoodCritical();
            bool urgentWounded = IsWoundedCritical();
            string moodLabel = BuildMoodLabel();
            int pressure = Math.Max(_campaignTension, Math.Max(_realmInstability, _supplyPressure));
            int balance = _playerMomentum - pressure;

            ContextualSignalBoardSnapshot board = new ContextualSignalBoardSnapshot
            {
                TitleText = "Contextual Signal Board",
                HeaderText = (enabled ? "Signals enabled" : "Signals disabled") +
                             " | stance " + CurrentStance +
                             " | focus " + CurrentFocus +
                             " | " + moodLabel,
                CadenceText = "This hour " + _messagesThisHour + " / " + capacity +
                              " | gap " + EffectiveGlobalMessageGapHours().ToString("0.00") +
                              " campaign hours | last " + FormatAgeHours(now - _lastGlobalShowHour),
                StanceButtonText = "Stance: " + CurrentStance,
                FocusButtonText = "Focus: " + CurrentFocus,
                MoodText = "Mood: " + moodLabel,
                TensionText = _campaignTension.ToString(),
                MomentumText = _playerMomentum.ToString(),
                RealmScoreText = _realmInstability.ToString(),
                SupplyScoreText = _supplyPressure.ToString(),
                AdviceText = BuildImmediateAdvice(),
                RealmText = BuildRealmPulse(),
                PartyText = BuildPartyPulse(),
                CategoryText = "Categories: " + BuildEnabledCategoryCount() + " active",
                BridgeText = BuildBridgeSummary(),
                EmptyText = "No contextual signal data has been collected yet.",
                ShowEmptyText = false,
                IsAlert = pressure >= 60 || urgentFood || urgentWounded,
                IsHopeful = balance >= 25,
                IsQuiet = CurrentStance == SignalStance.Quiet
            };

            AddSignal(board,
                "Budget",
                _messagesThisHour + " / " + capacity,
                "Global gap " + EffectiveGlobalMessageGapHours().ToString("0.00") +
                "h | last signal " + FormatAgeHours(now - _lastGlobalShowHour),
                capped ? "Capped" : "Open",
                enabled && !capped,
                enabled && capped,
                !enabled);

            AddSignal(board,
                "Flow",
                _signalsShown + " shown",
                _signalsSuppressed + " suppressed by pacing, focus, or empty pools",
                _signalsSuppressed > _signalsShown ? "Noisy" : "Stable",
                _signalsShown >= _signalsSuppressed,
                _signalsSuppressed > _signalsShown,
                false);

            AddSignal(board,
                "Tension",
                _campaignTension.ToString(),
                "War and danger pressure affecting the message tone",
                _campaignTension >= 70 ? "High" : (_campaignTension >= 40 ? "Watch" : "Calm"),
                _campaignTension < 40,
                _campaignTension >= 40 && _campaignTension < 70,
                _campaignTension >= 70);

            AddSignal(board,
                "Momentum",
                _playerMomentum.ToString(),
                "Recent victories, gains, and favorable movement",
                _playerMomentum >= 60 ? "Rising" : (_playerMomentum >= 25 ? "Present" : "Low"),
                _playerMomentum >= 25,
                _playerMomentum < 25,
                false);

            AddSignal(board,
                "Realm",
                _realmInstability.ToString(),
                BuildRealmPulse(),
                _realmInstability >= 70 ? "Unstable" : (_realmInstability >= 40 ? "Watch" : "Settled"),
                _realmInstability < 40,
                _realmInstability >= 40 && _realmInstability < 70,
                _realmInstability >= 70);

            AddSignal(board,
                "Supply",
                _supplyPressure.ToString(),
                BuildPartyPulse(),
                urgentFood ? "Food" : (urgentWounded ? "Wounded" : (_supplyPressure >= 45 ? "Watch" : "Ready")),
                _supplyPressure < 35 && !urgentFood && !urgentWounded,
                _supplyPressure >= 35 && !urgentFood && !urgentWounded,
                urgentFood || urgentWounded);

            AddSignal(board,
                "Categories",
                BuildEnabledCategoryCount() + " active",
                BuildCategoryLine(),
                BuildEnabledCategoryCount() > 0 ? "Linked" : "Muted",
                BuildEnabledCategoryCount() > 0,
                BuildEnabledCategoryCount() == 0,
                false);

            AddSignal(board,
                "TAOM Bridges",
                BuildBridgeCount() + " linked",
                BuildBridgeLine(),
                BuildBridgeCount() > 0 ? "Linked" : "None",
                BuildBridgeCount() > 0,
                BuildBridgeCount() == 0,
                false);

            board.ShowEmptyText = board.Signals.Count == 0;
            return board;
        }

        public void CycleSignalStanceFromBoard()
        {
            CycleSignalStance();
            ShowControlToast();
        }

        public void CycleSignalFocusFromBoard()
        {
            CycleSignalFocus();
            ShowControlToast();
        }

        public void ShowStatusPanelFromBoard()
        {
            ShowStatusPanel();
        }

        // ═══════════════════════════════════════════════════════════════════
        // Battle outcome
        // ═══════════════════════════════════════════════════════════════════

        private void OnMapEventEnded(MapEvent mapEvent)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowBattleMessages || !ShouldAllowChannel(SignalChannel.War, false)) return;
            if (mapEvent == null) return;

            float now = NowHour;
            if (now - _lastBattleHour < MessageSettings.BattleCooldownHours) return;

            try
            {
                BeginEvent();

                BattleSideEnum playerSide;
                if (!TryGetPlayerSide(mapEvent, out playerSide)) return;

                bool playerWon = mapEvent.WinningSide == playerSide;
                var  nearby    = NearestTownOrCastle(mapEvent.Position);

                if (playerWon)
                {
                    AdjustMood(-2, 8, 0, 0);
                    ShowRL(Pick(MessageLibrary.BattleWin), ColGreen);

                    if (nearby != null && MBRandom.RandomFloat < 0.5f)
                        ShowRL(string.Format(Pick(MessageLibrary.BattleVictoryNearSettlement), nearby.Name),
                               ColGreen, isBonusLine: true);
                }
                else
                {
                    AdjustMood(8, -6, 0, 0);
                    ShowRL(Pick(MessageLibrary.BattleLoss), ColRed);

                    if (nearby != null && MBRandom.RandomFloat < 0.5f)
                        ShowRL(string.Format(Pick(MessageLibrary.BattleDefeatNearSettlement), nearby.Name),
                               ColRed, isBonusLine: true);
                }

                _lastBattleHour = now;
            }
            catch (Exception ex) { Log(ex, "OnMapEventEnded"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Raid
        // ═══════════════════════════════════════════════════════════════════

        private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowRaidMessages || !ShouldAllowChannel(SignalChannel.War, true)) return;

            float now = NowHour;
            if (now - _lastRaidHour < MessageSettings.RaidCooldownHours) return;

            try
            {
                BeginEvent();

                // BUG FIX: check whether the player is the attacker (raider) so
                // we don't show observer-flavour lines when they are doing the raiding.
                bool playerIsAttacker = raidEvent?.MapEvent != null &&
                    TryGetPlayerSide(raidEvent.MapEvent, out BattleSideEnum playerSide) &&
                    playerSide == BattleSideEnum.Attacker;

                bool repelled = winnerSide == BattleSideEnum.Defender;

                if (playerIsAttacker)
                {
                    // Player raided — show attacker-perspective lines
                    if (repelled)
                    {
                        AdjustMood(5, -3, 0, 0);
                        ShowRL(Pick(MessageLibrary.RaidRepelledByDefenders), ColAmber);
                    }
                    else
                    {
                        AdjustMood(6, 4, 0, 0);
                        ShowRL(Pick(MessageLibrary.RaidSucceeded), ColGold);
                        ShowRL(Pick(MessageLibrary.RaidCompleted), ColAmber, isBonusLine: true);
                    }
                }
                else
                {
                    // Player is defender or observer
                    if (repelled)
                    {
                        AdjustMood(-2, 4, 0, 0);
                        ShowRL(Pick(MessageLibrary.RaidRepelled), ColGreen);
                    }
                    else
                    {
                        AdjustMood(9, -3, 1, 0);
                        ShowRL(Pick(MessageLibrary.VillageUnderRaid), ColAmber);
                        ShowRL(Pick(MessageLibrary.RaidCompleted), ColRed, isBonusLine: true);
                    }
                }

                _lastRaidHour = now;
            }
            catch (Exception ex) { Log(ex, "OnRaidCompleted"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Siege
        // ═══════════════════════════════════════════════════════════════════

        private void OnSiegeStarted(SiegeEvent siegeEvent)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowSiegeMessages || !ShouldAllowChannel(SignalChannel.War, true)) return;

            float now = NowHour;
            if (now - _lastSiegeHour < MessageSettings.SiegeCooldownHours) return;

            try
            {
                BeginEvent();
                AdjustMood(10, -2, 2, 0);

                var settlement = siegeEvent?.BesiegedSettlement;
                if (settlement != null)
                {
                    ShowRL(string.Format(Pick(MessageLibrary.SiegeStartedNamed), settlement.Name), ColAmber);
                    ShowRL(Pick(MessageLibrary.SiegeStarted), ColGrey, isBonusLine: true);
                }
                else
                {
                    ShowRL(Pick(MessageLibrary.SiegeStarted), ColAmber);
                }

                _lastSiegeHour = now;
            }
            catch (Exception ex) { Log(ex, "OnSiegeStarted"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Hero death
        // ═══════════════════════════════════════════════════════════════════

        private void OnHeroKilled(Hero victim, Hero killer,
            KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowHeroDeathMessages || !ShouldAllowChannel(SignalChannel.War, true)) return;
            if (victim == null || victim == Hero.MainHero) return;

            float now = NowHour;
            if (now - _lastHeroDeathHour < MessageSettings.HeroDeathCooldownHours) return;

            try
            {
                BeginEvent();

                bool isEnemy = IsEnemyOfPlayer(victim);
                bool isAlly  = IsAllyOfPlayer(victim);

                if (isEnemy)
                {
                    AdjustMood(-3, 6, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.HeroDeathNamed), victim.Name), ColGold);
                    ShowRL(Pick(MessageLibrary.EnemyHeroKilled), ColGreen, isBonusLine: true);
                    _lastHeroDeathHour = now;
                }
                else if (isAlly)
                {
                    AdjustMood(8, -5, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.HeroDeathNamed), victim.Name), ColRed);
                    ShowRL(Pick(MessageLibrary.AllyHeroKilled), ColGrey, isBonusLine: true);
                    _lastHeroDeathHour = now;
                }
            }
            catch (Exception ex) { Log(ex, "OnHeroKilled"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // War & Peace
        // ═══════════════════════════════════════════════════════════════════

        private void OnWarDeclared(IFaction f1, IFaction f2, DeclareWarAction.DeclareWarDetail detail)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPoliticalMessages || !ShouldAllowChannel(SignalChannel.Realm, true)) return;

            float now = NowHour;
            if (now - _lastWarPeaceHour < MessageSettings.WarPeaceCooldownHours) return;

            try
            {
                BeginEvent();
                AdjustMood(12, -2, 10, 0);
                ShowRL(string.Format(Pick(MessageLibrary.WarDeclaredNamed),
                    f1?.Name?.ToString() ?? "Unknown", f2?.Name?.ToString() ?? "Unknown"), ColRed);
                ShowRL(Pick(MessageLibrary.WarDeclared), ColAmber, isBonusLine: true);
                _lastWarPeaceHour = now;
            }
            catch (Exception ex) { Log(ex, "OnWarDeclared"); }
        }

        private void OnMakePeace(IFaction f1, IFaction f2, MakePeaceAction.MakePeaceDetail detail)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPoliticalMessages || !ShouldAllowChannel(SignalChannel.Realm, false)) return;

            float now = NowHour;
            if (now - _lastWarPeaceHour < MessageSettings.WarPeaceCooldownHours) return;

            try
            {
                BeginEvent();
                AdjustMood(-8, 4, -6, 0);
                ShowRL(string.Format(Pick(MessageLibrary.PeaceMadeNamed),
                    f1?.Name?.ToString() ?? "Unknown",
                    f2?.Name?.ToString() ?? "Unknown"), ColGreen);
                ShowRL(Pick(MessageLibrary.PeaceMade), ColGreen, isBonusLine: true);
                _lastWarPeaceHour = now;
            }
            catch (Exception ex) { Log(ex, "OnMakePeace"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Settlement owner changed
        // ═══════════════════════════════════════════════════════════════════

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim,
            Hero newOwner, Hero oldOwner, Hero capturingHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            // FIX: use ShowSettlementMessages — ownership can change diplomatically,
            // not just through sieges, so ShowSiegeMessages was the wrong gate.
            if (!MessageSettings.Enabled || !MessageSettings.ShowSettlementMessages || !ShouldAllowChannel(SignalChannel.Realm, true)) return;
            if (settlement == null) return;

            float now = NowHour;
            if (now - _lastSettlementHour < MessageSettings.SettlementCooldownHours) return;

            try
            {
                BeginEvent();

                bool playerGained = newOwner?.MapFaction == Hero.MainHero?.MapFaction;

                if (playerGained)
                {
                    AdjustMood(-2, 9, -1, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.SettlementCapturedNamed), settlement.Name), ColGold);
                    ShowRL(Pick(MessageLibrary.SettlementCaptured), ColGold, isBonusLine: true);
                }
                else
                {
                    AdjustMood(10, -7, 4, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.SettlementTakenNamed), settlement.Name), ColRed);
                    ShowRL(Pick(MessageLibrary.SettlementLost), ColRed, isBonusLine: true);
                }

                _lastSettlementHour = now;
            }
            catch (Exception ex) { Log(ex, "OnSettlementOwnerChanged"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Hero level up
        // ═══════════════════════════════════════════════════════════════════

        private void OnHeroLevelledUp(Hero hero, bool shouldShow)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowLevelUpMessages || !ShouldAllowChannel(SignalChannel.Party, false)) return;
            if (hero != Hero.MainHero) return;
            try
            {
                BeginEvent();
                AdjustMood(0, 3, 0, 0);
                ShowRL(Pick(MessageLibrary.HeroLevelUp), ColGold);
            }
            catch (Exception ex) { Log(ex, "OnHeroLevelledUp"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Army created / dispersed
        // ═══════════════════════════════════════════════════════════════════

        private void OnArmyCreated(Army army)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPoliticalMessages || !ShouldAllowChannel(SignalChannel.War, true)) return;
            if (army == null) return;

            float now = NowHour;
            if (now - _lastArmyHour < MessageSettings.ArmyCooldownHours) return;

            try
            {
                BeginEvent();
                if (army.LeaderParty?.MapFaction == Hero.MainHero?.MapFaction)
                {
                    AdjustMood(3, 5, 0, 0);
                    ShowRL(Pick(MessageLibrary.ArmyMobilised), ColGold);
                    _lastArmyHour = now;
                }
                else if (IsEnemyFaction(army.LeaderParty?.MapFaction))
                {
                    AdjustMood(9, -2, 0, 0);
                    ShowRL(Pick(MessageLibrary.EnemyArmyMobilised), ColRed);
                    _lastArmyHour = now;
                }
            }
            catch (Exception ex) { Log(ex, "OnArmyCreated"); }
        }

        private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPoliticalMessages || !ShouldAllowChannel(SignalChannel.War, false)) return;

            float now = NowHour;
            if (now - _lastArmyHour < MessageSettings.ArmyCooldownHours) return;

            try
            {
                BeginEvent();
                if (isPlayersArmy)
                {
                    AdjustMood(4, -3, 0, 0);
                    ShowRL(Pick(MessageLibrary.ArmyDisbanded), ColAmber);
                    _lastArmyHour = now;
                }
            }
            catch (Exception ex) { Log(ex, "OnArmyDispersed"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Hero prisoner taken
        // ═══════════════════════════════════════════════════════════════════

        private void OnHeroPrisonerTaken(PartyBase captorParty, Hero prisoner)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPrisonerMessages || !ShouldAllowChannel(SignalChannel.Party, true)) return;
            if (prisoner == null) return;

            float now = NowHour;
            if (now - _lastPrisonerHour < MessageSettings.PrisonerCooldownHours) return;

            try
            {
                BeginEvent();

                bool isPlayer         = prisoner == Hero.MainHero;
                bool isAlly           = !isPlayer && IsAllyOfPlayer(prisoner);
                bool isEnemy          = IsEnemyOfPlayer(prisoner);
                bool capturedByPlayer = captorParty?.MobileParty == MobileParty.MainParty;

                if (isPlayer)
                {
                    AdjustMood(14, -8, 0, 0);
                    ShowRL(Pick(MessageLibrary.PlayerCaptured), ColRed);
                    _lastPrisonerHour = now;
                }
                else if (capturedByPlayer && isEnemy)
                {
                    AdjustMood(-3, 7, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.EnemyCapturedNamed), prisoner.Name), ColGold);
                    ShowRL(Pick(MessageLibrary.EnemyHeroCaptured), ColGreen, isBonusLine: true);
                    _lastPrisonerHour = now;
                }
                else if (isAlly)
                {
                    AdjustMood(8, -4, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.AllyCapturedNamed), prisoner.Name), ColRed);
                    ShowRL(Pick(MessageLibrary.AllyHeroCaptured), ColAmber, isBonusLine: true);
                    _lastPrisonerHour = now;
                }
            }
            catch (Exception ex) { Log(ex, "OnHeroPrisonerTaken"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Hero prisoner released
        // ═══════════════════════════════════════════════════════════════════

        private void OnHeroPrisonerReleased(
            Hero prisoner,
            PartyBase party,
            IFaction capturerFaction,
            EndCaptivityDetail detail,
            bool showNotification)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPrisonerMessages || !ShouldAllowChannel(SignalChannel.Party, false)) return;
            if (prisoner == null) return;

            float now = NowHour;
            if (now - _lastPrisonerHour < MessageSettings.PrisonerCooldownHours) return;

            try
            {
                BeginEvent();

                bool isPlayer = prisoner == Hero.MainHero;
                bool isAlly   = !isPlayer && IsAllyOfPlayer(prisoner);
                bool isEnemy  = IsEnemyOfPlayer(prisoner);

                if (isPlayer)
                {
                    AdjustMood(-10, 6, 0, 0);
                    ShowRL(Pick(MessageLibrary.PlayerReleased), ColGreen);
                    _lastPrisonerHour = now;
                }
                else if (isAlly)
                {
                    AdjustMood(-5, 4, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.AllyReleasedNamed), prisoner.Name), ColGreen);
                    ShowRL(Pick(MessageLibrary.AllyHeroReleased), ColGreen, isBonusLine: true);
                    _lastPrisonerHour = now;
                }
                else if (isEnemy)
                {
                    AdjustMood(2, -2, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.EnemyReleasedNamed), prisoner.Name), ColAmber);
                    ShowRL(Pick(MessageLibrary.EnemyHeroReleased), ColAmber, isBonusLine: true);
                    _lastPrisonerHour = now;
                }
            }
            catch (Exception ex) { Log(ex, "OnHeroPrisonerReleased"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Clan changes kingdom
        // ═══════════════════════════════════════════════════════════════════

        private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom,
            ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPoliticalMessages || !ShouldAllowChannel(SignalChannel.Realm, false)) return;
            if (clan == null) return;

            float now = NowHour;
            if (now - _lastClanChangeHour < MessageSettings.ClanChangeCooldownHours) return;

            try
            {
                BeginEvent();

                bool isPlayerClan      = clan == Clan.PlayerClan;
                bool fromPlayerKingdom = oldKingdom != null &&
                                         oldKingdom == Hero.MainHero?.MapFaction as Kingdom;
                bool toPlayerKingdom   = newKingdom != null &&
                                         newKingdom == Hero.MainHero?.MapFaction as Kingdom;

                string oldName = oldKingdom?.Name?.ToString() ?? "independence";
                string newName = newKingdom?.Name?.ToString() ?? "independence";

                if (isPlayerClan)
                {
                    AdjustMood(3, 2, 4, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.PlayerClanChangedKingdom), oldName, newName), ColGold);
                }
                else if (fromPlayerKingdom)
                {
                    AdjustMood(9, -6, 7, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.ClanDefectedFrom), clan.Name, newName), ColRed);
                    ShowRL(Pick(MessageLibrary.ClanDefection), ColRed, isBonusLine: true);
                }
                else if (toPlayerKingdom)
                {
                    AdjustMood(-2, 6, -1, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.ClanJoinedYour), clan.Name, oldName), ColGreen);
                    ShowRL(Pick(MessageLibrary.ClanJoined), ColGreen, isBonusLine: true);
                }
                else
                {
                    AdjustMood(3, 0, 5, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.ClanChangedAllegiance),
                        clan.Name, oldName, newName), ColAmber);
                }

                _lastClanChangeHour = now;
            }
            catch (Exception ex) { Log(ex, "OnClanChangedKingdom"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Kingdom destroyed
        // ═══════════════════════════════════════════════════════════════════

        private void OnKingdomDestroyed(Kingdom kingdom)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowPoliticalMessages || !ShouldAllowChannel(SignalChannel.Realm, true)) return;
            if (kingdom == null) return;

            float now = NowHour;
            if (now - _lastKingdomHour < MessageSettings.KingdomCooldownHours) return;

            try
            {
                BeginEvent();

                bool wasPlayerKingdom = kingdom == Hero.MainHero?.MapFaction as Kingdom;
                bool wasEnemy         = IsEnemyFaction(kingdom);
                string kingdomName    = kingdom.Name?.ToString() ?? "A kingdom";

                if (wasPlayerKingdom)
                {
                    AdjustMood(18, -12, 12, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.PlayerKingdomDestroyed), kingdomName), ColRed);
                    ShowRL(Pick(MessageLibrary.KingdomDestroyedPlayerFlavor), ColRed, isBonusLine: true);
                }
                else if (wasEnemy)
                {
                    AdjustMood(-8, 10, -4, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.EnemyKingdomDestroyed), kingdomName), ColGold);
                    ShowRL(Pick(MessageLibrary.KingdomDestroyedEnemyFlavor), ColGreen, isBonusLine: true);
                }
                else
                {
                    AdjustMood(8, 0, 10, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.NeutralKingdomDestroyed), kingdomName), ColAmber);
                    ShowRL(Pick(MessageLibrary.KingdomDestroyedNeutralFlavor), ColAmber, isBonusLine: true);
                }

                _lastKingdomHour = now;
            }
            catch (Exception ex) { Log(ex, "OnKingdomDestroyed"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // NEW — Tournament finished
        // ═══════════════════════════════════════════════════════════════════

        // Bannerlord 1.4.15 exact signature from CampaignEvents.TournamentFinished:
        // Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>
        private void OnTournamentFinished(
            CharacterObject winner,
            MBReadOnlyList<CharacterObject> participants,
            Town town,
            ItemObject prize)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowTournamentMessages || !ShouldAllowChannel(SignalChannel.Tournament, false)) return;

            // Only fire when the player participated
            bool playerParticipated = participants != null &&
                Hero.MainHero?.CharacterObject != null &&
                participants.Contains(Hero.MainHero.CharacterObject);
            if (!playerParticipated) return;

            float now = NowHour;
            if (now - _lastTournamentHour < MessageSettings.TournamentCooldownHours) return;

            try
            {
                BeginEvent();

                bool playerWon = winner == Hero.MainHero?.CharacterObject;
                string townName = town?.Name?.ToString() ?? "the arena";

                if (playerWon)
                {
                    AdjustMood(-1, 5, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.TournamentVictoryNamed), townName), ColPurple);
                    ShowRL(Pick(MessageLibrary.TournamentVictory), ColGold, isBonusLine: true);
                }
                else
                {
                    AdjustMood(1, -1, 0, 0);
                    string winnerName = winner?.Name?.ToString() ?? "another";
                    ShowRL(string.Format(Pick(MessageLibrary.TournamentDefeatNamed), townName, winnerName), ColGrey);
                    ShowRL(Pick(MessageLibrary.TournamentDefeat), ColGrey, isBonusLine: true);
                }

                _lastTournamentHour = now;
            }
            catch (Exception ex) { Log(ex, "OnTournamentFinished"); }
        }

        private void OnTournamentStarted(Town town)
        {
            if (town == null || !IsPlayerAtTown(town))
                return;

            PostTournamentStarted(town, false, "OnTournamentStarted");
        }

        private void OnPlayerJoinedTournament(Town town, bool isParticipant)
        {
            if (!isParticipant)
                return;

            PostTournamentStarted(town, true, "OnPlayerJoinedTournament");
        }

        private void PostTournamentStarted(Town town, bool playerJoined, string context)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowTournamentMessages || !ShouldAllowChannel(SignalChannel.Tournament, false))
                return;

            float now = NowHour;
            if (now - _lastTournamentNoticeHour < Math.Min(2f, MessageSettings.TournamentCooldownHours))
                return;

            try
            {
                BeginEvent();
                AdjustMood(0, playerJoined ? 2 : 1, 0, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.TournamentStarted), ColPurple);
                _lastTournamentNoticeHour = now;
            }
            catch (Exception ex) { Log(ex, context); }
        }

        private void OnCompanionAdded(Hero companion)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowCompanionMessages || !ShouldAllowChannel(SignalChannel.Party, false))
                return;
            if (companion == null)
                return;

            float now = NowHour;
            if (now - _lastCompanionHour < MessageSettings.CompanionCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(-1, 4, 0, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.CompanionRecruited), ColGreen);
                _lastCompanionHour = now;
            }
            catch (Exception ex) { Log(ex, "OnCompanionAdded"); }
        }

        private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowCompanionMessages || !ShouldAllowChannel(SignalChannel.Party, false))
                return;
            if (companion == null)
                return;

            float now = NowHour;
            if (now - _lastCompanionHour < MessageSettings.CompanionCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(2, -3, 0, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.CompanionLeft), ColAmber);
                _lastCompanionHour = now;
            }
            catch (Exception ex) { Log(ex, "OnCompanionRemoved"); }
        }

        private void OnBeforeHeroesMarried(Hero firstHero, Hero secondHero, bool showNotification)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowFamilyMessages || !ShouldAllowChannel(SignalChannel.Realm, false))
                return;
            if (!IsPlayerFamilyRelevant(firstHero) && !IsPlayerFamilyRelevant(secondHero))
                return;

            float now = NowHour;
            if (now - _lastFamilyHour < MessageSettings.FamilyCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(-3, 5, -1, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.HeroMarried), ColSilver);
                _lastFamilyHour = now;
            }
            catch (Exception ex) { Log(ex, "OnBeforeHeroesMarried"); }
        }

        private void OnHeroCreated(Hero hero, bool showNotification)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowFamilyMessages || !ShouldAllowChannel(SignalChannel.Realm, false))
                return;
            if (hero == null || !hero.IsChild || hero.Age > 1f || !IsPlayerFamilyRelevant(hero))
                return;

            float now = NowHour;
            if (now - _lastFamilyHour < MessageSettings.FamilyCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(-2, 5, -1, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.ChildBorn), ColSilver);
                _lastFamilyHour = now;
            }
            catch (Exception ex) { Log(ex, "OnHeroCreated"); }
        }

        private void OnPartyAddedToMap(PartyBase party)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowCaravanMessages || !ShouldAllowChannel(SignalChannel.Party, false))
                return;
            if (!IsPlayerOwnedCaravan(party?.MobileParty))
                return;

            float now = NowHour;
            if (now - _lastCaravanHour < MessageSettings.CaravanCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(0, 3, 0, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.CaravanDeparted), ColGold);
                _lastCaravanHour = now;
            }
            catch (Exception ex) { Log(ex, "OnPartyAddedToMap"); }
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowCaravanMessages || !ShouldAllowChannel(SignalChannel.Party, true))
                return;
            if (!IsPlayerOwnedCaravan(attackerParty?.MobileParty) && !IsPlayerOwnedCaravan(defenderParty?.MobileParty))
                return;

            float now = NowHour;
            if (now - _lastCaravanHour < MessageSettings.CaravanCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(7, -5, 1, 4);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.CaravanRaided), ColRed);
                _lastCaravanHour = now;
            }
            catch (Exception ex) { Log(ex, "OnMapEventStarted"); }
        }

        private void OnCaravanTransactionCompleted(
            MobileParty caravanParty,
            Town town,
            List<ValueTuple<EquipmentElement, int>> soldItems)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowCaravanMessages || !ShouldAllowChannel(SignalChannel.Party, false))
                return;
            if (!IsPlayerOwnedCaravan(caravanParty))
                return;

            float now = NowHour;
            if (now - _lastCaravanHour < MessageSettings.CaravanCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(-2, 4, -1, -1);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.CaravanArrived), ColGreen);
                _lastCaravanHour = now;
            }
            catch (Exception ex) { Log(ex, "OnCaravanTransactionCompleted"); }
        }

        private void OnAfterSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowRouteMessages || !ShouldAllowChannel(SignalChannel.Party, false))
                return;
            if (party != MobileParty.MainParty || settlement == null)
                return;

            float now = NowHour;
            if (now - _lastRouteArrivalHour < MessageSettings.RouteArrivalCooldownHours)
                return;
            if (MBRandom.RandomFloat > MessageSettings.RouteArrivalMessageChance)
                return;

            try
            {
                BeginEvent();
                bool shown = ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.TravelArrived), ColGrey);

                if (shown && ModBridge.RoadsLoaded)
                {
                    string nextStop = ModBridge.GetNextStopName();
                    if (!string.IsNullOrEmpty(nextStop))
                    {
                        ShowRL(string.Format(ContentMessageLibrary.Pick(ContentMessageLibrary.RouteActiveContext), nextStop),
                               ColSilver, isBonusLine: true);
                    }
                }

                _lastRouteArrivalHour = now;
            }
            catch (Exception ex) { Log(ex, "OnAfterSettlementEntered"); }
        }

        private void OnHideoutBattleCompleted(
            BattleSideEnum winnerSide,
            HideoutEventComponent hideoutEvent,
            HideoutEventComponent.HideoutBattleEndState endState)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowHideoutMessages || !ShouldAllowChannel(SignalChannel.War, false))
                return;
            if (winnerSide != BattleSideEnum.Attacker)
                return;

            MapEvent mapEvent = hideoutEvent?.MapEvent;
            if (mapEvent != null && (!TryGetPlayerSide(mapEvent, out BattleSideEnum playerSide) || playerSide != BattleSideEnum.Attacker))
                return;

            float now = NowHour;
            if (now - _lastHideoutHour < MessageSettings.HideoutCooldownHours)
                return;

            try
            {
                BeginEvent();
                AdjustMood(-4, 6, -1, 0);
                ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.BanditHideoutCleared), ColGreen);
                _lastHideoutHour = now;
            }
            catch (Exception ex) { Log(ex, "OnHideoutBattleCompleted"); }
        }

        private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowQuestMessages || !ShouldAllowChannel(SignalChannel.Party, false))
                return;

            bool success = detail == QuestBase.QuestCompleteDetails.Success;
            bool failure = detail == QuestBase.QuestCompleteDetails.Fail ||
                           detail == QuestBase.QuestCompleteDetails.Timeout ||
                           detail == QuestBase.QuestCompleteDetails.FailWithBetrayal;
            if (!success && !failure)
                return;

            float now = NowHour;
            if (now - _lastQuestHour < MessageSettings.QuestCooldownHours)
                return;

            try
            {
                BeginEvent();
                if (success)
                {
                    AdjustMood(-2, 5, -1, 0);
                    ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.QuestCompleted), ColGreen);
                }
                else
                {
                    AdjustMood(4, -4, 1, 0);
                    ShowRL(ContentMessageLibrary.Pick(ContentMessageLibrary.QuestFailed), ColAmber);
                }

                _lastQuestHour = now;
            }
            catch (Exception ex) { Log(ex, "OnQuestCompleted"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // NEW — Hero becomes a fugitive
        // ═══════════════════════════════════════════════════════════════════

        private void OnCharacterBecameFugitive(Hero hero, bool showNotification)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowFugitiveMessages || !ShouldAllowChannel(SignalChannel.Realm, false)) return;
            if (hero == null || hero == Hero.MainHero) return;

            float now = NowHour;
            if (now - _lastFugitiveHour < MessageSettings.FugitiveCooldownHours) return;

            try
            {
                BeginEvent();

                bool isAlly  = IsAllyOfPlayer(hero);
                bool isEnemy = IsEnemyOfPlayer(hero);

                if (isEnemy)
                {
                    AdjustMood(-2, 4, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.EnemyFugitiveNamed), hero.Name), ColGreen);
                    ShowRL(Pick(MessageLibrary.EnemyFugitive), ColGreen, isBonusLine: true);
                    _lastFugitiveHour = now;
                }
                else if (isAlly)
                {
                    AdjustMood(5, -3, 0, 0);
                    ShowRL(string.Format(Pick(MessageLibrary.AllyFugitiveNamed), hero.Name), ColAmber);
                    ShowRL(Pick(MessageLibrary.AllyFugitive), ColAmber, isBonusLine: true);
                    _lastFugitiveHour = now;
                }
            }
            catch (Exception ex) { Log(ex, "OnCharacterBecameFugitive"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // NEW — Weekly tick: broad world-observation ambient messages
        // ═══════════════════════════════════════════════════════════════════

        private void OnWeeklyTick()
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowAmbientMessages || !ShouldAllowChannel(SignalChannel.Ambient, false)) return;

            float now = NowHour;
            if (now - _lastWeeklyHour < MessageSettings.WeeklyAmbientCooldownHours) return;
            if (MBRandom.RandomFloat > MessageSettings.WeeklyMessageChance) return;

            try
            {
                BeginEvent();
                DecayMood(now);
                bool shown = ShowRL(Pick(MessageLibrary.WeeklyAmbient), ColGrey);
                if (shown && MessageSettings.ShowWeeklyRealmPulse)
                    ShowRL(BuildRealmPulse(), ColSilver, isBonusLine: true);
                _lastWeeklyHour = now;
            }
            catch (Exception ex) { Log(ex, "OnWeeklyTick"); }
        }

        private static string BuildRealmPulse()
        {
            int kingdoms = 0;
            int warLinks = 0;

            try
            {
                foreach (Kingdom kingdom in Kingdom.All)
                {
                    if (kingdom == null) continue;
                    kingdoms++;
                    warLinks += kingdom.FactionsAtWarWith?.Count ?? 0;
                }
            }
            catch { }

            var clan = Hero.MainHero?.Clan;
            string clanState = clan != null
                ? "clan tier " + clan.Tier + ", renown " + clan.Renown.ToString("0")
                : "clan unformed";

            int fronts = Math.Max(0, warLinks / 2);
            return "[Realm Pulse] " + kingdoms + " kingdoms watched | " +
                   fronts + " war fronts | " + clanState;
        }

        // ═══════════════════════════════════════════════════════════════════
        // Hourly tick — ambient, travel, time-of-day, morale, food, wounded
        // ═══════════════════════════════════════════════════════════════════

        private void OnHourlyTick()
        {
            if (!MessageSettings.Enabled) return;
            try
            {
                float nowHour   = NowHour;
                float hourOfDay = NowHourOfDay;
                int   hourInt   = (int)hourOfDay;

                BeginEvent();
                DecayMood(nowHour);

                // ── Dawn (hour 5) ─────────────────────────────────────────
                if (MessageSettings.ShowAmbientMessages &&
                    ShouldAllowChannel(SignalChannel.Ambient, false) &&
                    hourInt == 5 && _lastDawnHour < nowHour - 20f &&
                    MBRandom.RandomFloat < MessageSettings.DawnMessageChance)
                {
                    ShowRL(Pick(MessageLibrary.DawnMessages), ColSilver);
                    _lastDawnHour = nowHour;
                }

                // ── Dusk (hour 19) ────────────────────────────────────────
                if (MessageSettings.ShowAmbientMessages &&
                    ShouldAllowChannel(SignalChannel.Ambient, false) &&
                    hourInt == 19 && _lastDuskHour < nowHour - 20f &&
                    MBRandom.RandomFloat < MessageSettings.DuskMessageChance)
                {
                    ShowRL(Pick(MessageLibrary.DuskMessages), ColDusk);
                    _lastDuskHour = nowHour;
                }

                // ── Deep night (hour 1) ───────────────────────────────────
                if (MessageSettings.ShowAmbientMessages &&
                    ShouldAllowChannel(SignalChannel.Ambient, false) &&
                    hourInt == 1 && _lastAmbientHour < nowHour - MessageSettings.AmbientCooldownHours &&
                    MBRandom.RandomFloat < MessageSettings.NightMessageChance)
                {
                    ShowRL(Pick(MessageLibrary.NightMessages), ColBlue);
                    _lastAmbientHour = nowHour;
                }

                // ── Travel messages (only when moving) ────────────────────
                if (MessageSettings.ShowTravelMessages &&
                    ShouldAllowChannel(SignalChannel.Party, false) &&
                    IsPlayerMoving() &&
                    _lastTravelHour < nowHour - MessageSettings.TravelCooldownHours &&
                    MBRandom.RandomFloat < MessageSettings.TravelMessageChance)
                {
                    ShowRL(Pick(MessageLibrary.TravelOnRoute), ColGrey);
                    _lastTravelHour = nowHour;
                }

                // ── Morale check ──────────────────────────────────────────
                if (MessageSettings.ShowMoraleMessages &&
                    ShouldAllowChannel(SignalChannel.Party, false) &&
                    _lastMoraleHour < nowHour - MessageSettings.MoraleCooldownHours &&
                    MBRandom.RandomFloat < MessageSettings.MoraleMessageChance)
                {
                    PostMoraleMessage(nowHour);
                }

                bool urgentFood = IsFoodCritical();
                bool urgentWounded = IsWoundedCritical();
                UpdateSupplyPressure();

                // ── Low food warning ──────────────────────────────────────
                if (MessageSettings.ShowSupplyMessages &&
                    ShouldAllowChannel(SignalChannel.Party, urgentFood) &&
                    _lastFoodHour < nowHour - MessageSettings.FoodCooldownHours &&
                    (urgentFood || MBRandom.RandomFloat < MessageSettings.FoodMessageChance))
                {
                    PostFoodMessage(nowHour);
                }

                // ── Heavy casualties warning ──────────────────────────────
                if (MessageSettings.ShowSupplyMessages &&
                    ShouldAllowChannel(SignalChannel.Party, urgentWounded) &&
                    _lastWoundedHour < nowHour - MessageSettings.WoundedCooldownHours &&
                    (urgentWounded || MBRandom.RandomFloat < MessageSettings.WoundedMessageChance))
                {
                    PostWoundedMessage(nowHour);
                }

                TryPostTAOMBridgePulse(nowHour);
            }
            catch (Exception ex) { Log(ex, "OnHourlyTick"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Daily tick — season messages
        // ═══════════════════════════════════════════════════════════════════

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowAmbientMessages || !ShouldAllowChannel(SignalChannel.Ambient, false)) return;
            if (settlement == null || !settlement.IsTown) return;

            try
            {
                int seasonNow = (int)CampaignTime.Now.GetSeasonOfYear;

                if (seasonNow != _lastSeasonIndex)
                {
                    _seasonFiredThisSeason = false;
                    _lastSeasonIndex = seasonNow;
                }

                if (_seasonFiredThisSeason) return;

                // BUG FIX: commit the attempt BEFORE the probability roll.
                // OnDailyTickSettlement fires once per town (20+ calls/day),
                // so each town would get its own independent roll — making the
                // effective chance nearly 100 %. Setting the flag first means
                // only the first town to reach this code gets the roll.
                _seasonFiredThisSeason = true;

                if (MBRandom.RandomFloat > MessageSettings.SeasonMessageChance) return;

                BeginEvent();

                string[] pool;
                Color    col;
                switch ((CampaignTime.Seasons)seasonNow)
                {
                    case CampaignTime.Seasons.Spring: pool = MessageLibrary.SeasonSpring; col = ColGreen;  break;
                    case CampaignTime.Seasons.Summer: pool = MessageLibrary.SeasonSummer; col = ColGold;   break;
                    case CampaignTime.Seasons.Autumn: pool = MessageLibrary.SeasonAutumn; col = ColAmber;  break;
                    default:                          pool = MessageLibrary.SeasonWinter; col = ColSilver; break;
                }
                ShowRL(Pick(pool), col);
            }
            catch (Exception ex) { Log(ex, "OnDailyTickSettlement"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Sub-routines
        // ═══════════════════════════════════════════════════════════════════

        private void PostMoraleMessage(float nowHour)
        {
            var party = MobileParty.MainParty;
            if (party == null) return;

            // BUG FIX: always update the cooldown timestamp so mid-morale
            // (30–70) does not cause PostMoraleMessage to be called every
            // single campaign hour once the window reopens.
            _lastMoraleHour = nowHour;

            float morale = party.Morale;
            if (morale >= 70f)
            {
                AdjustMood(-2, 4, 0, -1);
                ShowRL(Pick(MessageLibrary.MoraleHigh), ColGreen);
            }
            else if (morale <= 30f)
            {
                AdjustMood(5, -3, 0, 2);
                ShowRL(Pick(MessageLibrary.MoraleLow), ColRed);
            }
        }

        // NEW — low food warning
        private void PostFoodMessage(float nowHour)
        {
            var party = MobileParty.MainParty;
            if (party == null) return;

            _lastFoodHour = nowHour;  // always update to avoid spam on mid-food levels

            if (party.FoodChange >= 0f) return;  // not consuming food — skip

            float daysRemaining = party.Food / (-party.FoodChange);
            if (daysRemaining > MessageSettings.FoodWarningDaysThreshold) return;

            if (daysRemaining <= 0f)
            {
                AdjustMood(8, -5, 0, 8);
                ShowRL(Pick(MessageLibrary.Starving), ColRed);
            }
            else
            {
                AdjustMood(4, -2, 0, 5);
                ShowRL(Pick(MessageLibrary.LowFood), ColAmber);
            }
        }

        // NEW — heavy-casualties warning
        private void PostWoundedMessage(float nowHour)
        {
            var party = MobileParty.MainParty;
            if (party == null) return;

            _lastWoundedHour = nowHour;

            int total    = party.MemberRoster.TotalManCount;
            int wounded  = party.MemberRoster.TotalWounded;
            if (total <= 0) return;

            float fraction = (float)wounded / total;
            if (fraction < MessageSettings.WoundedThresholdFraction) return;

            AdjustMood(5, -3, 0, 5);
            ShowRL(Pick(MessageLibrary.PartyHeavilyWounded), ColAmber);
        }

        private void TryPostTAOMBridgePulse(float nowHour)
        {
            if (!MessageSettings.Enabled || !MessageSettings.ShowTAOMBridgeMessages)
                return;
            if (!ShouldAllowChannel(SignalChannel.Realm, false) && !ShouldAllowChannel(SignalChannel.Party, false))
                return;
            if (nowHour - _lastBridgePulseHour < MessageSettings.TAOMBridgeCooldownHours)
                return;
            if (MBRandom.RandomFloat > MessageSettings.TAOMBridgeMessageChance)
                return;

            try
            {
                BeginEvent();

                Settlement settlement = GetContextSettlement();
                string settlementName = settlement?.Name?.ToString() ?? "the local holding";

                if (settlement != null && ModBridge.ShadowDreadLoaded)
                {
                    float dread = ModBridge.GetDread(settlement);
                    if (IsSevereMetric(dread) && TryShowBridgeLine(ContentMessageLibrary.DreadTerrorised, settlementName, ColRed, nowHour)) return;
                    if (IsHighMetric(dread) && TryShowBridgeLine(ContentMessageLibrary.DreadFearful, settlementName, ColAmber, nowHour)) return;
                }

                if (settlement != null && ModBridge.WeightLoaded)
                {
                    float strain = ModBridge.GetAdminStrain(settlement);
                    if (IsHighMetric(strain) && TryShowBridgeLine(ContentMessageLibrary.WeightStrainHigh, settlementName, ColAmber, nowHour)) return;

                    float legitimacy = ModBridge.GetRuleLegitimacy(settlement);
                    if (IsLowMetric(legitimacy) && TryShowBridgeLine(ContentMessageLibrary.WeightLegitimacyLow, settlementName, ColRed, nowHour)) return;
                    if (IsHighMetric(legitimacy) && TryShowBridgeLine(ContentMessageLibrary.WeightLegitimacyHigh, settlementName, ColGreen, nowHour)) return;
                }

                if (settlement != null && ModBridge.WatchLoaded)
                {
                    float readiness = ModBridge.GetWatchReadiness(settlement);
                    if (IsLowMetric(readiness) && TryShowBridgeLine(ContentMessageLibrary.WatchReadinessLow, settlementName, ColRed, nowHour)) return;

                    float burden = ModBridge.GetCivicBurden(settlement);
                    if (IsHighMetric(burden) && TryShowBridgeLine(ContentMessageLibrary.WatchBurdenHigh, settlementName, ColAmber, nowHour)) return;
                    if (IsHighMetric(readiness) && TryShowBridgeLine(ContentMessageLibrary.WatchReadinessGood, settlementName, ColGreen, nowHour)) return;
                }

                if (settlement != null && ModBridge.BondsLoaded)
                {
                    float strain = ModBridge.GetVillageStrain(settlement);
                    if (IsHighMetric(strain) && TryShowBridgeLine(ContentMessageLibrary.BondsStrainHigh, settlementName, ColAmber, nowHour)) return;

                    float grain = ModBridge.GetGrainFlow(settlement);
                    if (IsLowMetric(grain) && TryShowBridgeLine(ContentMessageLibrary.BondsGrainLow, settlementName, ColRed, nowHour)) return;

                    float compact = ModBridge.GetCompactLegitimacy(settlement);
                    if (IsHighMetric(compact) && TryShowBridgeLine(ContentMessageLibrary.BondsHealthy, settlementName, ColGreen, nowHour)) return;
                }

                if (settlement != null && ModBridge.MendingLoaded)
                {
                    int wounds = ModBridge.GetWoundCount(settlement);
                    float severity = ModBridge.GetWoundSeverity(settlement);
                    float recovery = ModBridge.GetRecoveryProgress(settlement);
                    if (wounds > 0 && IsHighMetric(severity) && TryShowBridgeLine(ContentMessageLibrary.LongMendingWounded, settlementName, ColAmber, nowHour)) return;
                    if (IsHighMetric(recovery) && TryShowBridgeLine(ContentMessageLibrary.LongMendingMended, settlementName, ColGreen, nowHour)) return;
                    if (wounds > 0 && HasMetricValue(recovery) && TryShowBridgeLine(ContentMessageLibrary.LongMendingRecovering, settlementName, ColSilver, nowHour)) return;
                }

                if (settlement != null && ModBridge.MarchesLoaded)
                {
                    float pressure = ModBridge.GetMarchPressure(settlement);
                    if (IsHighMetric(pressure) && TryShowBridgeLine(ContentMessageLibrary.MarchPressureHigh, settlementName, ColAmber, nowHour)) return;
                    if (IsLowMetric(pressure) && TryShowBridgeLine(ContentMessageLibrary.MarchPressureEasing, settlementName, ColGreen, nowHour)) return;
                }

                if (settlement != null && ModBridge.FoundingLoaded)
                {
                    int stage = ModBridge.GetFoundingStage(settlement);
                    float progress = ModBridge.GetFoundingProgress(settlement);
                    if (stage > 0 && IsHighMetric(progress) && TryShowBridgeLine(ContentMessageLibrary.FoundingRecognition, settlementName, ColGold, nowHour)) return;
                    if (stage > 0 || HasMetricValue(progress))
                    {
                        if (TryShowBridgeLine(ContentMessageLibrary.FoundingProgress, settlementName, ColSilver, nowHour)) return;
                    }
                }

                if (ModBridge.HostsLoaded)
                {
                    float cohesion = ModBridge.GetHostCohesion();
                    if (IsLowMetric(cohesion) && TryShowBridgeLine(ContentMessageLibrary.HostsCohesionLow, null, ColAmber, nowHour)) return;
                    if (settlement != null && IsHighMetric(ModBridge.GetRequisitionPressure(settlement)) &&
                        TryShowBridgeLine(ContentMessageLibrary.HostsRequisitionHigh, settlementName, ColRed, nowHour)) return;
                    if (IsHighMetric(cohesion) && TryShowBridgeLine(ContentMessageLibrary.HostsCohesionGood, null, ColGreen, nowHour)) return;
                }

                if (ModBridge.OathsLoaded)
                {
                    Hero hero = Hero.MainHero;
                    float feudheat = ModBridge.GetFeudheat(hero);
                    if (IsHighMetric(feudheat) && TryShowBridgeLine(ContentMessageLibrary.OathsFeudheatHigh, null, ColRed, nowHour)) return;

                    float strength = ModBridge.GetOathStrength(hero);
                    if (IsHighMetric(strength) && TryShowBridgeLine(ContentMessageLibrary.OathsStrengthHigh, null, ColGreen, nowHour)) return;
                    if (IsLowMetric(strength) && TryShowBridgeLine(ContentMessageLibrary.OathsStrengthLow, null, ColAmber, nowHour)) return;
                }

                if (settlement != null && ModBridge.LegacyLoaded)
                {
                    int traits = ModBridge.GetActiveTraitCount(settlement);
                    if (traits > 0 && TryShowBridgeLine(ContentMessageLibrary.LegacyTraitEarned, settlementName, ColGold, nowHour)) return;
                    if (IsHighMetric(ModBridge.GetLegacyScore(settlement)) &&
                        TryShowBridgeLine(ContentMessageLibrary.LegacyScoreBuilding, settlementName, ColSilver, nowHour)) return;
                }

                if (settlement != null && ModBridge.CourtLoaded)
                {
                    if (ModBridge.GetPetitionCount(settlement) > 0 &&
                        TryShowBridgeLine(ContentMessageLibrary.CourtPetitionsPending, settlementName, ColAmber, nowHour)) return;
                    if (IsHighMetric(ModBridge.GetCourtPrestige(settlement)) &&
                        TryShowBridgeLine(ContentMessageLibrary.CourtPrestigeHigh, settlementName, ColGreen, nowHour)) return;
                }

                if (settlement != null && ModBridge.RoadsMELoaded)
                {
                    if (IsHighMetric(ModBridge.GetCorridorScore(settlement)) &&
                        TryShowBridgeLine(ContentMessageLibrary.RoadsMEDangerous, settlementName, ColRed, nowHour)) return;
                    if (IsHighMetric(ModBridge.GetRoadCondition(settlement)) &&
                        TryShowBridgeLine(ContentMessageLibrary.RoadsMEGood, settlementName, ColGreen, nowHour)) return;
                }

                if (ModBridge.ArmyDocLoaded)
                {
                    float drift = ModBridge.GetCompositionDrift(MobileParty.MainParty);
                    if (IsHighMetric(drift) && TryShowBridgeLine(ContentMessageLibrary.ArmyDriftHigh, null, ColAmber, nowHour)) return;
                    if (IsLowMetric(drift) && TryShowBridgeLine(ContentMessageLibrary.ArmyDriftLow, null, ColGreen, nowHour)) return;
                }

                if (ModBridge.RangerLoaded)
                {
                    float weather = ModBridge.GetWeatherSeverity();
                    if (IsHighMetric(weather) && TryShowBridgeLine(ContentMessageLibrary.RangerWeatherSevere, null, ColBlue, nowHour)) return;

                    float camp = ModBridge.GetCampReadiness();
                    if (IsLowMetric(camp) && TryShowBridgeLine(ContentMessageLibrary.RangerCampPoor, null, ColAmber, nowHour)) return;
                    if (IsHighMetric(camp) && TryShowBridgeLine(ContentMessageLibrary.RangerCampGood, null, ColGreen, nowHour)) return;
                }

                if (ModBridge.PalantirLoaded)
                {
                    float health = ModBridge.GetSaveHealthScore();
                    if (IsLowMetric(health) && TryShowBridgeLine(ContentMessageLibrary.PalantirHealthPoor, MetricLabel(health), ColAmber, nowHour)) return;
                    if (IsHighMetric(health) && TryShowBridgeLine(ContentMessageLibrary.PalantirHealthGood, MetricLabel(health), ColGreen, nowHour)) return;
                }
            }
            catch (Exception ex) { Log(ex, "TryPostTAOMBridgePulse"); }
        }

        // ═══════════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════════

        private string BuildStatusPanelBody()
        {
            var sb = new StringBuilder(900);
            float now = NowHour;
            DecayMood(now);
            UpdateSupplyPressure();

            sb.AppendLine("Signal Budget");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine("Enabled: " + (MessageSettings.Enabled ? "yes" : "no"));
            sb.AppendLine("Stance: " + CurrentStance + " | Focus: " + CurrentFocus);
            sb.AppendLine("This hour: " + _messagesThisHour + " / " + EffectiveMaxMessagesPerHour() +
                          " messages | gap: " + EffectiveGlobalMessageGapHours().ToString("0.00") + " campaign hours");
            sb.AppendLine("Last message: " + FormatAgeHours(now - _lastGlobalShowHour));
            sb.AppendLine("Shown: " + _signalsShown + " | Suppressed by pacing: " + _signalsSuppressed);
            sb.AppendLine("F9 opens this panel. Settlement menus include stance/focus controls.");
            sb.AppendLine();

            sb.AppendLine("Campaign Pulse");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine(BuildMoodLine());
            sb.AppendLine(BuildRealmPulse());
            sb.AppendLine(BuildPartyPulse());
            sb.AppendLine(BuildImmediateAdvice());
            sb.AppendLine();

            sb.AppendLine("Active Categories");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine(BuildCategoryLine());
            sb.AppendLine();

            sb.AppendLine("Linked TAOM Bridges");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine(BuildBridgeLine());

            return sb.ToString();
        }

        private string BuildMoodLine()
        {
            return "Mood: " + BuildMoodLabel() +
                   " | tension " + _campaignTension +
                   " | momentum " + _playerMomentum +
                   " | realm " + _realmInstability +
                   " | supply " + _supplyPressure;
        }

        private string BuildMoodLabel()
        {
            int pressure = Math.Max(_campaignTension, Math.Max(_realmInstability, _supplyPressure));
            int balance = _playerMomentum - pressure;

            if (_supplyPressure >= 65)
                return "Supply crisis";
            if (_campaignTension >= 70 || _realmInstability >= 70)
                return "War shadow";
            if (balance >= 25)
                return "Rising hope";
            if (balance <= -25)
                return "Hard-pressed";
            if (pressure >= 40)
                return "Uneasy";
            return "Steady";
        }

        private static string BuildPartyPulse()
        {
            try
            {
                var party = MobileParty.MainParty;
                if (party == null)
                    return "Party: not available";

                int total = party.MemberRoster?.TotalManCount ?? 0;
                int wounded = party.MemberRoster?.TotalWounded ?? 0;
                string food = party.FoodChange < 0f
                    ? (party.Food / (-party.FoodChange)).ToString("0.0") + " food days"
                    : "food stable";

                return "Party: " + total + " troops | wounded " + wounded +
                       " | morale " + party.Morale.ToString("0") + " | " + food;
            }
            catch
            {
                return "Party: pulse unavailable";
            }
        }

        private string BuildImmediateAdvice()
        {
            if (IsFoodCritical())
                return "Advice: food is critical; supply warnings will bypass the random chance gate.";
            if (IsWoundedCritical())
                return "Advice: casualties are heavy; wounded warnings will bypass the random chance gate.";
            if (_realmInstability >= 60)
                return "Advice: realm instability is high; switch focus to Realm if political traffic is being missed.";
            if (_campaignTension >= 60)
                return "Advice: campaign tension is high; War focus keeps battle signals visible.";
            if (_playerMomentum >= 60)
                return "Advice: momentum is strong; Quiet stance can reduce celebratory chatter.";

            try
            {
                var party = MobileParty.MainParty;
                if (party != null && party.Morale <= 30f)
                    return "Advice: morale is low; morale lines are likely when cooldown allows.";
            }
            catch { }

            return "Advice: " + BuildMoodLabel().ToLowerInvariant() + "; " + CurrentStance + " stance is active.";
        }

        private static string BuildCategoryLine()
        {
            var parts = new System.Collections.Generic.List<string>();
            AddCategory(parts, MessageSettings.ShowBattleMessages, "battle");
            AddCategory(parts, MessageSettings.ShowRaidMessages, "raids");
            AddCategory(parts, MessageSettings.ShowSiegeMessages, "sieges");
            AddCategory(parts, MessageSettings.ShowSettlementMessages, "settlements");
            AddCategory(parts, MessageSettings.ShowPoliticalMessages, "politics");
            AddCategory(parts, MessageSettings.ShowPrisonerMessages, "prisoners");
            AddCategory(parts, MessageSettings.ShowAmbientMessages, "ambient");
            AddCategory(parts, MessageSettings.ShowTravelMessages, "travel");
            AddCategory(parts, MessageSettings.ShowSupplyMessages, "supply");
            AddCategory(parts, MessageSettings.ShowTournamentMessages, "tournaments");
            AddCategory(parts, MessageSettings.ShowCompanionMessages, "companions");
            AddCategory(parts, MessageSettings.ShowCaravanMessages, "caravans");
            AddCategory(parts, MessageSettings.ShowFamilyMessages, "family");
            AddCategory(parts, MessageSettings.ShowHideoutMessages, "hideouts");
            AddCategory(parts, MessageSettings.ShowQuestMessages, "quests");
            AddCategory(parts, MessageSettings.ShowRouteMessages, "routes");
            AddCategory(parts, MessageSettings.ShowTAOMBridgeMessages, "TAOM bridges");
            return parts.Count == 0 ? "No categories enabled" : string.Join(", ", parts);
        }

        private static void AddCategory(System.Collections.Generic.List<string> parts, bool enabled, string label)
        {
            if (enabled)
                parts.Add(label);
        }

        private static string BuildBridgeLine()
        {
            var parts = new System.Collections.Generic.List<string>();
            AddBridge(parts, ModBridge.ShadowDreadLoaded, "Shadow & Dread");
            AddBridge(parts, ModBridge.RoadsLoaded, "Roads of the King");
            AddBridge(parts, ModBridge.WeightLoaded, "Weight of Rule");
            AddBridge(parts, ModBridge.WatchLoaded, "Watch of the Marches");
            AddBridge(parts, ModBridge.BondsLoaded, "Bonds of the Holding");
            AddBridge(parts, ModBridge.MendingLoaded, "Long Mending");
            AddBridge(parts, ModBridge.MarchesLoaded, "Burdened Marches");
            AddBridge(parts, ModBridge.FoundingLoaded, "Founding of Holds");
            AddBridge(parts, ModBridge.HostsLoaded, "Hosts in the Field");
            AddBridge(parts, ModBridge.OathsLoaded, "Oaths and Houses");
            AddBridge(parts, ModBridge.LegacyLoaded, "Legacy Traits");
            AddBridge(parts, ModBridge.CourtLoaded, "Court/Council");
            AddBridge(parts, ModBridge.RoadsMELoaded, "Roads of Middle-earth");
            AddBridge(parts, ModBridge.ArmyDocLoaded, "Army Composition");
            AddBridge(parts, ModBridge.RangerLoaded, "Ranger's Satchel");
            AddBridge(parts, ModBridge.PalantirLoaded, "Palantir");
            return parts.Count == 0 ? "No optional TAOM bridge currently linked" : string.Join(", ", parts);
        }

        private static void AddBridge(System.Collections.Generic.List<string> parts, bool loaded, string label)
        {
            if (loaded)
                parts.Add(label);
        }

        private static int BuildEnabledCategoryCount()
        {
            int count = 0;
            if (MessageSettings.ShowBattleMessages) count++;
            if (MessageSettings.ShowRaidMessages) count++;
            if (MessageSettings.ShowSiegeMessages) count++;
            if (MessageSettings.ShowSettlementMessages) count++;
            if (MessageSettings.ShowPoliticalMessages) count++;
            if (MessageSettings.ShowPrisonerMessages) count++;
            if (MessageSettings.ShowAmbientMessages) count++;
            if (MessageSettings.ShowTravelMessages) count++;
            if (MessageSettings.ShowSupplyMessages) count++;
            if (MessageSettings.ShowTournamentMessages) count++;
            if (MessageSettings.ShowCompanionMessages) count++;
            if (MessageSettings.ShowCaravanMessages) count++;
            if (MessageSettings.ShowFamilyMessages) count++;
            if (MessageSettings.ShowHideoutMessages) count++;
            if (MessageSettings.ShowQuestMessages) count++;
            if (MessageSettings.ShowRouteMessages) count++;
            if (MessageSettings.ShowTAOMBridgeMessages) count++;
            return count;
        }

        private static string BuildBridgeSummary()
        {
            int count = BuildBridgeCount();
            return count == 0 ? "Bridges: none linked" : "Bridges: " + count + " linked";
        }

        private static int BuildBridgeCount()
        {
            int count = 0;
            if (ModBridge.ShadowDreadLoaded) count++;
            if (ModBridge.RoadsLoaded) count++;
            if (ModBridge.WeightLoaded) count++;
            if (ModBridge.WatchLoaded) count++;
            if (ModBridge.BondsLoaded) count++;
            if (ModBridge.MendingLoaded) count++;
            if (ModBridge.MarchesLoaded) count++;
            if (ModBridge.FoundingLoaded) count++;
            if (ModBridge.HostsLoaded) count++;
            if (ModBridge.OathsLoaded) count++;
            if (ModBridge.LegacyLoaded) count++;
            if (ModBridge.CourtLoaded) count++;
            if (ModBridge.RoadsMELoaded) count++;
            if (ModBridge.ArmyDocLoaded) count++;
            if (ModBridge.RangerLoaded) count++;
            if (ModBridge.PalantirLoaded) count++;
            return count;
        }

        private static void AddSignal(
            ContextualSignalBoardSnapshot board,
            string name,
            string value,
            string detail,
            string status,
            bool good,
            bool watch,
            bool bad)
        {
            if (board == null)
                return;

            board.Signals.Add(new ContextualSignalSnapshot
            {
                Name = name ?? string.Empty,
                ValueText = value ?? string.Empty,
                DetailText = detail ?? string.Empty,
                StatusText = status ?? string.Empty,
                IsGood = good && !bad,
                IsWatch = watch && !bad,
                IsBad = bad
            });
        }

        private static bool IsFoodCritical()
        {
            try
            {
                var party = MobileParty.MainParty;
                if (party == null || party.FoodChange >= 0f)
                    return false;

                float daysRemaining = party.Food / (-party.FoodChange);
                return daysRemaining <= Math.Max(1f, MessageSettings.FoodWarningDaysThreshold * 0.5f);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWoundedCritical()
        {
            try
            {
                var party = MobileParty.MainParty;
                if (party == null || party.MemberRoster == null)
                    return false;

                int total = party.MemberRoster.TotalManCount;
                if (total <= 0)
                    return false;

                float fraction = (float)party.MemberRoster.TotalWounded / total;
                return fraction >= Math.Max(0.5f, MessageSettings.WoundedThresholdFraction);
            }
            catch
            {
                return false;
            }
        }

        private static string FormatAgeHours(float delta)
        {
            if (delta > 900f)
                return "none this session";
            if (delta < 0f)
                return "pending";
            if (delta < 1f)
                return ((int)Math.Max(0f, delta * 60f)) + " campaign minutes ago";
            return delta.ToString("0.0") + " campaign hours ago";
        }

        private bool TryShowBridgeLine(string[] pool, object formatArg, Color color, float nowHour)
        {
            string text = ContentMessageLibrary.Pick(pool);
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (formatArg != null)
            {
                try { text = string.Format(text, formatArg); }
                catch { }
            }

            bool shown = ShowRL(text, color);
            if (shown)
                _lastBridgePulseHour = nowHour;
            return shown;
        }

        private static Settlement GetContextSettlement()
        {
            try
            {
                if (MobileParty.MainParty?.CurrentSettlement != null)
                    return MobileParty.MainParty.CurrentSettlement;
                if (Hero.MainHero?.CurrentSettlement != null)
                    return Hero.MainHero.CurrentSettlement;
                if (MobileParty.MainParty != null)
                    return NearestTownOrCastle(MobileParty.MainParty.Position);
            }
            catch { }

            return null;
        }

        private static bool IsPlayerAtTown(Town town)
        {
            if (town == null)
                return false;

            Settlement settlement = town.Settlement;
            return settlement != null &&
                   (MobileParty.MainParty?.CurrentSettlement == settlement ||
                    Hero.MainHero?.CurrentSettlement == settlement);
        }

        private static bool IsPlayerFamilyRelevant(Hero hero)
        {
            if (hero == null)
                return false;
            if (hero == Hero.MainHero || hero.Clan == Clan.PlayerClan)
                return true;
            if (hero.Father != null && hero.Father.Clan == Clan.PlayerClan)
                return true;
            if (hero.Mother != null && hero.Mother.Clan == Clan.PlayerClan)
                return true;
            if (hero.Spouse != null && hero.Spouse.Clan == Clan.PlayerClan)
                return true;
            return false;
        }

        private static bool IsPlayerOwnedCaravan(MobileParty party)
        {
            if (party == null || !party.IsCaravan)
                return false;

            Hero owner = party.Owner ?? party.LeaderHero;
            if (owner == null && party.CaravanPartyComponent != null)
                owner = party.CaravanPartyComponent.Owner ?? party.CaravanPartyComponent.PartyOwner;

            return owner == Hero.MainHero ||
                   owner?.Clan == Clan.PlayerClan ||
                   party.ActualClan == Clan.PlayerClan;
        }

        private static bool HasMetricValue(float value)
        {
            return value > 0.001f;
        }

        private static bool IsHighMetric(float value)
        {
            if (value <= 0f)
                return false;
            if (value <= 1f)
                return value >= 0.75f;
            if (value <= 10f)
                return value >= 7.5f;
            return value >= 75f;
        }

        private static bool IsSevereMetric(float value)
        {
            if (value <= 0f)
                return false;
            if (value <= 1f)
                return value >= 0.9f;
            if (value <= 10f)
                return value >= 9f;
            return value >= 90f;
        }

        private static bool IsLowMetric(float value)
        {
            if (value <= 0f)
                return false;
            if (value <= 1f)
                return value <= 0.25f;
            if (value <= 10f)
                return value <= 2.5f;
            return value <= 25f;
        }

        private static string MetricLabel(float value)
        {
            if (value <= 0f)
                return "unknown";
            if (value <= 1f)
                return (value * 100f).ToString("0") + "%";
            return value.ToString("0");
        }

        private static string Pick(string[] pool) => MessageLibrary.Pick(pool);

        private static bool IsPlayerMoving()
        {
            var p = MobileParty.MainParty;
            return p != null && p.CurrentSettlement == null && p.Speed > 0.1f;
        }

        private static bool TryGetPlayerSide(MapEvent mapEvent, out BattleSideEnum side)
        {
            side = BattleSideEnum.None;
            if (Hero.MainHero?.PartyBelongedTo == null) return false;
            var pp = Hero.MainHero.PartyBelongedTo.Party;
            try
            {
                var att = mapEvent.AttackerSide;
                if (att?.Parties != null)
                    foreach (var mp in att.Parties)
                        if (mp.Party == pp) { side = BattleSideEnum.Attacker; return true; }
                var def = mapEvent.DefenderSide;
                if (def?.Parties != null)
                    foreach (var mp in def.Parties)
                        if (mp.Party == pp) { side = BattleSideEnum.Defender; return true; }
            }
            catch { }
            return false;
        }

        private static Settlement NearestTownOrCastle(CampaignVec2 pos)
        {
            Settlement nearest = null;
            float best = float.MaxValue;
            foreach (var s in Settlement.All)
            {
                if (!s.IsTown && !s.IsCastle) continue;
                float dx = s.GatePosition.X - pos.X;
                float dy = s.GatePosition.Y - pos.Y;
                float d  = dx * dx + dy * dy;
                if (d < best) { best = d; nearest = s; }
            }
            return nearest;
        }

        private static bool IsEnemyOfPlayer(Hero hero)
        {
            if (hero?.MapFaction == null || Hero.MainHero?.MapFaction == null) return false;
            return Hero.MainHero.MapFaction.IsAtWarWith(hero.MapFaction);
        }

        private static bool IsAllyOfPlayer(Hero hero)
        {
            IFaction heroFaction   = hero?.MapFaction;
            IFaction playerFaction = Hero.MainHero?.MapFaction;

            if (heroFaction == null || playerFaction == null)
                return false;

            IFaction heroMapFaction = heroFaction.MapFaction ?? heroFaction;
            IFaction playerMapFaction = playerFaction.MapFaction ?? playerFaction;

            return heroFaction == playerFaction
                || heroMapFaction == playerMapFaction;
        }

        private static bool IsEnemyFaction(IFaction faction)
        {
            if (faction == null || Hero.MainHero?.MapFaction == null) return false;
            return Hero.MainHero.MapFaction.IsAtWarWith(faction);
        }

        private static void Log(Exception ex, string context)
        {
            try { TaleWorlds.Library.Debug.Print($"[ContextualMessages][{context}] {ex.Message}"); }
            catch { }
        }
    }
}
