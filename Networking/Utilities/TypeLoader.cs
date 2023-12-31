﻿using Common.Extensions;
using Common.Logging;
using Common.IO.Collections;

using Networking.Data;

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Common.Values;

namespace Networking.Utilities
{
    public static class TypeLoader
    {
        public static LockedDictionary<Type, Action<Writer, object>> writers;
        public static LockedDictionary<Type, Func<Reader, object>> readers;

        public static LogOutput log;

        internal static void Init()
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

            foreach (var writer in writers)
            {
                if (!readers.ContainsKey(writer.Key))
                {
                    log.Warn($"Missing reader for type '{writer.Key.FullName}'!");
                    continue;
                }
            }

            foreach (var reader in readers)
            {
                if (!writers.ContainsKey(reader.Key))
                {
                    log.Warn($"Missing reader for type '{reader.Key.FullName}'!");
                    continue;
                }
            }

            log.Info($"Cache: {writers.Count}/{readers.Count}");
        }

        public static Action<Writer, object> GetWriter(this Type type)
        {
            if (writers.TryGetValue(type, out var writer))
                return writer;

            if (type.IsEnum)
                return (writer, obj) =>
                {
                    var enumObj = obj as Enum;
                    var enumCode = enumObj.GetTypeCode();
                    var enumNumType = enumCode.ToType();
                    var enumWriter = writers[enumNumType];

                    writer.WriteByte((byte)enumCode);
                    writer.WriteType(obj.GetType());

                    enumWriter.Call(writer, Convert.ChangeType(obj, enumNumType));
                };

            throw new Exception($"No writer for type '{type.FullName}' was found.");
        }

        public static Func<Reader, object> GetReader(this Type type)
        {
            if (readers.TryGetValue(type, out var reader))
                return reader;

            if (type.IsEnum)
                return reader =>
                {
                    var enumTypeCode = (TypeCode)reader.ReadByte();
                    var enumType = reader.ReadType();
                    var enumNumType = enumTypeCode.ToType();
                    var enunNum = readers[enumNumType].Call(reader);

                    return Convert.ChangeType(enunNum, enumType);
                };

            throw new Exception($"No reader for type '{type.FullName}' was found.");
        }
    }
}