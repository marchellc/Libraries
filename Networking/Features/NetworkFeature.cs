using Common.Extensions;
using Common.Logging;

using System;
using System.Collections.Generic;

namespace Networking.Features
{
    public class NetworkFeature
    {
        private Dictionary<Type, Action<object>> listeners = new Dictionary<Type, Action<object>>();

        public bool IsRunning { get; private set; }

        public LogOutput Log { get; private set; }
        public NetworkFunctions Network { get; private set; }

        public virtual void Start() { }
        public virtual void Stop() { }

        public void Listen<TMessage>(Action<TMessage> listener)
            => listeners[typeof(TMessage)] = msg =>
            {
                listener.Call((TMessage)msg);
            };

        public void Remove<TMessage>()
            => listeners.Remove(typeof(TMessage));

        internal void InternalStart(NetworkFunctions network)
        {
            if (IsRunning)
                throw new InvalidOperationException($"This feature is already running");

            IsRunning = true;

            Network = network;

            try
            {
                Log = new LogOutput(GetType().Name.SpaceByUpperCase()).Setup();

                Start();
            }
            catch (Exception ex)
            {
                Log?.Error(ex);
            }
        }

        internal void InternalStop()
        {
            if (!IsRunning)
                throw new InvalidOperationException($"This feature is not running");

            IsRunning = false;

            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Log?.Error(ex);
            }

            Log.Dispose();
            Log = null;

            Network = null;

            listeners.Clear();
        }

        internal bool HasListener(Type type)
            => listeners.ContainsKey(type);

        internal void Receive(object msg)
        {
            var msgType = msg.GetType();

            if (!listeners.TryGetValue(msgType, out var listener))
                return;

            listener.Call(msg);
        }
    }
}