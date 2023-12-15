using Fasterflect;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Extensions
{
    public static class AssemblyUtilities
    {
        public static Type[] Types(this Assembly assembly, params string[] filter)
            => AssemblyExtensions.Types(assembly, Flags.AllMembers, filter).ToArray();

        public static Type[] Types<T>(this Assembly assembly)
            => AssemblyExtensions.TypesImplementing<T>(assembly).ToArray();

        public static Type[] TypesWithAttribute<T>(this Assembly assembly) where T : Attribute
            => AssemblyExtensions.TypesWith<T>(assembly).ToArray();

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