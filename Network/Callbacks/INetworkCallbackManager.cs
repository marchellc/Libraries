using Network.Data;
using Network.Features;

using System;
using System.Collections.Generic;

namespace Network.Callbacks
{
    public interface INetworkCallbackManager : INetworkFeature, IDataTarget
    {
        int TotalHandlers { get; }

        void RegisterHandler<THandler>(Func<THandler, bool> handler);
        void RemoveHandler<THandler>(Func<THandler, bool> handler);

        IEnumerable<Delegate> GetHandlers(Type type);
        IEnumerable<Delegate> GetHandlers<THandler>();
    }
}