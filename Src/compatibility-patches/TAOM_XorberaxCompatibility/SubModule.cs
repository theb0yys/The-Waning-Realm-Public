using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TAOM.XorberaxCompatibility;

public sealed class SubModule : MBSubModuleBase
{
	internal const string SupportedTaomBuild = "fd85dca4-5ad3-4ed9-a68f-989d7d3981fe";

    private Harmony harmony;

	private bool attempted;

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		if (attempted)
		{
			return;
		}
		attempted = true;
		try
		{
			MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(Mission), "MeleeHitCallback", (Type[])null, (Type[])null);
			Type type = AccessTools.TypeByName("TAOM.Features.CombatMechanics.Models.TaomCombatMechanicsModel");
			Type type2 = AccessTools.TypeByName("TAOM.Features.CombatMechanics.CreatureCombatService");
			Type type3 = AccessTools.TypeByName("SandBox.GameComponents.SandboxAgentApplyDamageModel");
			MethodInfo methodInfo2 = ((type == null) ? null : AccessTools.DeclaredMethod(type, "CalculateRemainingMomentum", (Type[])null, (Type[])null));
			MethodInfo methodInfo3 = ((type2 == null) ? null : AccessTools.DeclaredMethod(type2, "CalculateCleaveMomentum", (Type[])null, (Type[])null));
			RequireBuild(methodInfo?.Module, "4267abec-e4ba-4746-94b7-47cbe3ba79f8", "Bannerlord");
			RequireBuild(methodInfo2?.Module, SupportedTaomBuild, "TAOM");
			RequireBuild(methodInfo3?.Module, SupportedTaomBuild, "TAOM creature service");
			RequireBuild(type3?.Module, "7c957d2a-5cbc-4e29-b5b4-8f4fc6f82313", "Sandbox");
			if (typeof(Harmony).Assembly.GetName().Version != new Version(2, 4, 2, 0))
			{
				throw new InvalidOperationException("Requires the inspected Harmony 2.4.2 runtime.");
			}
			RequireBuild((Harmony.GetPatchInfo((MethodBase)methodInfo)?.Postfixes.FirstOrDefault((Patch p) => p.owner == "XorberaxLegacy" && p.PatchMethod.Name == "MeleeHitCallbackPostfix" && p.PatchMethod.DeclaringType?.FullName == "XorberaxLegacy.Patches.CutThroughEveryonePatchMeleeHit") ?? throw new InvalidOperationException("Xorberax cleave postfix is missing; load this module after XorberaxLegacy and TAOM.")).PatchMethod.Module, "1226f98a-2a9d-44c7-b1c4-c00f46405cb9", "Xorberax Legacy");
			if (methodInfo.GetParameters().Length != 11 || methodInfo2.GetParameters().Length != 7 || methodInfo3.ReturnType != typeof(float?) || methodInfo3.GetParameters().Length != 3)
			{
				throw new InvalidOperationException("Unsupported cleave method signatures.");
			}
			PatchInstaller.ActiveModel = GetActiveModel;
			CleaveContext.PrepareThread();
			harmony = new Harmony("TAOM.XorberaxCompatibility.Cleave");
			PatchInstaller.Install(harmony, methodInfo, methodInfo2, methodInfo3);
			InformationManager.DisplayMessage(new InformationMessage("TAOM / Xorberax: creature cleave compatibility 0.1.1 active."));
		}
		catch (Exception ex)
		{
			Harmony obj = harmony;
			if (obj != null)
			{
				obj.UnpatchAll("TAOM.XorberaxCompatibility.Cleave");
			}
			PatchInstaller.ActiveModel = null;
			InformationManager.DisplayMessage(new InformationMessage("TAOM / Xorberax patch inactive: " + ex.Message));
		}
	}

	private static object GetActiveModel()
	{
		MissionGameModels current = MissionGameModels.Current;
		if (current == null)
		{
			return null;
		}
		return current.AgentApplyDamageModel;
	}

	internal static void RequireBuild(System.Reflection.Module module, string mvid, string name)
	{
		if (module == null || module.ModuleVersionId != new Guid(mvid))
		{
			throw new InvalidOperationException(name + " build mismatch (expected " + mvid + "; actual " + (module == null ? "missing" : module.ModuleVersionId.ToString()) + "); cleave patch 0.1.1 was not installed.");
		}
	}

	protected override void OnSubModuleUnloaded()
	{
		Harmony obj = harmony;
		if (obj != null)
		{
			obj.UnpatchAll("TAOM.XorberaxCompatibility.Cleave");
		}
		PatchInstaller.ActiveModel = null;
		base.OnSubModuleUnloaded();
	}
}

