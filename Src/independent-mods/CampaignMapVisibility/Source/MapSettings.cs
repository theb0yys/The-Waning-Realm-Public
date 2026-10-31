using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace CampaignMapVisibility
{
    public sealed class MapSettings : AttributeGlobalSettings<MapSettings>
    {
        public override string Id => "CampaignMapVisibility";
        public override string DisplayName => "Campaign Map Visibility";
        public override string FolderName => "CampaignMapVisibility";
        public override string FormatType => "json2";

        [SettingPropertyBool("Keep map markers visible when zoomed out", Order = 0, RequireRestart = false,
            HintText = "Keep settlement names, moving party labels and battle markers visible at every zoom level, and hide floating settlement eye icons. Normal scouting still applies.")]
        [SettingPropertyGroup("Map visibility")]
        public bool KeepMarkersVisible { get; set; } = true;

        [SettingPropertyBool("Reveal entire map", Order = 1, RequireRestart = false,
            HintText = "Show moving party labels and battle markers beyond scouting range. Turning this off restores normal scouting visibility. Does not discover hidden locations or change campaign AI.")]
        [SettingPropertyGroup("Map visibility")]
        public bool RevealEntireMap { get; set; } = false;
    }

    internal static class MapOptions
    {
        // Only the UI thread reads MCM. Worker-thread patches read these primitive snapshots.
        internal static volatile bool KeepMarkers = true;
        internal static volatile bool Reveal;

        internal static void Refresh()
        {
            var settings = MapSettings.Instance;
            KeepMarkers = settings == null || settings.KeepMarkersVisible;
            Reveal = settings != null && settings.RevealEntireMap;
        }
    }
}
