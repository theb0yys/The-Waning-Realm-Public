using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;

namespace CustomBattleReforged
{
    public sealed class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "taom.custombattlereforged";
        private bool _attempted;

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_attempted) return;
            _attempted = true;
            var harmony = new Harmony(HarmonyId);
            try
            {
                if (!ModuleHelper.IsModuleActive("TAOM")) throw new InvalidOperationException("Enable TAOM before Custom Battle Reforged.");
                VerifyHash(typeof(CustomBattleVM).Assembly.Location, "8073B0AE60CAA77A7E3BE5FE892BDCC427E11337C7F1338743F857B9B30EBB25");
                VerifyHash(typeof(TaleWorlds.Core.BasicCharacterObject).Assembly.Location, "3E5CECE8CA42FAF3FDDE27632519ED2A6F86D1EC6D1A742867729F00001689BF");
                VerifyHash(Path.Combine(ModuleHelper.GetModuleFullPath("TAOM"), "bin", "Win64_Shipping_Client", "TAOM.dll"), "AEEC07AA61C84F45E9E8662C294CFF68B000F041D8FCA453CF30058CB9AD14D8");
                harmony.PatchAll(typeof(SubModule).Assembly);
                InstallTaomServicePatches(harmony);
                Debug.Print("[Custom Battle Reforged] Installed custom-battle-only patches.");
            }
            catch (Exception error)
            {
                harmony.UnpatchAll(HarmonyId);
                RuntimeCatalog.Clear();
                ReportUnavailable(error.Message);
            }
        }

        private static void VerifyHash(string path, string expected)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") != expected)
                    throw new InvalidOperationException("Unsupported game build: " + Path.GetFileName(path));
        }

        private static void InstallTaomServicePatches(Harmony harmony)
        {
            Type service = AccessTools.TypeByName("TAOM.Features.CustomBattles.CustomBattleService");
            if (service == null) throw new MissingMemberException("TAOM custom battle service is unavailable.");
            PatchService(harmony, service, "GetFactionIds", Type.EmptyTypes, nameof(FactionIds));
            PatchService(harmony, service, "GetCommanderIds", Type.EmptyTypes, nameof(CommanderIds));
            PatchService(harmony, service, "GetCommanderIdsForFaction", new[] { typeof(string) }, nameof(FactionCommanders));
            PatchService(harmony, service, "GetCommanderIdsForFaction", new[] { typeof(string), typeof(int) }, nameof(LimitedFactionCommanders));
        }

        private static void PatchService(Harmony harmony, Type service, string method, Type[] arguments, string patch)
        {
            MethodInfo target = AccessTools.Method(service, method, arguments);
            if (target == null || target.ReturnType != typeof(IReadOnlyList<string>)) throw new MissingMethodException(service.FullName, method);
            var postfix = new HarmonyMethod(typeof(SubModule), patch) { priority = Priority.Last };
            harmony.Patch(target, postfix: postfix);
        }

        private static void FactionIds(ref IReadOnlyList<string> __result)
        {
            if (!RuntimeCatalog.Active) return;
            var ids = new List<string>(RuntimeCatalog.Factions.Count);
            foreach (var faction in RuntimeCatalog.Factions) ids.Add(faction.StringId);
            __result = ids;
        }

        private static void CommanderIds(ref IReadOnlyList<string> __result)
        {
            if (!RuntimeCatalog.Active) return;
            var ids = new List<string>(RuntimeCatalog.Commanders.Count);
            foreach (var commander in RuntimeCatalog.Commanders) ids.Add(commander.StringId);
            __result = ids;
        }

        private static void FactionCommanders(string factionId, ref IReadOnlyList<string> __result)
        {
            LimitedFactionCommanders(factionId, int.MaxValue, ref __result);
        }

        private static void LimitedFactionCommanders(string factionId, int takeMax, ref IReadOnlyList<string> __result)
        {
            if (!RuntimeCatalog.Active) return;
            var ids = new List<string>();
            if (factionId != null && takeMax > 0 && RuntimeCatalog.CommandersByCulture.TryGetValue(factionId, out var commanders))
                foreach (var commander in commanders) { if (ids.Count >= takeMax) break; ids.Add(commander.StringId); }
            __result = ids;
        }

        internal static void ReportUnavailable(string reason)
        {
            string message = "Custom Battle Reforged could not activate: " + reason;
            Debug.Print("[Custom Battle Reforged] " + message);
            InformationManager.DisplayMessage(new InformationMessage(message));
        }
    }
}
