using Common;
using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Pooling.Pools;

using Networking.Entities.Attributes;

using System;
using System.Collections.Generic;

namespace Networking.Entities
{
    public static class NetEntityUtils
    {
        public static readonly LogOutput Log = new LogOutput("NetEntityUtils").Setup();

        public static void ReloadRegistry(LockedList<NetEntityData> registry)
        {
            registry.Clear();

            var types = ModuleInitializer.SafeQueryTypes();

            foreach (var type in types)
            {
                try
                {
                    if (type.IsStatic() || type.IsInterface || type == typeof(NetEntity) || !type.IsSubclassOf(typeof(NetEntity)))
                        continue;

                    Log.Verbose($"Found NetEntity class: {type.FullName}");

                    var entries = ListPool<NetEntityEntryData>.Shared.Rent();
                    var data = default(NetEntityData);

                    CollectProperties(type, entries);
                    CollectMethods(type, entries);
                    CollectEvents(type, entries);

                    SetMemberCodes(type, entries);

                    if (type.HasAttribute<NetEntityRemoteTypeAttribute>(out var netEntityRemoteTypeAttribute))
                    {
                        if (string.IsNullOrWhiteSpace(netEntityRemoteTypeAttribute.Name))
                        {
                            Log.Warn($"Class '{type.FullName}' has registered a remote-type attribute, but it's name is empty.");
                            continue;
                        }

                        Log.Verbose($"Class '{type.FullName}' has registered a remote-type attribute: {netEntityRemoteTypeAttribute.Name}");
                        data = new NetEntityData(type, netEntityRemoteTypeAttribute.IsNamespace ? netEntityRemoteTypeAttribute.Name : $"{type.Namespace}.{netEntityRemoteTypeAttribute.Name}", ListPool<NetEntityEntryData>.Shared.ToArrayReturn(entries));
                    }
                    else
                        data = new NetEntityData(type, type.Name, ListPool<NetEntityEntryData>.Shared.ToArrayReturn(entries));

                    registry.Add(data);

                    Log.Info(
                        $"Found a new NetEntity class: {type.FullName}\n" +
                        $"- Local Code: {data.LocalCode}\n" +
                        $"- Remote Code: {data.RemoteCode}\n" +
                        $"- Found Entries: {data.Entries.Length}");
                }
                catch { }
            }
        }

        private static void CollectProperties(Type type, List<NetEntityEntryData> entries)
        {
            foreach (var property in type.GetAllProperties())
            {
                var setMethod = property.GetSetMethod(true);
                var getMethod = property.GetGetMethod(true);

                if (setMethod is null || setMethod.IsStatic)
                    continue;

                if (getMethod is null || getMethod.IsStatic)
                    continue;

                if (!property.CanWrite || !property.CanRead)
                    continue;

                if (!property.Name.StartsWith("Network"))
                    continue;

                var code = property.Name.GetShortCode();

                entries.Add(new NetEntityEntryData(NetEntityEntryType.NetworkProperty, code, property, property.Name));

                Log.Verbose($"Found a new property entry in '{type.FullName}': {property.Name} ({code})");
            }
        }

        private static void CollectMethods(Type searchType, List<NetEntityEntryData> entries)
        {
            foreach (var method in searchType.GetAllMethods())
            {
                if (method.IsStatic || method.ReturnType != typeof(void))
                    continue;

                var code = method.Name.GetShortCode();
                var type = NetEntityEntryType.ClientCode;

                if (method.Name.StartsWith("Client"))
                    type = NetEntityEntryType.ClientCode;
                else if (method.Name.StartsWith("Server"))
                    type = NetEntityEntryType.ServerCode;
                else
                    continue;

                entries.Add(new NetEntityEntryData(type, code, method, method.Name));

                Log.Verbose($"Found a new {type} entry in '{searchType.FullName}': {method.Name} ({code})");
            }
        }

        private static void CollectEvents(Type type, List<NetEntityEntryData> entries)
        {
            foreach (var ev in type.GetAllEvents())
            {
                if (!ev.IsMulticast)
                    continue;

                if (!ev.Name.StartsWith("Network"))
                    continue;

                var code = ev.Name.GetShortCode();

                entries.Add(new NetEntityEntryData(NetEntityEntryType.NetworkEvent, code, ev, ev.Name));

                Log.Verbose($"Found a new event entry in '{type.FullName}': {ev.Name} ({code})");
            }
        }

        private static void SetMemberCodes(Type type, IEnumerable<NetEntityEntryData> entries)
        {
            foreach (var field in type.GetAllFields())
            {
                if (!field.IsStatic || field.FieldType != typeof(ushort) || field.IsInitOnly || !field.Name.EndsWith("Code"))
                    continue;

                var memberName = field.Name.Replace("Code", "");
                var memberCode = memberName.GetShortCode();

                field.SetValueFast(memberCode);

                Log.Verbose($"Set code '{memberCode}' to field '{field.Name}' in '{type.FullName}' ({memberName})");
            }
        }
    }
}