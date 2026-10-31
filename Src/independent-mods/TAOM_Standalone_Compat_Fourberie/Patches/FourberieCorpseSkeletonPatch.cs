using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace Fourberie_CompatPatch
{
    // The raycast bone belongs to the corpse, not the player who grabbed it.
    [HarmonyPatch]
    public static class FourberieCorpseSkeletonPatch
    {
        private static Type _stealthType;
        private static MethodInfo _target;

        [HarmonyPrepare]
        public static bool Prepare()
        {
            _stealthType = AccessTools.TypeByName("Fourberie.FStealthMissionLogic");
            var draggingType = AccessTools.TypeByName("SandBox.Missions.MissionLogics.CorpseDraggingMissionLogic");
            _target = draggingType == null ? null : AccessTools.Method(draggingType,
                "OnAgentInteraction", new[] { typeof(Agent), typeof(Agent), typeof(sbyte) });
            return _stealthType != null && _target != null;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod() { return _target; }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var visualsGetter = AccessTools.PropertyGetter(typeof(Agent), nameof(Agent.AgentVisuals));
            int match = -1;
            for (int i = 1; i < code.Count; i++)
            {
                if (code[i - 1].opcode != OpCodes.Ldarg_1 || !code[i].Calls(visualsGetter))
                    continue;
                if (match >= 0 || code[i].labels.Count != 0 || code[i].blocks.Count != 0)
                    throw new InvalidOperationException("Unsupported corpse skeleton IL; no mapping change applied.");
                match = i;
            }
            if (match < 0)
                throw new InvalidOperationException("Corpse skeleton lookup not found; no mapping change applied.");

            code.InsertRange(match, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FourberieCorpseSkeletonPatch), nameof(ResolveSkeletonAgent)))
            });
            return code;
        }

        public static Agent ResolveSkeletonAgent(Agent carrier, Agent corpse, MissionBehavior draggingLogic)
        {
            if (carrier == null || corpse == null || !carrier.IsHuman || !corpse.IsHuman || draggingLogic == null)
                return carrier;
            var mission = draggingLogic.Mission;
            if (mission != null && _stealthType != null)
            {
                foreach (var behavior in mission.MissionBehaviors)
                    if (behavior.GetType() == _stealthType)
                        return corpse;
            }
            return carrier;
        }
    }
}
