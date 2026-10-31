namespace ContextualMessages.Config
{
    public static class MessageSettings
    {
        // ── Master toggle ──────────────────────────────────────────────────
        public static bool Enabled = true;

        // ── Per-category toggles ───────────────────────────────────────────
        public static bool ShowBattleMessages      = true;
        public static bool ShowRaidMessages        = true;
        public static bool ShowSiegeMessages       = true;
        public static bool ShowHeroDeathMessages   = true;
        public static bool ShowPoliticalMessages   = true;
        /// <summary>Settlement ownership changes (captures, diplomatic transfers).</summary>
        public static bool ShowSettlementMessages  = true;  // FIX: was incorrectly gated under ShowSiegeMessages
        public static bool ShowAmbientMessages     = true;   // time of day, seasons, weekly
        public static bool ShowTravelMessages      = true;
        public static bool ShowMoraleMessages      = true;
        public static bool ShowLevelUpMessages     = true;
        public static bool ShowPrisonerMessages    = true;
        public static bool ShowTournamentMessages  = true;   // NEW
        public static bool ShowSupplyMessages      = true;   // NEW — food & wounded
        public static bool ShowFugitiveMessages    = true;   // NEW
        public static bool ShowCompanionMessages   = true;
        public static bool ShowCaravanMessages     = true;
        public static bool ShowFamilyMessages      = true;
        public static bool ShowHideoutMessages     = true;
        public static bool ShowQuestMessages       = true;
        public static bool ShowRouteMessages       = true;
        public static bool ShowTAOMBridgeMessages  = true;
        public static bool ShowWeeklyRealmPulse    = true;

        // ══════════════════════════════════════════════════════════════════
        // GLOBAL RATE LIMITS
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Minimum gap (in campaign hours) between ANY two messages.
        /// 0.15 ≈ 9 game-minutes.
        /// </summary>
        public static float GlobalMessageGapHours = 0.15f;

        /// <summary>Maximum messages shown in a single campaign hour.</summary>
        public static int   MaxMessagesPerHour = 4;

        /// <summary>
        /// Bonus lines allowed after the first Show per event.
        /// Set to 0 for strictly one-line-per-event.
        /// </summary>
        public static int   MaxBonusLinesPerEvent = 1;

        // ══════════════════════════════════════════════════════════════════
        // EVENT-DRIVEN COOLDOWNS  (campaign hours)
        // ══════════════════════════════════════════════════════════════════

        public static float BattleCooldownHours       = 1f;
        public static float RaidCooldownHours          = 2f;
        public static float SiegeCooldownHours         = 2f;
        public static float HeroDeathCooldownHours     = 1f;
        public static float WarPeaceCooldownHours      = 1f;
        public static float SettlementCooldownHours    = 2f;
        public static float ArmyCooldownHours          = 2f;
        public static float PrisonerCooldownHours      = 2f;
        public static float ClanChangeCooldownHours    = 2f;
        public static float KingdomCooldownHours       = 0f;  // rare enough — fire every time
        public static float TournamentCooldownHours    = 8f;  // NEW
        public static float FugitiveCooldownHours      = 6f;  // NEW
        public static float CompanionCooldownHours     = 4f;
        public static float CaravanCooldownHours       = 8f;
        public static float FamilyCooldownHours        = 12f;
        public static float HideoutCooldownHours       = 6f;
        public static float QuestCooldownHours         = 4f;
        public static float RouteArrivalCooldownHours  = 4f;

        // ── Hourly-tick cooldowns (campaign hours) ─────────────────────────
        public static float AmbientCooldownHours  = 6f;
        public static float TravelCooldownHours   = 4f;
        public static float MoraleCooldownHours   = 8f;
        public static float FoodCooldownHours     = 8f;    // NEW
        public static float WoundedCooldownHours  = 12f;   // NEW
        public static float TAOMBridgeCooldownHours = 12f;

        // ── Weekly-tick cooldown ──────────────────────────────────────────
        public static float WeeklyAmbientCooldownHours = 120f;  // NEW — ~5 campaign days

        // ── Probability gates (0–1) ────────────────────────────────────────
        public static float DawnMessageChance    = 0.40f;
        public static float DuskMessageChance    = 0.40f;
        public static float NightMessageChance   = 0.25f;
        public static float SeasonMessageChance  = 0.20f;
        public static float TravelMessageChance  = 0.15f;
        public static float MoraleMessageChance  = 0.30f;
        public static float FoodMessageChance    = 0.70f;  // bypassed when food is critical
        public static float WoundedMessageChance = 0.40f;  // bypassed when wounds are critical
        public static float WeeklyMessageChance  = 0.55f;  // NEW
        public static float RouteArrivalMessageChance = 0.45f;
        public static float TAOMBridgeMessageChance = 0.35f;

        // ── Supply thresholds ─────────────────────────────────────────────

        /// <summary>
        /// Days of food remaining below which a low-food warning fires.
        /// Calculated as Food / (-FoodChange).
        /// </summary>
        public static float FoodWarningDaysThreshold = 3f;

        /// <summary>
        /// Fraction of party wounded before a heavy-casualties message fires.
        /// 0.35 = 35 % wounded.
        /// </summary>
        public static float WoundedThresholdFraction = 0.35f;
    }
}
