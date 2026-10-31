using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace TroopLimitExpander
{
    internal sealed class TroopLimitMcmSettings : AttributeGlobalSettings<TroopLimitMcmSettings>
    {
        public override string Id => "WANINGTROOPLIMITEXPANDER";
        public override string DisplayName => "Waning Troop Limit Expander";
        public override string FolderName => "WANINGTROOPLIMITEXPANDER";
        public override string FormatType => "json2";

        [SettingPropertyInteger("Custom battle troops per side", 200, 25000,
            Order = 0, RequireRestart = false,
            HintText = "Maximum roster size accepted per side in Custom Battle.")]
        [SettingPropertyGroup("Battle Size", GroupOrder = 0)]
        public int CustomBattleTroopsPerSideCap { get; set; } = 25000;

        [SettingPropertyBool("Expand active battle size",
            Order = 1, RequireRestart = true,
            HintText = "Raises active battle-size caps in field and siege missions. Keep off for the safest first-battle profile.")]
        [SettingPropertyGroup("Battle Size", GroupOrder = 0)]
        public bool ExpandActiveBattleSize { get; set; } = false;

        [SettingPropertyInteger("Field active troop cap", 200, 25000,
            Order = 2, RequireRestart = true,
            HintText = "Requested active troop cap for field battles before the safety cap is applied.")]
        [SettingPropertyGroup("Battle Size", GroupOrder = 0)]
        public int FieldBattleTroopCap { get; set; } = 1600;

        [SettingPropertyInteger("Siege active troop cap", 200, 16000,
            Order = 3, RequireRestart = true,
            HintText = "Requested active troop cap for siege battles before the safety cap is applied.")]
        [SettingPropertyGroup("Battle Size", GroupOrder = 0)]
        public int SiegeBattleTroopCap { get; set; } = 1200;

        [SettingPropertyInteger("Active-agent safety cap", 400, 8000,
            Order = 4, RequireRestart = true,
            HintText = "Global safety ceiling applied to field and siege active troop caps.")]
        [SettingPropertyGroup("Battle Size", GroupOrder = 0)]
        public int ActiveAgentSafetyCap { get; set; } = 1800;

        [SettingPropertyBool("Custom battle crash guard",
            Order = 0, RequireRestart = true,
            HintText = "Keeps first-battle active deployment conservative while allowing large Custom Battle reserves.")]
        [SettingPropertyGroup("Safety", GroupOrder = 1)]
        public bool CustomBattleCrashGuard { get; set; } = true;

        [SettingPropertyInteger("Initial deployment side percent", 45, 75,
            Order = 1, RequireRestart = true,
            HintText = "Maximum initial deployment ratio per side. Crash guard caps this to 60 percent.")]
        [SettingPropertyGroup("Safety", GroupOrder = 1)]
        public int InitialDeploymentMaxSidePercent { get; set; } = 50;

        [SettingPropertyInteger("Reinforcement waves", 0, 96,
            Order = 0, RequireRestart = true,
            HintText = "Maximum reinforcement waves used to stage very large rosters.")]
        [SettingPropertyGroup("Reinforcements", GroupOrder = 2)]
        public int LandReinforcementWaves { get; set; } = 50;

        [SettingPropertyInteger("Wave batch percent", 1, 50,
            Order = 1, RequireRestart = true,
            HintText = "Percentage of a side roster released in each reinforcement batch.")]
        [SettingPropertyGroup("Reinforcements", GroupOrder = 2)]
        public int LandWaveBatchPercent { get; set; } = 4;

        [SettingPropertyInteger("Reinforcement interval seconds", 1, 30,
            Order = 2, RequireRestart = true,
            HintText = "Seconds between reinforcement checks.")]
        [SettingPropertyGroup("Reinforcements", GroupOrder = 2)]
        public int LandReinforcementIntervalSeconds { get; set; } = 6;

        [SettingPropertyBool("Show in-battle overlay",
            Order = 0, RequireRestart = false,
            HintText = "Adds or removes the in-battle troop telemetry overlay.")]
        [SettingPropertyGroup("Overlay", GroupOrder = 3)]
        public bool ShowBattleOverlay { get; set; } = true;

        [SettingPropertyBool("Show startup diagnostics",
            Order = 1, RequireRestart = false,
            HintText = "Shows the runtime summary and safety note at startup.")]
        [SettingPropertyGroup("Overlay", GroupOrder = 3)]
        public bool ShowRuntimeDiagnostics { get; set; } = true;
    }
}
