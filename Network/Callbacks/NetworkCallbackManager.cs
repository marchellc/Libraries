using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using Network.Data;
using Network.Features;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Network.Callbacks
{
    public class NetworkCallbackManager : NetworkFeatureBase, INetworkCallbackManager, IDataTarget
    {
        private readonly List<Delegate> _emptyList = new List<Delegate>();
        private readonly LockedDictionary<Type, List<Delegate>> _handlers = new LockedDictionary<Type, List<Delegate>>();

        public int TotalHandlers => _handlers.Count;

        public IEnumerable<Delegate> GetHandlers(Type type)
            => _handlers.TryGetValue(type, out var handlers) ? handlers : _emptyList;

        public IEnumerable<Delegate> GetHandlers<THandler>()
            => _handlers.TryGetValue(typeof(THandler), out var handlers) ? handlers : _emptyList;

        public void RegisterHandler<THandler>(Func<THandler, bool> handler)
        {
            if (!_handlers.TryGetValue(typeof(THandler), out var handlers))
                handlers = _handlers[typeof(THandler)] = new List<Delegate>();

            if (handlers.Any(h => h.Method == handler.Method && h.Target.IsEqualTo(handler.Target)))
                return;

            handlers.Add(handler);
        }


        public void RemoveHandler<THandler>(Func<THandler, bool> handler)
        {
            if (!_handlers.TryGetValue(typeof(THandler), out var handlers))
                return;

            handlers.RemoveAll(h => h.Method == handler.Method && h.Target.IsEqualTo(handler.Target));
        }

        public bool Process(object data)
        {
            if (data is null)
                return false;

            var type = data.GetType();

            if (!_handlers.TryGetValue(type, out var handlers))
                return false;

            foreach (var handler in handlers)
            {
                try
                {
                    var result = handler.DynamicInvoke(data);

                    if (result is null || result is not bool handlerResult)
                        continue;

                    if (!handlerResult)
                        return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return false;
        }

        public bool Accepts(object data)
            => data != null;
    }
}