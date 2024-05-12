using Common.IO.Collections;
using Common.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class AssemblyExtensions
    {
        private static readonly LockedDictionary<Assembly, List<Type>> _types = new LockedDictionary<Assembly, List<Type>>();
        private static readonly LockedDictionary<Assembly, List<MethodInfo>> _methods = new LockedDictionary<Assembly, List<MethodInfo>>();

        public static Func<Assembly, byte[]> RawBytesMethod { get; }
        public static Type RuntimeAssemblyType { get; }

        static AssemblyExtensions()
        {
            RuntimeAssemblyType = Type.GetType("System.Reflection.RuntimeAssembly");

            if (RuntimeAssemblyType is null)
            {
                LogOutput.Common.Warn($"Type 'System.Reflection.RuntimeAssembly' is not present in this runtime!");
                return;
            }

            var runtimeAssemblyMethod = RuntimeAssemblyType.GetAllMethods().FirstOrDefault(m => m.Name == "GetRawBytes" && m.ReturnType == typeof(byte[]) && m.Parameters().Length == 0);

            if (runtimeAssemblyMethod is null)
            {
                LogOutput.Common.Warn($"RuntimeAssembly.GetRawBytes method does not exist in this runtime!");
                return;
            }

            RawBytesMethod = assembly => (byte[])runtimeAssemblyMethod.Invoke(assembly, null);
        }

        public static Type[] Types<T>(this Assembly assembly)
        {
            if (!_types.TryGetValue(assembly, out var types))
                types = _types[assembly] = assembly.GetTypes().ToList();

            return types.WhereArray(t => t.InheritsType<T>());
        }

        public static Type[] TypesWithAttribute<T>(this Assembly assembly) where T : Attribute
        {
            if (!_types.TryGetValue(assembly, out var types))
                types = _types[assembly] = assembly.GetTypes().ToList();

            return types.WhereArray(t => t.HasAttribute<T>());
        }

        public static byte[] GetRawBytes(this Assembly assembly)
        {
            if (RawBytesMethod is null)
                throw new InvalidOperationException($"GetRawBytes method is not supported in this runtime!");

            return RawBytesMethod(assembly);
        }

        public static MethodInfo[] Methods(this Assembly assembly)
        {
            if (_methods.TryGetValue(assembly, out var methods))
                return methods.ToArray();

            methods = new List<MethodInfo>();

            foreach (var type in assembly.GetTypes())
                methods.AddRange(type.GetAllMethods());

            return (_methods[assembly] = methods).ToArray();
        }

        public static MethodInfo[] MethodsWithAttribute<T>(this Assembly assembly) where T : Attribute
        {
            if (_methods.TryGetValue(assembly, out var methods))
                return methods.WhereArray(m => m.HasAttribute<T>());

            methods = new List<MethodInfo>();

            foreach (var type in assembly.GetTypes())
                methods.AddRange(type.GetAllMethods());

            return (_methods[assembly] = methods).WhereArray(m => m.HasAttribute<T>());
        }
    }
}