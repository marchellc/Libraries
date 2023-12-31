using Common.Extensions;
using Common.Logging;

using Networking.Data;

using System;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Networking.Utilities
{
    public static class TypeLoader
    {
        private static readonly MethodInfo[] writerMethods;
        private static readonly MethodInfo[] readerMethods;

        private static readonly Dictionary<Type, Action<Writer, object>> writerDelegates = new Dictionary<Type, Action<Writer, object>>();
        private static readonly Dictionary<Type, Func<Reader, object>> readerDelegates = new Dictionary<Type, Func<Reader, object>>();
        private static readonly Dictionary<Type, Func<object>> instanceDelegates = new Dictionary<Type, Func<object>>();

        private static readonly List<Type> deserializableTypes = new List<Type>();
        private static readonly List<Type> unknownTypes = new List<Type>();

        public static readonly List<Type> additionalTypes = new List<Type>();

        public static readonly LogOutput log;

        static TypeLoader()
        {
            log = new LogOutput("Type Loader");
            log.Setup();

            try
            {
                writerMethods = typeof(Writer).GetAllMethods().Where(m => !m.IsStatic && m.Name.StartsWith("Write") && m.Parameters().Length == 1).ToArray();
                readerMethods = typeof(Reader).GetAllMethods().Where(m => !m.IsStatic && m.Name.StartsWith("Read") && m.Parameters().Length == 1).ToArray();
            }
            catch (Exception ex)
            {
                log.Error($"Failed while loading methods:\n{ex}");
            }

            log.Debug($"Loaded {writerMethods.Length} writer methods");
            log.Debug($"Loaded {readerMethods.Length} reader methods");

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        try
                        {
                            if (typeof(IDeserialize).IsAssignableFrom(type)
                                && typeof(ISerialize).IsAssignableFrom(type))
                                deserializableTypes.Add(type);
                            else
                                unknownTypes.Add(type);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Failed while loading type '{type.FullName}' in assembly '{assembly.FullName}':\n{ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed while loading types:\n{ex}");
            }

            try
            {
                for (int i = 0; i < writerMethods.Length; i++)
                    additionalTypes.Add(writerMethods[i].Parameters()[0].ParameterType);
            }
            catch (Exception ex)
            {
                log.Error($"Failed while loading sync types");
            }
        }

        public static object Instance(Type type)
        {
            try
            {
                if (instanceDelegates.TryGetValue(type, out var instantiator))
                    return instantiator();
                else
                {
                    instantiator = GenerateInstance(type);

                    if (instantiator is null)
                        throw new InvalidOperationException($"Failed to create an instantiator for type '{type.FullName}'");

                    return (instanceDelegates[type] = instantiator)();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to create an instance of type {type.FullName}:\n{ex}");
                return null;
            }
        }

        public static bool IsDeserializable(Type type)
        {
            if (unknownTypes.Contains(type))
                return false;
            else if (!deserializableTypes.Contains(type))
                return false;
            else
            {
                if (typeof(IDeserialize).IsAssignableFrom(type))
                {
                    deserializableTypes.Add(type);
                    return true;
                }
                else
                {
                    unknownTypes.Add(type);
                    return false;
                }
            }
        }

        public static Action<Writer, object> GetWriter(Type type)
        {
            try
            {
                if (writerDelegates.TryGetValue(type, out var action))
                    return action;
                else
                {
                    action = GenerateWriter(type);

                    if (action is null)
                        throw new InvalidOperationException($"Failed to generate a writer for type '{type.FullName}'");

                    return writerDelegates[type] = action;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to retrieve writer for type {type.FullName}:\n{ex}");
                return null;
            }
        }

        public static Func<Reader, object> GetReader(Type type)
        {
            try
            {
                if (readerDelegates.TryGetValue(type, out var reader))
                    return reader;
                else
                {
                    reader = GenerateReader(type);

                    if (reader is null)
                        throw new InvalidOperationException($"Failed to generate a reader for type '{type.FullName}'");

                    return readerDelegates[type] = reader;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed while retrieving reader for type {type.FullName}:\n{ex}");
                return null;
            }
        }

        private static Action<Writer, object> GenerateWriter(Type type)
        {
            for (int i = 0; i < writerMethods.Length; i++)
            {
                try
                {
                    var methodParams = writerMethods[i].Parameters();

                    if (methodParams[0].ParameterType != type)
                        continue;

                    var instanceExp = Expression.Parameter(typeof(Writer), "instance");
                    var parameterExp = Expression.Parameter(typeof(object), "value");
                    var callExp = Expression.Call(instanceExp, writerMethods[i], parameterExp);
                    var exp = Expression.Lambda<Action<Writer, object>>(callExp, instanceExp, parameterExp);

                    log.Trace($"Generated WRITER: {exp}");

                    return exp.Compile();
                }
                catch (Exception ex)
                {
                    log.Error($"Failed while compiling a writer lambda for {writerMethods[i].ToName()}:\n{ex}");
                }
            }

            return null;
        }

        private static Func<Reader, object> GenerateReader(Type type)
        {
            for (int i = 0; i < readerMethods.Length; i++)
            {
                try
                {
                    var methodType = readerMethods[i].ReturnType;

                    if (methodType != type)
                        continue;

                    var instanceExp = Expression.Parameter(typeof(Reader), "instance");
                    var callExp = Expression.Call(instanceExp, readerMethods[i]);
                    var exp = Expression.Lambda<Func<Reader, object>>(callExp, instanceExp);

                    log.Trace($"Generated READER: {exp}");

                    return exp.Compile();
                }
                catch (Exception ex)
                {
                    log.Error($"Failed while compiling a writer lambda for {writerMethods[i].ToName()}:\n{ex}");
                }
            }

            return null;
        }

        private static Func<object> GenerateInstance(Type type)
        {
            try
            {
                return Expression.Lambda<Func<object>>(Expression.New(type)).Compile();
            }
            catch (Exception ex)
            {
                log.Error($"Failed while compiling an instance lambda for {type.FullName}:\n{ex}");
                return null;
            }
        }
    }
}