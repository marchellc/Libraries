using Common.Extensions;
using Common.Logging;
using Common.IO.Collections;

using Networking.Data;

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Utilities
{
    public static class TypeLoader
    {
        public static readonly LockedDictionary<Type, Action<Writer, object>> writers;
        public static readonly LockedDictionary<Type, Func<Reader, object>> readers;

        public static readonly LogOutput log;

        static TypeLoader()
        {
            log = new LogOutput("Type Loader");
            log.Setup();

            writers = new LockedDictionary<Type, Action<Writer, object>>();
            readers = new LockedDictionary<Type, Func<Reader, object>>();

            var assemblies = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
            var currAssembly = Assembly.GetExecutingAssembly();

            var readerType = typeof(Reader);
            var writerType = typeof(Writer);

            if (!assemblies.Contains(currAssembly))
                assemblies.Add(currAssembly);

            var joinedMethods = readerType.GetAllMethods().Union(writerType.GetAllMethods());

            foreach (var method in joinedMethods)
            {
                if (method.IsStatic)
                    continue;

                if (method.Name.StartsWith("Read") && method.ReturnType != typeof(void) && method.Parameters().Length == 0 && !method.ContainsGenericParameters)
                {
                    var readType = method.ReturnType;

                    if (readers.ContainsKey(readType))
                        continue;

                    readers[readType] = reader => method.Call(reader);
                }
                else if (method.Name.StartsWith("Write") && method.ReturnType == typeof(void) && !method.ContainsGenericParameters)
                {
                    var methodParams = method.Parameters();

                    if (methodParams.Length != 1)
                        continue;

                    var writeType = methodParams[0].ParameterType;

                    if (writers.ContainsKey(writeType))
                        continue;

                    writers[writeType] = (writer, value) => method.Call(writer, value);
                }
            }

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetAllMethods())
                    {
                        if (!method.IsStatic)
                            continue;

                        var methodParams = method.Parameters();

                        if (method.ReturnType != typeof(void) && methodParams.Length == 2 && methodParams[0].ParameterType == readerType)
                        {
                            if (readers.ContainsKey(method.ReturnType))
                                continue;

                            readers[method.ReturnType] = reader => method.Call(null, reader);

                            log.Debug($"Cached custom reader: {method.ReturnType.FullName} ({method.ToName()})");
                        }
                        else if (method.ReturnType == typeof(void) && methodParams.Length == 1 && methodParams[0].ParameterType == typeof(Writer))
                        {
                            if (writers.ContainsKey(methodParams[0].ParameterType))
                                continue;

                            writers[methodParams[0].ParameterType] = (writer, value) => method.Call(null, writer, value);

                            log.Debug($"Cached custom writer: {methodParams[0].ParameterType.FullName} ({method.ToName()})");
                        }
                    }
                }
            }

            log.Info($"Cache: {writers.Count}/{readers.Count}");
        }

        public static Action<Writer, object> GetWriter(this Type type)
        {
            if (writers.TryGetValue(type, out var writer))
                return writer;

            throw new Exception($"No writers for type {type.FullName}");
        }

        public static Func<Reader, object> GetReader(this Type type)
        {
            if (readers.TryGetValue(type, out var reader))
                return reader;

            throw new Exception($"No readers for type {type.FullName}");
        }
    }
}