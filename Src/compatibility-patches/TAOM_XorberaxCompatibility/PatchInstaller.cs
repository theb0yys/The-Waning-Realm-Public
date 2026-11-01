using System;
using System.Reflection;
using HarmonyLib;

namespace TAOM.XorberaxCompatibility;

internal static class PatchInstaller
{
	internal const string Owner = "TAOM.XorberaxCompatibility.Cleave";

	internal const string XorberaxOwner = "XorberaxLegacy";

	internal static Func<object> ActiveModel;

	internal static void Install(Harmony harmony, MethodInfo hit, MethodInfo model, MethodInfo service)
	{
		try
		{
			harmony.Patch((MethodBase)service, (HarmonyMethod)null, Hook("ServicePostfix", 0), (HarmonyMethod)null, (HarmonyMethod)null);
			harmony.Patch((MethodBase)model, Hook("ModelPrefix", 800), Hook("ModelPostfix", 0), (HarmonyMethod)null, Hook("ModelFinalizer", 0));
			HarmonyMethod val = Hook("HitPostfix", 0);
			val.after = new string[1] { "XorberaxLegacy" };
			harmony.Patch((MethodBase)hit, Hook("HitPrefix", 800), val, (HarmonyMethod)null, Hook("HitFinalizer", 0));
		}
		catch
		{
			harmony.UnpatchAll("TAOM.XorberaxCompatibility.Cleave");
			throw;
		}
	}

	private static HarmonyMethod Hook(string name, int priority)
	{
		return new HarmonyMethod(typeof(PatchInstaller).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic))
		{
			priority = priority
		};
	}

	private static void HitPrefix(object attacker, out ScopeToken __state)
	{
		__state = CleaveContext.BeginHit(attacker);
	}

	private static void HitPostfix(ScopeToken __state, bool __runOriginal, ref float inOutMomentumRemaining)
	{
		CleaveContext.Restore(__state, __runOriginal, ref inOutMomentumRemaining);
	}

	private static Exception HitFinalizer(ScopeToken __state, Exception __exception)
	{
		CleaveContext.EndHit(__state);
		return __exception;
	}

	private static void ModelPrefix(object __instance, object attacker, out ScopeToken __state)
	{
		__state = CleaveContext.BeginModel(attacker, ActiveModel != null && __instance == ActiveModel());
	}

	private static void ServicePostfix(float? __result)
	{
		CleaveContext.ObserveService(__result);
	}

	private static void ModelPostfix(ScopeToken __state, float __result)
	{
		CleaveContext.CompleteModel(__state, __result);
	}

	private static Exception ModelFinalizer(ScopeToken __state, Exception __exception)
	{
		CleaveContext.EndModel(__state);
		return __exception;
	}
}
