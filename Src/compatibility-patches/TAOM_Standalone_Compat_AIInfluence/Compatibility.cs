using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TaomAIInfluenceCompat;

public sealed class SubModule : MBSubModuleBase
{
	private Harmony _harmony;

	internal const string TaomHash = "AEEC07AA61C84F45E9E8662C294CFF68B000F041D8FCA453CF30058CB9AD14D8";

	internal const string AIHash = "472658F15A0EE00B6B05904830FDA257593653F74FDF3C8FEC1345267A73862D";

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		try
		{
			Assembly assembly = FindLoaded("TAOM");
			Assembly assembly2 = FindLoaded("AIInfluence");
			RequireHash(assembly, "AEEC07AA61C84F45E9E8662C294CFF68B000F041D8FCA453CF30058CB9AD14D8");
			RequireHash(assembly2, "472658F15A0EE00B6B05904830FDA257593653F74FDF3C8FEC1345267A73862D");
			Type type = assembly.GetType("TAOM.Adapters.AllianceAdapter", throwOnError: true);
			MethodInfo method = assembly2.GetType("AIInfluence.Diplomacy.DiplomacyPatches", throwOnError: true).GetMethod("WithBypass", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(Action) }, null);
			if (method == null || method.ReturnType != typeof(void))
			{
				throw new MissingMethodException("AI Influence WithBypass(Action) is unavailable.");
			}
			Action<Action> withBypass = (Action<Action>)Delegate.CreateDelegate(typeof(Action<Action>), method);
			_harmony = new Harmony("taom.compat.aiinfluence.diplomacy.v1");
			DiplomacyBridge.Install(_harmony, type, withBypass);
			InformationManager.DisplayMessage(new InformationMessage("[TAOM / AI Influence] Diplomacy bridge 0.1.1 enabled."));
		}
		catch (Exception ex)
		{
			InformationManager.DisplayMessage(new InformationMessage("[TAOM / AI Influence] Bridge disabled: " + ex.Message));
		}
	}

	protected override void OnSubModuleUnloaded()
	{
		if (_harmony != null)
		{
			DiplomacyBridge.Uninstall(_harmony);
		}
		_harmony = null;
		base.OnSubModuleUnloaded();
	}

	private static Assembly FindLoaded(string name)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name == name)
			{
				return assembly;
			}
		}
		throw new InvalidOperationException(name + " must load before this patch.");
	}

	internal static void RequireHash(Assembly assembly, string expected)
	{
		using FileStream inputStream = File.OpenRead(assembly.Location);
		using SHA256 sHA = SHA256.Create();
		if (!string.Equals(BitConverter.ToString(sHA.ComputeHash(inputStream)).Replace("-", ""), expected, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Unverified " + assembly.GetName().Name + " build; bridge 0.1.1 requires the inspected TAOM 2.0.29.5 and AI Influence DLLs.");
		}
	}
}
internal static class DiplomacyBridge
{
	private sealed class Binding
	{
		internal MethodInfo Original;

		internal MethodInfo Replacement;
	}

	private static readonly Dictionary<MethodBase, Binding> Bindings = new Dictionary<MethodBase, Binding>();

	private static Action<Action> _withBypass;

	internal static void Install(Harmony harmony, Type adapter, Action<Action> withBypass)
	{
		if (harmony == null || adapter == null || withBypass == null)
		{
			throw new ArgumentNullException("A verified adapter and bypass delegate are required.");
		}
		if (_withBypass != null)
		{
			throw new InvalidOperationException("Bridge already installed.");
		}
		try
		{
			AddBinding(adapter, "StartAlliance", typeof(IAllianceCampaignBehavior), "StartAlliance", new Type[2]
			{
				typeof(Kingdom),
				typeof(Kingdom)
			});
			AddBinding(adapter, "DeclareWar", typeof(DeclareWarAction), "ApplyByDefault", new Type[2]
			{
				typeof(IFaction),
				typeof(IFaction)
			});
			AddBinding(adapter, "MakePeace", typeof(MakePeaceAction), "Apply", new Type[2]
			{
				typeof(IFaction),
				typeof(IFaction)
			});
			_withBypass = withBypass;
			HarmonyMethod val = new HarmonyMethod(typeof(DiplomacyBridge).GetMethod("Transpile", BindingFlags.Static | BindingFlags.NonPublic));
			foreach (MethodBase key in Bindings.Keys)
			{
				harmony.Patch(key, (HarmonyMethod)null, (HarmonyMethod)null, val, (HarmonyMethod)null);
			}
		}
		catch
		{
			Uninstall(harmony);
			throw;
		}
	}

	private static void AddBinding(Type adapter, string name, Type nativeType, string nativeName, Type[] nativeArguments)
	{
		MethodInfo method = adapter.GetMethod(name, new Type[2]
		{
			typeof(string),
			typeof(string)
		});
		MethodInfo method2 = nativeType.GetMethod(nativeName, nativeArguments);
		MethodInfo method3 = typeof(DiplomacyBridge).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
		if (method == null || method.IsStatic || method.ReturnType != typeof(void) || method2 == null || method3 == null)
		{
			throw new MissingMethodException("Unsupported adapter/native signature: " + name);
		}
		Bindings.Add(method, new Binding
		{
			Original = method2,
			Replacement = method3
		});
	}

	internal static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		if (!Bindings.TryGetValue(__originalMethod, out var value))
		{
			throw new InvalidOperationException("Unregistered TAOM adapter method.");
		}
		List<CodeInstruction> list = new List<CodeInstruction>();
		int num = 0;
		foreach (CodeInstruction instruction in instructions)
		{
			CodeInstruction val = new CodeInstruction(instruction);
			if ((val.opcode == OpCodes.Call || val.opcode == OpCodes.Callvirt) && object.Equals(val.operand, value.Original))
			{
				val.opcode = OpCodes.Call;
				val.operand = value.Replacement;
				num++;
			}
			list.Add(val);
		}
		if (num != 1)
		{
			throw new InvalidOperationException("Expected one native call in " + __originalMethod.Name + "; found " + num + ".");
		}
		return list;
	}

	internal static void Uninstall(Harmony harmony)
	{
		harmony.UnpatchAll(harmony.Id);
		Bindings.Clear();
		_withBypass = null;
	}

	private static void StartAlliance(IAllianceCampaignBehavior behavior, Kingdom first, Kingdom second)
	{
		_withBypass(delegate
		{
			behavior.StartAlliance(first, second);
		});
	}

	private static void DeclareWar(IFaction first, IFaction second)
	{
		_withBypass(delegate
		{
			DeclareWarAction.ApplyByDefault(first, second);
		});
	}

	private static void MakePeace(IFaction first, IFaction second)
	{
		_withBypass(delegate
		{
			MakePeaceAction.Apply(first, second);
		});
	}
}

