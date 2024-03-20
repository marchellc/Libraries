using Common.Extensions;
using Common.Logging;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Common.IO.Data
{
    public static class DataReaderLoader
    {
        public static LogOutput Log { get; private set; }

        internal static void Initialize()
        {
            Log = new LogOutput("Reader Loader");
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
                foreach (var method in typeof(DataReader).GetAllMethods())
                {
                    if (method.HasAttribute<DataLoaderIgnoreAttribute>())
                        continue;

                    if (method.IsPublic && method.Name.StartsWith("Read") && method.Parameters().Length == 0 && !method.ContainsGenericParameters)
                    {
                        var type = method.ReturnType;

                        DataReaderUtils.Readers[type] = DataReaderUtils.ReaderMethodToDelegate(method);

                        Log.Info($"Cached default reader for '{type.FullName}': {method.ToName()}");
                    }
                }
            }
            catch
            {

            }
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

                    if (method.HasAttribute<DataLoaderIgnoreAttribute>())
                        continue;

                    var methodParams = method.Parameters();

                    if (methodParams.Length != 1 || methodParams[0].ParameterType != typeof(DataReader))
                        continue;

                    var readerType = method.ReturnType;

                    if (method.HasAttribute<DataReaderAttribute>(out var dataReaderAttribute) && dataReaderAttribute.ReplacedType != null)
                        readerType = dataReaderAttribute.ReplacedType;

                    if (!DelegateExtensions.DisableFastInvoker)
                    {
                        try
                        {
                            var invokeHandler = MethodInvoker.GetHandler(method);
                            DataReaderUtils.Readers[readerType] = reader => invokeHandler(null, reader);
                        }
                        catch
                        {
                            Log.Warn($"Failed to create fast invoke handler for method '{method.ToName()}', falling back to reflection");
                            DataReaderUtils.Readers[readerType] = reader => method.Invoke(reader, null);
                        }
                    }
                    else
                    {
                        DataReaderUtils.Readers[readerType] = reader => method.Invoke(reader, null);
                    }

                    Log.Info($"Cached data reader: ({readerType.FullName}) {method.ToName()}");
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