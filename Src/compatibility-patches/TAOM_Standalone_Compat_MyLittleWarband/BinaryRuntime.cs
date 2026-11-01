using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    public static class BinaryRuntime
    {
        public const string RuntimeName = "TAOM_Standalone_Compat_MyLittleWarband.Runtime";
        public const string RuntimeHarmonyId = "taom.compat.mylittlewarband.runtime";
        private const string SettingsType = "MyLittleWarband.MyLittleWarbandSetting";
        private static readonly Guid SupportedMvid = new Guid("ecf22d06-2f8c-411c-80ab-6704956dd347");
        private const string SupportedHash = "B241034E4EC4B5E9389078B8CEF50B560638A85BDDC40EDBBC6BF83D49931AF1";
        public static int RewrittenMethods { get; private set; }
        public static int RewrittenCalls { get; private set; }
        private static Type savedUnit;

        public static Assembly Build(Assembly original, string sourcePath)
        {
            if (new FileInfo(sourcePath).Length != 98816) throw new NotSupportedException("Unexpected MLW binary size; only the inspected v1.2.7 is supported.");
            byte[] source = File.ReadAllBytes(sourcePath);
            using (SHA256 sha = SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(source)).Replace("-", "") != SupportedHash ||
                    original.GetName().Name != "MyLittleWarband" || original.ManifestModule.ModuleVersionId != SupportedMvid)
                    throw new NotSupportedException("This TAOM adapter requires the inspected My Little Warband v1.2.7 DLL.");

            LegacyApi.BindSettings(original.GetType(SettingsType, true));
            Type cecil = CecilMetadata.Type("ModuleDefinition");
            object module = cecil.GetMethod("ReadModule", new[] { typeof(Stream) }).Invoke(null, new object[] { new MemoryStream(source, false) });
            using ((IDisposable)module)
            {
                object resolver = CecilMetadata.Get(module, "AssemblyResolver");
                CecilMetadata.Call(resolver, "AddSearchDirectory", Path.GetDirectoryName(typeof(MBCharacterSkills).Assembly.Location));
                CecilMetadata.Call(resolver, "AddSearchDirectory", Path.GetDirectoryName(typeof(BinaryRuntime).Assembly.Location));
                CecilMetadata.Call(resolver, "AddSearchDirectory", Path.GetDirectoryName(sourcePath));
                if ((Guid)CecilMetadata.Get(module, "Mvid") != SupportedMvid) throw new InvalidOperationException("MLW module identity mismatch.");
                CecilMetadata.Set(CecilMetadata.Get(CecilMetadata.Get(module, "Assembly"), "Name"), "Name", RuntimeName);
                CecilMetadata.Set(module, "Name", RuntimeName + ".dll");
                CecilMetadata.Set(module, "Mvid", Guid.NewGuid());

                var references = new Dictionary<int, object>();
                foreach (var entry in CallMap()) references.Add(entry.Key, CecilMetadata.Call(module, "ImportReference", entry.Value));
                object objectType = CecilMetadata.Call(module, "ImportReference", typeof(object));
                var types = new List<object>();
                foreach (object type in (IEnumerable)CecilMetadata.Call(module, "GetTypes")) types.Add(type);
                if (types.Count > 128) throw new InvalidOperationException("MLW type bound exceeded.");
                // Resolve deferred method-token references before removing owner types.
                // Cecil's reader otherwise consults the mutated type table for later bodies.
                foreach (object type in types)
                    foreach (object method in CecilMetadata.List(type, "Methods"))
                        if ((bool)CecilMetadata.Get(method, "HasBody"))
                            foreach (object instruction in CecilMetadata.List(CecilMetadata.Get(method, "Body"), "Instructions"))
                                CecilMetadata.Get(instruction, "Operand");
                IList rootTypes = CecilMetadata.List(module, "Types");
                // Keep exactly one original MCM settings owner and one save definer.
                foreach (object type in types)
                {
                    string name = (string)CecilMetadata.Get(type, "FullName");
                    if (name == SettingsType || name == "MyLittleWarband.SaveDefiner" || name == "MyLittleWarband.PartyUnitUpgradeLengthPatch")
                        rootTypes.Remove(type);
                }
                foreach (object reference in (IEnumerable)CecilMetadata.Call(module, "GetTypeReferences")) RetargetType(module, reference);

                RewrittenMethods = 0;
                RewrittenCalls = 0;
                int methodCount = 0, instructionCount = 0;
                object call = typeof(Harmony).Assembly.GetType("Mono.Cecil.Cil.OpCodes", true).GetField("Call").GetValue(null);
                Type methodReference = CecilMetadata.Type("MethodReference");
                foreach (object type in types)
                {
                    if (!rootTypes.Contains(type) && CecilMetadata.Get(type, "DeclaringType") == null) continue;
                    foreach (object field in CecilMetadata.List(type, "Fields"))
                        CecilMetadata.Set(field, "FieldType", RewriteSharedType(CecilMetadata.Get(field, "FieldType"), objectType));
                    foreach (object method in CecilMetadata.List(type, "Methods"))
                    {
                        if (++methodCount > 512) throw new InvalidOperationException("MLW method bound exceeded.");
                        if (!(bool)CecilMetadata.Get(method, "HasBody")) continue;
                        object body = CecilMetadata.Get(method, "Body");
                        foreach (object variable in CecilMetadata.List(body, "Variables"))
                            CecilMetadata.Set(variable, "VariableType", RewriteSharedType(CecilMetadata.Get(variable, "VariableType"), objectType));
                        bool changed = false;
                        foreach (object instruction in CecilMetadata.List(body, "Instructions"))
                        {
                            if (++instructionCount > 30000) throw new InvalidOperationException("MLW instruction bound exceeded.");
                            object operand = CecilMetadata.Get(instruction, "Operand");
                            if (operand is string text && text == "MyLittleWarband")
                            {
                                // Only SubModule's private Harmony owner string has this value.
                                if ((string)CecilMetadata.Get(type, "FullName") == "MyLittleWarband.SubModule") CecilMetadata.Set(instruction, "Operand", RuntimeHarmonyId);
                            }
                            if (!methodReference.IsInstanceOfType(operand)) continue;
                            object replacement;
                            bool mapped = references.TryGetValue(CecilMetadata.Token(operand), out replacement);
                            if (!mapped)
                            {
                                string declaring = (string)CecilMetadata.Get(CecilMetadata.Get(operand, "DeclaringType"), "FullName");
                                string name = (string)CecilMetadata.Get(operand, "Name");
                                string factory = null;
                                if (declaring == SettingsType && name.StartsWith("get_", StringComparison.Ordinal)) factory = name.Substring(4);
                                else if (declaring.Contains("GlobalSettings`1<" + SettingsType + ">") && name == "get_Instance") factory = nameof(LegacyApi.SettingsInstance);
                                if (factory != null)
                                {
                                    MethodInfo wrapper = typeof(LegacyApi).GetMethod(factory, BindingFlags.Public | BindingFlags.Static);
                                    if (wrapper == null) throw new MissingMethodException("Unsupported MLW settings member: " + name);
                                    replacement = CecilMetadata.Call(module, "ImportReference", wrapper);
                                    mapped = true;
                                }
                            }
                            if (!mapped) continue;
                            CecilMetadata.Set(instruction, "Operand", replacement);
                            // All old constructors become static factories; native instance
                            // getters/load methods retain callvirt and the original stack shape.
                            if (!(bool)CecilMetadata.Get(replacement, "HasThis")) CecilMetadata.Set(instruction, "OpCode", call);
                            RewrittenCalls++;
                            changed = true;
                        }
                        foreach (object instruction in CecilMetadata.List(body, "Instructions"))
                            if (CecilMetadata.Get(CecilMetadata.Get(instruction, "OpCode"), "FlowControl").ToString() == "Call" &&
                                CecilMetadata.Get(instruction, "Operand") == null)
                                throw new InvalidOperationException("Missing call operand in " + CecilMetadata.Get(method, "FullName") + " at " + CecilMetadata.Get(instruction, "Offset"));
                        if (changed) RewrittenMethods++;
                    }
                }
                int categoryMethods = EquipmentCategoryCompatibility.Rewrite(module);
                RewrittenMethods += categoryMethods;
                RewrittenCalls += categoryMethods;
                EquipmentEnumCompatibility.Rewrite(module);
                RewrittenMethods += 3;
                RewrittenCalls++; // Equipment-card bridge; the other two methods retain their control flow.
                using (var output = new MemoryStream())
                {
                    CecilMetadata.Call(module, "Write", output);
                    return Assembly.Load(output.ToArray());
                }
            }
        }

        private static object RewriteSharedType(object type, object objectType)
        {
            if ((string)CecilMetadata.Get(type, "FullName") == SettingsType) return objectType;
            PropertyInfo arguments = type.GetType().GetProperty("GenericArguments");
            if (arguments != null)
            {
                IList list = (IList)arguments.GetValue(type, null);
                for (int i = 0; i < list.Count; i++) list[i] = RewriteSharedType(list[i], objectType);
            }
            return type;
        }

        private static void RetargetType(object module, object reference)
        {
            Type replacement = null;
            switch ((string)CecilMetadata.Get(reference, "FullName"))
            {
                case "TaleWorlds.Core.ImageIdentifierVM": replacement = typeof(ImageIdentifierVM); break;
                case "TaleWorlds.Core.ImageIdentifier": replacement = typeof(ImageIdentifier); break;
                case "TaleWorlds.Core.ImageIdentifierType": replacement = typeof(int); break;
                case "TaleWorlds.GauntletUI.Data.GauntletMovie":
                case "TaleWorlds.GauntletUI.Data.IGauntletMovie": replacement = typeof(GauntletMovieIdentifier); break;
            }
            if (replacement == null) return;
            object imported = CecilMetadata.Call(module, "ImportReference", replacement);
            foreach (string property in new[] { "Name", "Namespace", "Scope", "IsValueType" })
                CecilMetadata.Set(reference, property, CecilMetadata.Get(imported, property));
        }

        private static Dictionary<int, MethodBase> CallMap()
        {
            var map = new Dictionary<int, MethodBase>();
            Add(map, 0x0a00001e, nameof(LegacyApi.ItemVM));
            Add(map, 0x0a000062, nameof(LegacyApi.ColoredTooltip));
            Add(map, 0x0a000064, nameof(LegacyApi.Tooltip));
            Add(map, 0x0a000082, nameof(LegacyApi.Layer));
            map.Add(0x0a000084, typeof(GauntletLayer).GetMethod("LoadMovie", new[] { typeof(string), typeof(ViewModel) }));
            map.Add(0x0a00008c, typeof(GauntletLayer).GetMethod("ReleaseMovie", new[] { typeof(GauntletMovieIdentifier) }));
            Add(map, 0x0a0000df, nameof(LegacyApi.AllEquipments));
            Add(map, 0x0a0000f0, nameof(LegacyApi.FillCharacter));
            Add(map, 0x0a00010e, nameof(LegacyApi.CharacterVM));
            Add(map, 0x0a00010f, nameof(LegacyApi.EmptyVM));
            Add(map, 0x0a000132, nameof(LegacyApi.Item));
            Add(map, 0x0a000133, nameof(LegacyApi.Empty));
            Add(map, 0x0a000134, nameof(LegacyApi.Inquiry));
            Add(map, 0x0a00013b, nameof(LegacyApi.HiddenInEncyclopedia));
            Add(map, 0x0a00013d, nameof(LegacyApi.Character));
            Add(map, 0x0a00013e, nameof(LegacyApi.EnabledInquiry));
            Add(map, 0x0a000156, nameof(LegacyApi.Banner));
            map.Add(0x0a000164, typeof(MBCharacterSkills).GetProperty("Skills").GetGetMethod());
            Add(map, 0x0a000168, nameof(LegacyApi.SetCulture));
            return map;
        }

        private static void Add(Dictionary<int, MethodBase> map, int token, string name) => map.Add(token, typeof(LegacyApi).GetMethod(name));

        public static void BindSaveTypes(Harmony harmony, Assembly original, Assembly runtime)
        {
            savedUnit = runtime.GetType("MyLittleWarband.CustomUnit", true);
            Type definer = original.GetType("MyLittleWarband.SaveDefiner", true);
            var transplant = new HarmonyMethod(typeof(BinaryRuntime), nameof(SaveTypeOperands));
            harmony.Patch(AccessTools.DeclaredMethod(definer, "DefineClassTypes"), transpiler: transplant);
            harmony.Patch(AccessTools.DeclaredMethod(definer, "DefineContainerDefinitions"), transpiler: transplant);
        }

        private static IEnumerable<CodeInstruction> SaveTypeOperands(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldtoken && instruction.operand is Type type)
                {
                    if (type.FullName == "MyLittleWarband.CustomUnit") instruction.operand = savedUnit;
                    else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                        type.GetGenericArguments()[1].FullName == "MyLittleWarband.CustomUnit")
                        instruction.operand = typeof(Dictionary<,>).MakeGenericType(typeof(string), savedUnit);
                }
                yield return instruction;
            }
        }
    }
}
