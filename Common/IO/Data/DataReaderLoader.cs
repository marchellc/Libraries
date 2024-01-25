using Common.Extensions;
using Common.Logging;
using Common.Utilities;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public static class DataReaderLoader
    {
        public static LogOutput Log { get; private set; }

        public static event Action<Type, MethodInfo, FastInvokeHandler> OnReaderCached;

        internal static void Initialize()
        {
            Log = new LogOutput("Reader Loader").Setup();

            AssemblyCache.OnTypeCached += OnTypeCached;

            Log.Verbose("Loading data readers ..");

            var curAssemblies = new List<Assembly>(AssemblyCache.Assemblies);

            try
            {
                foreach (var assembly in curAssemblies)
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                            OnTypeCached(type, type.FullName, type.FullName.GetStableHash());
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            try
            {
                foreach (var method in typeof(DataReader).GetAllMethods())
                {
                    if (method.IsPublic && method.Name.StartsWith("Read") && method.Parameters().Length == 0 && !method.ContainsGenericParameters)
                    {
                        var type = method.ReturnType;

                        DataReaderUtils.Readers[type] = DataReaderUtils.ReaderMethodToDelegate(method);

                        Log.Verbose($"Cached default reader for '{type.FullName}': {method.ToName()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            Log.Info("Initialized.");
        }

        private static void OnTypeCached(Type type, string name, ushort hash)
        {
            try
            {
                if (name.StartsWith("System.") || name.StartsWith("Microsoft."))
                    return;

                foreach (var method in type.GetAllMethods())
                {
                    if (!method.IsStatic)
                        continue;

                    var methodParams = method.Parameters();

                    if (methodParams.Length != 1 || methodParams[0].ParameterType != typeof(DataReader))
                        continue;

                    var readerType = method.ReturnType;

                    if (method.HasAttribute<DataReaderAttribute>(out var dataReaderAttribute) && dataReaderAttribute.ReplacedType != null)
                        readerType = dataReaderAttribute.ReplacedType;

                    FastInvokeHandler invokeHandler = null;

                    try
                    {
                        invokeHandler = MethodInvoker.GetHandler(method);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to create fast invoke handler for method '{method.ToName()}':\n{ex}");
                        continue;
                    }

                    DataReaderUtils.Readers[readerType] = reader => invokeHandler(null, reader);

                    Log.Verbose($"Cached data reader: ({readerType.FullName}) {method.ToName()}");

                    OnReaderCached.Call(readerType, method, invokeHandler);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while reading type '{type.FullName}':\n{ex}");
            }
        }
    }
}