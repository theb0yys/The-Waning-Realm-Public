using System.Runtime.CompilerServices;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

[assembly: InternalsVisibleTo("PlayerSpeedControl.Tests")]

namespace PlayerSpeedControl
{
    public sealed class SubModule : MBSubModuleBase
    {
        private Harmony harmony;
        private float elapsed;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            harmony = new Harmony("PlayerSpeedControl.v1");
            harmony.PatchAll(typeof(SubModule).Assembly);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            elapsed += dt;
            if (elapsed < 0.2f) return;
            elapsed = 0f;
            SpeedOptions.Refresh();
        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            base.OnMissionBehaviorInitialize(mission);
            mission.AddMissionBehavior(new RunningSpeedMission());
        }

        protected override void OnSubModuleUnloaded()
        {
            RunningSpeedPatch.Release(false);
            harmony?.UnpatchAll("PlayerSpeedControl.v1");
            base.OnSubModuleUnloaded();
        }
    }

    internal sealed class RunningSpeedMission : MissionBehavior
    {
        private Agent previous;
        private float previousFactor = 1f;
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public override void OnMissionTick(float dt)
        {
            Agent current = Mission.MainAgent;
            float factor = RunningSpeedPatch.Factor(current);
            if (ReferenceEquals(current, previous) && factor == previousFactor) return;
            if (!ReferenceEquals(current, previous)) RunningSpeedPatch.Release();
            previous = current;
            previousFactor = factor;
            if (current != null && current.IsActive()) current.UpdateAgentProperties();
        }

        public override void OnRemoveBehavior()
        {
            // Native entities may already be tearing down; discard our state without a native push.
            RunningSpeedPatch.Release(false);
            previous = null;
            base.OnRemoveBehavior();
        }
    }
}
