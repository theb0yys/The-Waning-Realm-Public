using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TaomRetinuesCompat
{
    public sealed class SubModule : MBSubModuleBase
    {
        internal const string HarmonyId = "taom.compat.retinues.colors.v1";
        internal const string TaomHash = "AEEC07AA61C84F45E9E8662C294CFF68B000F041D8FCA453CF30058CB9AD14D8";
        internal const string RetinuesHash = "8398BFE484A209A693DC218C8614A51674DEB9E7DAA101BEF8108C3D0FD1724A";
        private Harmony _harmony;
        private bool _attempted;

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            if (_attempted) return;
            _attempted = true;
            try
            {
                Assembly taom = FindLoaded("TAOM");
                Assembly retinues = FindLoaded("Retinues");
                RequireHash(taom, TaomHash);
                RequireHash(retinues, RetinuesHash);
                MethodInfo prefix = taom.GetType("TAOM.Features.BannerColorPersistence.Hooks.Mission_SpawnAgent_Patch", true)
                    .GetMethod("Prefix", BindingFlags.Public | BindingFlags.Static);
                RequireSpawnHooks(prefix, retinues);
                _harmony = new Harmony(HarmonyId);
                RetinuesColorBridge.Install(_harmony, prefix, RetinuesEligibility.IsManagedCombatTroop);
                InformationManager.DisplayMessage(new InformationMessage("[TAOM / Retinues] Clan-colour bridge 0.1.1 enabled."));
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("[TAOM / Retinues] Clan-colour bridge 0.1.1 disabled: " + ex.Message));
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            if (_harmony != null) RetinuesColorBridge.Uninstall(_harmony);
            _harmony = null;
            base.OnSubModuleUnloaded();
        }

        internal static void RequireSpawnHooks(MethodInfo taomPrefix, Assembly retinues)
        {
            MethodInfo spawn = typeof(Mission).GetMethod("SpawnAgent", new[] { typeof(AgentBuildData), typeof(bool) });
            Patches hooks = Harmony.GetPatchInfo(spawn);
            bool taomFound = false, retinuesFound = false;
            if (hooks != null)
                foreach (Patch patch in hooks.Prefixes)
                {
                    taomFound |= patch.PatchMethod == taomPrefix;
                    retinuesFound |= patch.PatchMethod.DeclaringType?.Assembly == retinues &&
                        patch.PatchMethod.DeclaringType.FullName == "Retinues.Behaviors.Agents.Patches.SpawnResolverPatch";
                }
            if (!taomFound || !retinuesFound)
                throw new InvalidOperationException("TAOM colour and Retinues spawn hooks must both be enabled before this patch.");
        }

        private static Assembly FindLoaded(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetName().Name == name) return assembly;
            throw new InvalidOperationException(name + " must be loaded before this patch.");
        }

        internal static void RequireHash(Assembly assembly, string expected)
        {
            using (var stream = File.OpenRead(assembly.Location))
            using (var sha = SHA256.Create())
                if (!string.Equals(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""), expected, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Unsupported " + assembly.GetName().Name + " DLL; targets verified TAOM 2.0.29.5 / Retinues v2.0.0.13.");
        }
    }
}
