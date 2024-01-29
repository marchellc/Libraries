using Common.Logging;

using Fasterflect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class AssemblyExtensions
    {
        public static Func<Assembly, byte[]> RawBytesMethod { get; }
        public static Type RuntimeAssemblyType { get; }

        static AssemblyExtensions()
        {
            RuntimeAssemblyType = Type.GetType("System.Reflection.RuntimeAssembly");

            var runtimeAssemblyMethod = RuntimeAssemblyType.GetAllMethods().FirstOrDefault(m => m.Name == "GetRawBytes" && m.ReturnType == typeof(byte[]) && m.Parameters().Length == 0);

            if (runtimeAssemblyMethod is null)
            {
                LogOutput.Common.Warn($"RuntimeAssembly.GetRawBytes does not exist in this runtime!");
                return;
            }

            var runtimeMethodInvoker = HarmonyLib.MethodInvoker.GetHandler(runtimeAssemblyMethod);

            RawBytesMethod = assembly => (byte[])runtimeMethodInvoker(assembly);
        }

        public static Type[] Types(this Assembly assembly, params string[] filter)
            => Fasterflect.AssemblyExtensions.Types(assembly, Flags.AllMembers, filter).ToArray();

        public static Type[] Types<T>(this Assembly assembly)
            => Fasterflect.AssemblyExtensions.TypesImplementing<T>(assembly).ToArray();

        public static Type[] TypesWithAttribute<T>(this Assembly assembly) where T : Attribute
            => Fasterflect.AssemblyExtensions.TypesWith<T>(assembly).ToArray();

        public static byte[] GetRawBytes(this Assembly assembly)
        {
            if (RawBytesMethod is null)
                return Array.Empty<byte>();

            return RawBytesMethod(assembly);
        }

        public static MethodInfo[] Methods(this Assembly assembly)
        {
            var list = new List<MethodInfo>();

            foreach (var type in assembly.Types())
                list.AddRange(type.Methods());

            return list.ToArray();
        }

        public static MethodInfo[] MethodsWithAttribute<T>(this Assembly assembly) where T : Attribute
        {
            var list = new List<MethodInfo>();

            foreach (var type in assembly.Types())
                list.AddRange(type.MethodsWithAttribute<T>());

            return list.ToArray();
        }
    }
}