using Common.Extensions;

using System;

namespace Common.IO.Data
{
    public static class DataWriterLoader
    {
        internal static void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoaded;

            foreach (var type in ModuleInitializer.SafeQueryTypes())
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

                    if (methodParams.Length != 2 || methodParams[0].ParameterType != typeof(DataWriter))
                        continue;

                    var writerType = methodParams[1].ParameterType;

                    if (method.HasAttribute<DataWriterAttribute>(out var dataWriterAttribute) && dataWriterAttribute.ReplacedType != null)
                        writerType = dataWriterAttribute.ReplacedType;

                    DataWriterUtils.Writers[writerType] = (writer, value) => method.Invoke(writer, new object[] { value });
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
