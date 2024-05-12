using Common.Extensions;

using HarmonyLib;

using System;

namespace Common.IO.Data
{
    public static class DataReaderLoader
    {
        internal static void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            var types = ModuleInitializer.SafeQueryTypes();

            foreach (var type in types)
                SearchType(type);
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

                    DataReaderUtils.Readers[readerType] = reader => method.Invoke(reader, null);
                }
            }
            catch { }
        }

        private static void OnAssemblyLoaded(object _, AssemblyLoadEventArgs ev)
        {
            foreach (var type in ev.LoadedAssembly.GetTypes())
                SearchType(type);
        }
    }
}