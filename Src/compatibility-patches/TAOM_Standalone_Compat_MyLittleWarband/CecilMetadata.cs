using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace TAOM_Standalone_Compat_MyLittleWarband
{
    // Harmony already embeds Cecil privately. This startup-only adapter uses that
    // exact installed copy, without shipping another Cecil or editing any assembly.
    internal static class CecilMetadata
    {
        public static Type Type(string name) => typeof(Harmony).Assembly.GetType("Mono.Cecil." + name, true);
        private static PropertyInfo Property(object obj, string name)
        {
            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (property != null) return property;
            }
            throw new MissingMemberException(obj.GetType().FullName, name);
        }
        public static object Get(object obj, string name) => Property(obj, name).GetValue(obj, null);
        public static void Set(object obj, string name, object value) => Property(obj, name).SetValue(obj, value, null);
        public static IList List(object obj, string name) => (IList)Get(obj, name);
        public static object Call(object obj, string name, params object[] args)
        {
            foreach (MethodInfo method in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name != name) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool matches = true;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) matches = false;
                if (matches) return method.Invoke(obj, args);
            }
            throw new MissingMethodException(obj.GetType().FullName, name);
        }
        public static int Token(object obj) => (int)Call(Get(obj, "MetadataToken"), "ToInt32");
    }
}
