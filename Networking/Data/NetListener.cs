using Common.Extensions;
using Common.IO.Collections;
using Common.IO.Data;
using Common.Utilities;

using Networking.Interfaces;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Networking.Enums;

namespace Networking.Data
{
    public class NetListener : NetComponent, ITarget
    {
        private readonly LockedDictionary<Type, List<WrappedListener>> activeListeners = new LockedDictionary<Type, List<WrappedListener>>();

        public override void Stop()
            => Clear();

        public bool Listen<T>(IListener<T> listener) where T : IData
        {
            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            if (!activeListeners.TryGetValue(typeof(T), out var listeners))
                activeListeners[typeof(T)] = listeners = new List<WrappedListener>();

            var listenerType = listener.GetType();

            if (listeners.Any(lis => lis.Type == listenerType && lis.Target.IsEqualTo(listener)))
                return false;

            listeners.Add(new WrappedListener(value => listener.Process((T)value), listenerType.Assembly, listenerType, listener));

            listener.Listener = this;
            listener.OnRegistered();

            Log.Verbose($"Registered listener for '{typeof(T).FullName}': {listener.GetType().FullName}");

            return true;
        }

        public bool Clear<T>(IListener<T> listener) where T : IData
        {
            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            if (!activeListeners.TryGetValue(typeof(T), out var listeners))
                return false;

            if (listeners.RemoveAll(lis => lis.Target == listener && lis.Type == listener.GetType()) > 0)
            {
                listener.OnUnregistered();
                return true;
            }

            return false;
        }

        public bool Clear<T>() where T : IData
        {
            if (activeListeners.TryGetValue(typeof(T), out var listeners))
            {
                foreach (var listener in listeners)
                {
                    var method = listener.Type.Method("OnUnregistered");

                    if (method is null)
                        continue;

                    method.Call(listener.Target);
                }

                listeners.Clear();
            }

            return activeListeners.Remove(typeof(T));
        }

        public void Clear()
        {
            foreach (var pair in activeListeners)
            {
                foreach (var listener in pair.Value)
                {
                    var method = listener.Type.Method("OnUnregistered");

                    if (method is null)
                        continue;

                    method.Call(listener.Target);
                }

                pair.Value.Clear();
            }

            activeListeners.Clear();
        }

        public bool TryProcess(IData data)
        {
            if (data is null)
                return false;

            var type = data.GetType();

            if (!activeListeners.TryGetValue(type, out var listeners))
                return false;

            foreach (var listener in listeners)
            {
                var result = listener.Delegate.Call(data);

                if (result is ListenerResult.Skip)
                    return true;
            }

            return true;
        }

        public struct WrappedListener
        {
            public readonly Func<IData, ListenerResult> Delegate;
            public readonly Assembly Assembly;
            public readonly Type Type;
            public readonly object Target;

            public WrappedListener(Func<IData, ListenerResult> delegateValue, Assembly assembly, Type type, object target)
            {
                Delegate = delegateValue;
                Assembly = assembly;
                Type = type;
                Target = target;
            }
        }
    }
}