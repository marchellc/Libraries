using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using Networking.Entities.Messages;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Entities
{
    public class NetEntityDataStorage
    {
        public struct PropertyHook
        {
            public Action<object, object> Proxy;
            public Delegate Regular;
        }

        private readonly LockedDictionary<ushort, object> storedValues = new LockedDictionary<ushort, object>();
        private readonly LockedDictionary<ushort, List<PropertyHook>> hooks = new LockedDictionary<ushort, List<PropertyHook>>();

        public readonly NetEntity Entity;
        public readonly LogOutput Log;

        public event Action<ushort, object, object> OnValueChanged; 

        public NetEntityDataStorage(NetEntity entity, NetEntityData netEntityData)
        {
            Entity = entity;
            Log = new LogOutput($"Entity Data ({Entity.entityId})").Setup();

            for (int i = 0; i < netEntityData.Entries.Length; i++)
            {
                if (netEntityData.Entries[i].Type != NetEntityEntryType.NetworkProperty)
                    continue;

                var valueField = entity.GetType().Field($"{netEntityData.Entries[i].Name}DefValue");

                if (valueField != null)
                    storedValues[netEntityData.Entries[i].ShortCode] = valueField.GetValueFast<object>(entity);
                else
                    storedValues[netEntityData.Entries[i].ShortCode] = default;

                hooks[netEntityData.Entries[i].ShortCode] = new List<PropertyHook>();
                Log.Verbose($"Pre-saved value '{netEntityData.Entries[i].ShortCode}'");
            }
        }

        public void Hook<T>(ushort code, Action<T, T> hook)
        {
            if (!hooks.TryGetValue(code, out var list))
                return;

            if (list.Any(h => h.Regular != null && h.Regular.Method == hook.Method && h.Regular.Target.IsEqualTo(hook.Target)))
                return;

            list.Add(new PropertyHook
            {
                Regular = hook,
                Proxy = (prevValue, newValue) => hook.Call(prevValue is null ? default : (T)prevValue, newValue is null ? default : (T)newValue)
            });
        }

        public void Unhook<T>(ushort code, Action<T, T> hook)
        {
            if (!hooks.TryGetValue(code, out var list))
                return;

            list.RemoveAll(h => h.Regular != null && h.Regular.Method == hook.Method && h.Regular.Target.IsEqualTo(hook.Target));
        }

        public void SetValue(ushort code, object value)
        {
            if (!Entity.IsActive)
            {
                Log.Warn($"Attempted to change value on an inactive entity.");
                return;
            }

            if (!storedValues.ContainsKey(code))
            {
                Log.Warn($"Attempted to set a value with an unknown code: {code}");
                return;
            }

            InternalSetValue(code, value);
            Entity.Send(new NetEntityDataMessage(NetEntityEntryType.NetworkProperty, Entity.entityId, code, new object[] { value }));
        }

        public T GetValue<T>(ushort code)
        {
            if (!TryGetValue<T>(code, out var value))
                return default;

            return value;
        }

        public bool TryGetValue<T>(ushort code, out T value)
        {
            if (!Entity.IsActive)
            {
                value = default;
                return false;
            }

            if (!storedValues.TryGetValue(code, out var storedValue))
            {
                Log.Warn($"Attempted to retrieve value with an unknown code: {code}");

                value = default;
                return false;
            }

            if (storedValue is null)
            {
                value = default;
                return true;
            }

            if (storedValue is not T)
            {
                value = default;
                return false;
            }

            value = (T)storedValue;
            return true;
        }

        internal void InternalSetValue(ushort code, object value)
        {
            var current = GetValue<object>(code);

            Log.Verbose($"Setting value of {code} to {value} (current: {current})");

            storedValues[code] = value;

            OnValueChanged.Call(code, current, value);

            if (hooks.TryGetValue(code, out var propertyHooks))
            {
                foreach (var hook in propertyHooks)
                {
                    hook.Proxy.Call(current, value, null, Log.Error);
                }
            }
        }
    }
}