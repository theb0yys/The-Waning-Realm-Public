using System;
using System.IO;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace IAD_CompatPatch
{
    public class SubModule : MBSubModuleBase
    {
        private Harmony _harmony;
        private const string HarmonyId = "iad.compatpatch.1.4.5";

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Logs",
            "iad_compatpatch.log");

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            SafeLog("SubModule loading...");

            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll();

            SafeLog("Harmony.PatchAll() called.");

            InformationManager.DisplayMessage(new InformationMessage("[IAD_CompatPatch] Loaded."));
        }

        protected override void OnSubModuleUnloaded()
        {
            SafeLog("SubModule unloading...");
            _harmony?.UnpatchAll(HarmonyId);
            base.OnSubModuleUnloaded();
        }

        private static void SafeLog(string msg)
        {
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
