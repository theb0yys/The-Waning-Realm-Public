using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TaomRetinuesCompat
{
    internal static class RetinuesColorBridge
    {
        private static Func<AgentBuildData, bool> _isManagedCombatTroop;
        private static readonly MethodInfo EquipmentGetter = typeof(AgentBuildData).GetProperty("AgentOverridenSpawnEquipment").GetGetMethod();
        private static readonly MethodInfo GuardGetter = typeof(RetinuesColorBridge).GetMethod(nameof(ReadEquipmentGuard), BindingFlags.Static | BindingFlags.NonPublic);

        internal static void Install(Harmony harmony, MethodInfo taomPrefix, Func<AgentBuildData, bool> classifier)
        {
            if (_isManagedCombatTroop != null) throw new InvalidOperationException("Bridge already installed.");
            if (harmony == null || taomPrefix == null || classifier == null) throw new ArgumentNullException();
            ParameterInfo[] parameters = taomPrefix.GetParameters();
            if (!taomPrefix.IsStatic || taomPrefix.ReturnType != typeof(bool) || parameters.Length != 1 || parameters[0].ParameterType != typeof(AgentBuildData))
                throw new MissingMethodException("Unsupported TAOM spawn-prefix signature.");
            _isManagedCombatTroop = classifier;
            try { harmony.Patch(taomPrefix, transpiler: new HarmonyMethod(typeof(RetinuesColorBridge).GetMethod(nameof(Transpile), BindingFlags.Static | BindingFlags.NonPublic))); }
            catch { Uninstall(harmony); throw; }
        }

        internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
        {
            var result = new List<CodeInstruction>();
            int matches = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                var copy = new CodeInstruction(instruction);
                if ((copy.opcode == OpCodes.Call || copy.opcode == OpCodes.Callvirt) && Equals(copy.operand, EquipmentGetter))
                {
                    copy.opcode = OpCodes.Call;
                    copy.operand = GuardGetter;
                    matches++;
                }
                result.Add(copy);
            }
            if (matches != 1) throw new InvalidOperationException("Expected one TAOM equipment guard; found " + matches + ".");
            return result;
        }

        internal static void Uninstall(Harmony harmony)
        {
            harmony.UnpatchAll(harmony.Id);
            _isManagedCombatTroop = null;
        }

        // Change only the guard read; retain the exact equipment object for spawning.
        private static Equipment ReadEquipmentGuard(AgentBuildData data)
        {
            Equipment equipment = data.AgentOverridenSpawnEquipment;
            if (equipment == null || _isManagedCombatTroop == null) return equipment;
            try { return _isManagedCombatTroop(data) ? null : equipment; }
            catch (Exception) { return equipment; }
        }
    }
}
