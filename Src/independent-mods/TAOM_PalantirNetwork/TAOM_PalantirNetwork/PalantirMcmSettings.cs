using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace TAOM_PalantirNetwork
{
    internal sealed class PalantirMcmSettings : AttributeGlobalSettings<PalantirMcmSettings>
    {
        public override string Id          => "TAOM_PalantirNetwork";
        public override string DisplayName => "Palantir Network";
        public override string FolderName  => "TAOM_PalantirNetwork";
        public override string FormatType  => "json2";

        // ── General ──────────────────────────────────────────────────────────
        [SettingPropertyBool("Enable Palantir Network",
            Order = 0, RequireRestart = false,
            HintText = "Enables or disables the entire Palantir Network mod. " +
                       "Disable to stop tracking sightings.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public bool Enabled { get; set; } = true;

        [SettingPropertyText("Hotkey",
            Order = 1, RequireRestart = false,
            HintText = "Key name to open the Palantir menu from the campaign map. " +
                       "Uses InputKey enum names: P, O, F5, Numpad1, etc. Default: P")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public string HotkeyName { get; set; } = "P";

        [SettingPropertyInteger("Memory window (days)", 1, 30,
            Order = 2, RequireRestart = false,
            HintText = "How many in-game days a sighting remains in the Palantir's " +
                       "memory before it fades.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public int IntelDays { get; set; } = 5;

        [SettingPropertyInteger("Max results shown", 5, 80,
            Order = 3, RequireRestart = false,
            HintText = "Maximum number of sightings displayed in a single Palantir report.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public int MaxResults { get; set; } = 25;

        [SettingPropertyBool("Show threat pulse",
            Order = 4, RequireRestart = false,
            HintText = "Shows a periodic campaign message when recent hostile sightings form a serious threat picture.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public bool ShowThreatPulse { get; set; } = true;

        [SettingPropertyInteger("Threat pulse interval (days)", 1, 14,
            Order = 5, RequireRestart = false,
            HintText = "Minimum in-game days between Palantir threat pulse messages.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public int ThreatPulseIntervalDays { get; set; } = 3;

        [SettingPropertyInteger("Threat pulse threshold", 20, 100,
            Order = 6, RequireRestart = false,
            HintText = "Minimum danger score needed before the Palantir shows a threat pulse.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public int ThreatPulseThreshold { get; set; } = 55;

        [SettingPropertyBool("Detailed sighting history",
            Order = 7, RequireRestart = false,
            HintText = "Shows first seen, observation count, peak strength, and danger trend lines inside the Palantir report.")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public bool ShowHistoryDetails { get; set; } = true;

        // ── Filters ──────────────────────────────────────────────────────────
        [SettingPropertyBool("Hostiles only (default)",
            Order = 0, RequireRestart = false,
            HintText = "Starting state of the 'Hostiles only' toggle in a new campaign. " +
                       "Saved per-campaign after the first change.")]
        [SettingPropertyGroup("Filters", GroupOrder = 1)]
        public bool HostilesOnly { get; set; } = true;

        [SettingPropertyBool("Show caravans",
            Order = 1, RequireRestart = false,
            HintText = "Include caravan parties in the Palantir report.")]
        [SettingPropertyGroup("Filters", GroupOrder = 1)]
        public bool ShowCaravans { get; set; } = true;

        [SettingPropertyBool("Show armies",
            Order = 2, RequireRestart = false,
            HintText = "Include armies in the Palantir report.")]
        [SettingPropertyGroup("Filters", GroupOrder = 1)]
        public bool ShowArmies { get; set; } = true;

        [SettingPropertyBool("Show bandits",
            Order = 3, RequireRestart = false,
            HintText = "Include bandit parties in the Palantir report.")]
        [SettingPropertyGroup("Filters", GroupOrder = 1)]
        public bool ShowBandits { get; set; } = true;

        // ── TAOM Compatibility ────────────────────────────────────────────────
        [SettingPropertyBool("Add Palantir menu access",
            Order = 0, RequireRestart = true,
            HintText = "Adds 'Consult the Palantir' options to standard campaign menus and any extra TAOM menu IDs below.")]
        [SettingPropertyGroup("TAOM Compatibility", GroupOrder = 2)]
        public bool EnableRootMenuAccess { get; set; } = true;

        [SettingPropertyText("Additional root menu IDs (CSV)",
            Order = 1, RequireRestart = true,
            HintText = "Comma-separated list of extra TAOM GameMenu IDs from which the " +
                       "Palantir option should be accessible. Example: taom_town_menu, " +
                       "taom_castle_menu. Requires restart.")]
        [SettingPropertyGroup("TAOM Compatibility", GroupOrder = 2)]
        public string AdditionalRootMenuIdsCsv { get; set; } = "";

        // ── Debug ─────────────────────────────────────────────────────────────
        [SettingPropertyBool("Debug: log menu IDs",
            Order = 0, RequireRestart = false,
            HintText = "Logs the ID of every GameMenu that is opened. Useful for " +
                       "finding menu IDs for the compatibility list above.")]
        [SettingPropertyGroup("Debug", GroupOrder = 3)]
        public bool DebugShowMenuIds { get; set; } = false;

        [SettingPropertyBool("Debug: log sightings",
            Order = 1, RequireRestart = false,
            HintText = "Logs each party sighting to the in-game message log. " +
                       "May be noisy in large campaigns.")]
        [SettingPropertyGroup("Debug", GroupOrder = 3)]
        public bool DebugShowSightings { get; set; } = false;
    }
}
