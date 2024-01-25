using Common.Extensions;
using Common.Logging;
using Common.Utilities;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public static class DataWriterLoader
    {
        public static LogOutput Log { get; private set; }

        public static event Action<Type, MethodInfo, FastInvokeHandler> OnWriterCached;

        internal static void Initialize()
        {
            Log = new LogOutput("Writer Loader").Setup();

            AssemblyCache.OnTypeCached += OnTypeCached;

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
                foreach (var method in typeof(DataWriter).GetAllMethods())
                {
                    if (method.IsPublic && method.Name.StartsWith("Write") && method.Parameters().Length == 1 && !method.ContainsGenericParameters)
                    {
                        var type = method.Parameters()[0].ParameterType;

                        DataWriterUtils.Writers[type] = DataWriterUtils.WriterMethodToInvoke(method);

                        Log.Verbose($"Cached default writer for '{type.FullName}': {method.ToName()}");
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

                    if (methodParams.Length != 2 || methodParams[0].ParameterType != typeof(DataWriter))
                        continue;

                    var writerType = methodParams[1].ParameterType;

                    if (method.HasAttribute<DataWriterAttribute>(out var dataWriterAttribute) && dataWriterAttribute.ReplacedType != null)
                        writerType = dataWriterAttribute.ReplacedType;

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

                    DataWriterUtils.Writers[writerType] = (writer, value) => invokeHandler(null, writer, value);

                    Log.Verbose($"Cached data writer: ({writerType.FullName}) {method.ToName()}");

                    OnWriterCached.Call(writerType, method, invokeHandler);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while searching for data writers in type '{type.FullName}':\n{ex}");
            }
        }
    }
}
