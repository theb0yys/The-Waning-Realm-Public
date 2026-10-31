using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace PlayerJumpHeight
{
    public sealed class JumpSettings : AttributeGlobalSettings<JumpSettings>
    {
        public override string Id => "PlayerJumpHeight";
        public override string DisplayName => "Player Jump Height";
        public override string FolderName => "PlayerJumpHeight";
        public override string FormatType => "json2";

        [SettingPropertyBool("Enable higher jumps", Order = 0, RequireRestart = false,
            HintText = "Adjust your character's jump on foot. Changes apply when you next enter a scene or battle. Fall damage still applies.")]
        [SettingPropertyGroup("Player jump")]
        public bool Enabled { get; set; } = true;

        [SettingPropertyFloatingInteger("Jump height multiplier", 1f, 10f, "0.0'x'", Order = 1, RequireRestart = false,
            HintText = "1x is normal height; default 5x. Applies on your next scene entry or spawn. Other troops and mounts keep normal jumps.")]
        [SettingPropertyGroup("Player jump")]
        public float HeightMultiplier { get; set; } = 5f;
    }
}
