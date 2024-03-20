using Common.Extensions;
using Common.Logging;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public static class DataWriterLoader
    {
        public static LogOutput Log { get; private set; }

        internal static void Initialize()
        {
            Log = new LogOutput("Writer Loader");
            Log.Setup();

            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            var curAssemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());

            try
            {
                foreach (var assembly in curAssemblies)
                {
                    try
                    {
                        foreach (var type in assembly.GetTypes())
                            SearchType(type);
                    }
                    catch 
                    {

                    }
                }
            }
            catch
            {

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
            catch 
            {

            }

            Log.Info("Initialized.");
        }

        private static void SearchType(Type type)
        {
            try
            {
                if (type.FullName.StartsWith("System.") || type.FullName.StartsWith("Microsoft."))
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

                    if (!DelegateExtensions.DisableFastInvoker)
                    {
                        try
                        {
                            var invokeHandler = MethodInvoker.GetHandler(method);
                            DataWriterUtils.Writers[writerType] = (writer, value) => invokeHandler(null, writer, value);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to create fast invoke handler for method '{method.ToName()}', falling back to reflection:\n{ex}");
                            DataWriterUtils.Writers[writerType] = (writer, value) => method.Invoke(writer, new object[] { value });
                        }
                    }
                    else
                    {
                        DataWriterUtils.Writers[writerType] = (writer, value) => method.Invoke(writer, new object[] { value });
                    }

                    Log.Verbose($"Cached data writer: ({writerType.FullName}) {method.ToName()}");
                }
            }
            catch 
            {

            }
        }

        private static void OnAssemblyLoaded(object _, AssemblyLoadEventArgs ev)
        {
            try
            {
                if (ev.LoadedAssembly != null)
                {
                    foreach (var type in ev.LoadedAssembly.GetTypes())
                        SearchType(type);
                }
            }
            catch { }
        }
    }
}
