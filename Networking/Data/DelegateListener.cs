using Common.Extensions;
using Common.IO.Data;

using Networking.Enums;

using System;

namespace Networking.Data
{
    public class DelegateListener<T> : BasicListener<T>
        where T : IData
    {
        public Func<T, ListenerResult> ProcessDelegate { get; set; }

        public Action OnRegisteredDelegate { get; set; }
        public Action OnUnregisteredDelegate { get; set; }

        public DelegateListener(Func<T, ListenerResult> processDelegate = null, Action onRegisteredDelegate = null, Action onUnregisteredDelegate = null)
        {
            ProcessDelegate = processDelegate;
            OnRegisteredDelegate = onRegisteredDelegate;
            OnUnregisteredDelegate = onUnregisteredDelegate;
        }

        public override void OnRegistered()
        {
            base.OnRegistered();
            OnRegisteredDelegate.Call();
        }

        public override void OnUnregistered()
        {
            base.OnUnregistered();
            OnUnregisteredDelegate.Call();
        }

        public override ListenerResult Process(T message)
        {
            if (ProcessDelegate is null)
                return ListenerResult.Failed;

            return ProcessDelegate.Call(message);
        }
    }
}