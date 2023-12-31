using Common.Extensions;
using Common.Logging;

using Networking.Data;

using System;
using System.Collections.Generic;

namespace Networking.Features
{
    public class NetworkFeature
    {
        private Dictionary<Type, Action<object>> listeners = new Dictionary<Type, Action<object>>();

        public bool isRunning;

        public LogOutput log;
        public NetworkFunctions net;

        public virtual bool Receive(Reader reader) => false;

        public virtual void Start() { }
        public virtual void Stop() { }
        public virtual void SetupLog(LogOutput log) { }

        public void Listen<TMessage>(Action<TMessage> listener)
            => listeners[typeof(TMessage)] = msg =>
            {
                listener.Call((TMessage)msg);
            };

        public void Remove<TMessage>()
            => listeners.Remove(typeof(TMessage));

        internal void InternalStart()
        {
            if (isRunning)
                throw new InvalidOperationException($"This feature is already running");

            isRunning = true;

            log = new LogOutput(GetType().Name.SpaceByUpperCase());

            SetupLog(log);

            Start();
        }

        internal void InternalStop()
        {
            if (!isRunning)
                throw new InvalidOperationException($"This feature is not running");

            isRunning = false;

            Stop();

            log.Dispose();
            log = null;

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