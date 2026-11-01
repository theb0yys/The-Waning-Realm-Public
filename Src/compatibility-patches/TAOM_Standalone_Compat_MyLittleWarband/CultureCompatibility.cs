using System;
using System.Reflection;
using System.IO;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    public sealed class SubModule : MBSubModuleBase
    {
        private const string Owner = "taom.compat.mylittlewarband.culture";
        private Harmony harmony;

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            harmony = new Harmony(Owner);
            try
            {
                string gameBin = Path.GetDirectoryName(typeof(MBSubModuleBase).Assembly.Location);
                string source = Path.GetFullPath(Path.Combine(gameBin, "..", "..", "Modules", "MyLittleWarband", "bin", "Win64_Shipping_Client", "MyLittleWarband.dll"));
                // Use the launcher's normal load-from context so its later MLW
                // instantiation receives the exact assembly patched below.
                Assembly original = Assembly.LoadFrom(source);
                Assembly runtime = BinaryRuntime.Build(original, source);
                BinaryRuntime.BindSaveTypes(harmony, original, runtime);
                CultureCompatibility.Install(harmony, runtime);
                RuntimeLifecycle.Install(harmony, original, runtime);
            }
            catch
            {
                new Harmony(BinaryRuntime.RuntimeHarmonyId).UnpatchAll(BinaryRuntime.RuntimeHarmonyId);
                harmony.UnpatchAll(Owner);
                throw;
            }
        }

        protected override void OnSubModuleUnloaded()
        {
            new Harmony(BinaryRuntime.RuntimeHarmonyId).UnpatchAll(BinaryRuntime.RuntimeHarmonyId);
            harmony?.UnpatchAll(Owner);
            base.OnSubModuleUnloaded();
        }
    }

    public static class CultureCompatibility
    {
        // Resolve metadata once, never during per-troop restoration or UI refresh.
        private static readonly Action<BasicCharacterObject, MBBodyProperty> SetBodyRange =
            (Action<BasicCharacterObject, MBBodyProperty>)Delegate.CreateDelegate(
                typeof(Action<BasicCharacterObject, MBBodyProperty>),
                typeof(BasicCharacterObject).GetProperty("BodyPropertyRange").GetSetMethod(true));

        public static void Install(Harmony harmony, Assembly runtime)
        {
            Type behavior = runtime.GetType("MyLittleWarband.CustomUnitsBehavior", true);
            Type unit = runtime.GetType("MyLittleWarband.CustomUnit", true);
            MethodInfo appearance = Require(behavior, "updateAppearance", typeof(CharacterObject));
            MethodInfo copy = Require(behavior, "CopyCharacter", typeof(CharacterObject), typeof(CharacterObject));
            MethodInfo restore = Require(unit, "Update");
            FieldInfo id = unit.GetField("_id", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!appearance.IsStatic || !copy.IsStatic || restore.IsStatic || id?.FieldType != typeof(string))
                throw new InvalidOperationException("Unsupported My Little Warband culture API; expected v1.2.7.");

            harmony.Patch(appearance, prefix: new HarmonyMethod(typeof(CultureCompatibility), nameof(AppearancePrefix)));
            harmony.Patch(copy, postfix: new HarmonyMethod(typeof(CultureCompatibility), nameof(CopyPostfix)));
            harmony.Patch(restore, postfix: new HarmonyMethod(typeof(CultureCompatibility), nameof(RestorePostfix)));
            PortraitCompatibility.Install(harmony, runtime);
        }

        private static MethodInfo Require(Type type, string name, params Type[] parameters)
        {
            MethodInfo method = type == null ? null : AccessTools.DeclaredMethod(type, name, parameters);
            if (method == null || method.ReturnType != typeof(void))
                throw new InvalidOperationException("Unsupported My Little Warband API: " + name + ". Expected v1.2.7.");
            return method;
        }

        public static bool IsCustomTree(string id)
        {
            if (id == null) return false;
            int start;
            if (id.StartsWith("_basic_root", StringComparison.Ordinal)) start = "_basic_root".Length;
            else if (id.StartsWith("_elite_root", StringComparison.Ordinal)) start = "_elite_root".Length;
            else return false;
            for (int i = start; i < id.Length; i++)
                if (id[i] != '0' && id[i] != '1') return false;
            return true;
        }

        public static CharacterObject FindTemplate(CharacterObject character)
        {
            CultureObject culture = character?.Culture;
            if (culture == null) return null;
            if (character.IsFemale)
            {
                if (Usable(character, culture.Townswoman)) return culture.Townswoman;
                if (Usable(character, culture.FemaleDancer)) return culture.FemaleDancer;
            }
            if (Usable(character, culture.BasicTroop)) return culture.BasicTroop;
            if (Usable(character, culture.Townsman)) return culture.Townsman;
            return null;
        }

        private static bool Usable(CharacterObject target, CharacterObject template) =>
            template != null && !template.IsHero && template.IsFemale == target.IsFemale &&
            template.Culture == target.Culture && template.BodyPropertyRange != null;

        public static bool Synchronize(CharacterObject character)
        {
            if (character == null || character.IsHero || !IsCustomTree(character.StringId)) return false;
            CharacterObject template = FindTemplate(character);
            if (template == null) return false;
            SetBodyRange(character, template.BodyPropertyRange);
            character.Race = template.Race;
            return true;
        }

        // Missing templates preserve the current appearance instead of falling back to Empire
        // or dereferencing an absent FemaleDancer/BasicTroop in the original implementation.
        private static bool AppearancePrefix(CharacterObject __0)
        {
            if (__0 == null || __0.IsHero || !IsCustomTree(__0.StringId)) return true;
            Synchronize(__0);
            return false;
        }

        private static void CopyPostfix(CharacterObject __1) => Synchronize(__1);

        // MLW persists Culture (SaveableProperty 16), but not Race. Reconstruct only
        // after its existing restore completes; no new save fields or session scans.
        private static void RestorePostfix(string ____id)
        {
            if (IsCustomTree(____id))
                Synchronize(MBObjectManager.Instance.GetObject<CharacterObject>(____id));
        }
    }
}
