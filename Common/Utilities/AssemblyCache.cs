using Common.IO.Collections;
using Common.Extensions;
using Common.Logging;

using System;
using System.Reflection;
using System.Collections.Generic;

namespace Common.Utilities
{
    public static class AssemblyCache
    {
        private static readonly LockedDictionary<string, Type> typeNames = new LockedDictionary<string, Type>();
        private static readonly LockedDictionary<ushort, Type> typeHashes = new LockedDictionary<ushort, Type>();

        private static readonly List<Assembly> assemblies = new List<Assembly>();

        public static IReadOnlyList<Assembly> Assemblies
        {
            get => assemblies;
        }

        public static LogOutput Log { get; private set; }

        public static bool IsImmediateLoad = true;
        public static bool IsInitialized;

        public static event Action<Assembly> OnAssemblyLoaded;
        public static event Action<Type, string, ushort> OnTypeCached;

        static AssemblyCache()
        {
            Log = new LogOutput("Assembly Cache").Setup();

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

            if (ConsoleArgs.HasSwitch("AssemblyCacheDisableImmediate"))
                IsImmediateLoad = false;

            if (IsImmediateLoad)
                Log.Warn($"Enabled immediate type caching. This may consume a bit of memory. You can disable this behaviour by using the 'AssemblyCacheDisableImmediate' argument switch.");

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    assemblies.Add(asm);

                    if (IsImmediateLoad)
                        FillTypeCache(asm);

                    Log.Verbose($"Cached assembly: {asm.FullName}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while loading assemblies:\n{ex}");
            }

            if (typeHashes.Count != typeNames.Count)
                Log.Warn($"Type hash count mismatch! This should not be a problem (unless it is).");

            IsInitialized = true;

            Log.Verbose($"Cached {assemblies.Count} assemblies ({typeHashes.Count} / {typeNames.Count})");
        }

        public static bool TryRetrieveType(ushort typeHash, out Type type)
        {
            if (typeHashes.TryGetValue(typeHash, out type))
                return true;

            foreach (var assembly in Assemblies)
            {
                try
                {
                    foreach (var asmType in assembly.GetTypes())
                    {
                        var nameHash = asmType.FullName.GetStableHash();

                        if (nameHash == typeHash)
                        {
                            typeNames[asmType.FullName] = asmType;
                            typeHashes[nameHash] = asmType;

                            OnTypeCached.Call(asmType, asmType.FullName, nameHash, null, Log.Error);

                            type = asmType;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occured while searching for hash '{typeHash}' in assembly '{assembly.FullName}':\n{ex}");
                }
            }

            type = null;
            return false;
        }

        public static bool TryRetrieveType(string typeName, out Type type)
        {
            if (typeNames.TryGetValue(typeName, out type))
                return true;

            foreach (var assembly in Assemblies)
            {
                try
                {
                    foreach (var asmType in assembly.GetTypes())
                    {
                        if (asmType.FullName == typeName)
                        {
                            var nameHash = asmType.FullName.GetStableHash();

                            typeNames[asmType.FullName] = asmType;
                            typeHashes[nameHash] = asmType;

                            OnTypeCached.Call(asmType, asmType.FullName, nameHash, null, Log.Error);

                            type = asmType;
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occured while searching for type name '{typeName}' in assembly '{assembly.FullName}':\n{ex}");
                }
            }

            type = null;
            return false;
        }

        private static void FillTypeCache(Assembly assembly)
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    var nameHash = type.FullName.GetStableHash();

                    typeNames[type.FullName] = type;
                    typeHashes[nameHash] = type;

                    OnTypeCached.Call(type, type.FullName, nameHash, null, Log.Error);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while filling type cache in assembly '{assembly.FullName}':\n{ex}");
            }
        }

        private static void OnAssemblyLoad(object _, AssemblyLoadEventArgs ev)
        {
            try
            {
                if (ev.LoadedAssembly != null)
                {
                    assemblies.Add(ev.LoadedAssembly);

                    if (IsImmediateLoad)
                        FillTypeCache(ev.LoadedAssembly);

                    Log.Verbose($"Cached assembly: {ev.LoadedAssembly.FullName}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while filling type cache in assembly '{ev.LoadedAssembly?.FullName ?? "null assembly"}':\n{ex}");
            }
        }
    }
}
