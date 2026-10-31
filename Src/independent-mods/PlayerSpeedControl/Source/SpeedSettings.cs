using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace PlayerSpeedControl
{
    public sealed class SpeedSettings : AttributeGlobalSettings<SpeedSettings>
    {
        public override string Id => "PlayerSpeedControl";
        public override string DisplayName => "Player Speed Control";
        public override string FolderName => "PlayerSpeedControl";
        public override string FormatType => "json2";

        [SettingPropertyBool("Enable running adjustment", Order = 0, RequireRestart = false,
            HintText = "Adjust the player character's running speed on foot. Walking, riding and swimming keep their normal speeds.")]
        [SettingPropertyGroup("Running")]
        public bool RunningEnabled { get; set; } = true;

        [SettingPropertyFloatingInteger("Running speed multiplier", 0.5f, 3f, "0.00'x'", Order = 1, RequireRestart = false,
            HintText = "1.00x is normal speed. Applies to the player while running on foot, including combat.")]
        [SettingPropertyGroup("Running")]
        public float RunningMultiplier { get; set; } = 1f;

        [SettingPropertyBool("Enable map travel adjustment", Order = 0, RequireRestart = false,
            HintText = "Adjust the player party's land travel speed. Attached parties follow their army leader normally.")]
        [SettingPropertyGroup("Campaign map")]
        public bool TravelEnabled { get; set; } = true;

        [SettingPropertyFloatingInteger("Map travel speed multiplier", 0.5f, 3f, "0.00'x'", Order = 1, RequireRestart = false,
            HintText = "Multiplies normal land travel speed after terrain, weight, perks and other native effects. 1.00x is normal speed.")]
        [SettingPropertyGroup("Campaign map")]
        public float TravelMultiplier { get; set; } = 1f;
    }

    internal static class SpeedOptions
    {
        internal static volatile float Running = 1f;
        internal static volatile float Travel = 1f;

        internal static void Refresh()
        {
            var settings = SpeedSettings.Instance;
            Running = settings != null && settings.RunningEnabled ? SpeedPolicy.Clamp(settings.RunningMultiplier) : 1f;
            Travel = settings != null && settings.TravelEnabled ? SpeedPolicy.Clamp(settings.TravelMultiplier) : 1f;
        }
    }
}
