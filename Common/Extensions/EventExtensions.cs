using Common.Logging;

using System;
using System.Reflection;

namespace Common.Extensions
{
    public static class EventExtensions
    {
        public static LogOutput Log = new LogOutput("Event Extensions").Setup();

        public static EventInfo Event(this Type type, string eventName)
            => type.GetEvent(eventName, MethodExtensions.BindingFlags);

        public static EventInfo[] GetAllEvents(this Type type)
            => type.GetEvents(MethodExtensions.BindingFlags);

        public static void AddHandler(this Type type, string eventName, MethodBase method, object target)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            if (!method.TryCreateDelegate(target, ev.EventHandlerType, out var del))
                return;

            try
            {
                ev.RemoveEventHandler(target, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{method.ToName()}' from event '{ev.ToName()}':\n{ex}");
            }

            try
            {
                ev.AddEventHandler(target, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add event handler '{method.ToName()}' to event '{ev.ToName()}':\n{ex}");
            }
        }

        public static void AddHandler(this Type type, string eventName, MethodBase method)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            if (!method.TryCreateDelegate(ev.EventHandlerType, out var del))
                return;

            try
            {
                ev.RemoveEventHandler(null, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{method.ToName()}' from event '{ev.ToName()}':\n{ex}");
            }

            try
            {
                ev.AddEventHandler(null, del);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add event handler '{method.ToName()}' to event '{ev.ToName()}':\n{ex}");
            }
        }

        public static void AddHandler(this Type type, string eventName, Delegate handler)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            try
            {
                ev.RemoveEventHandler(handler.Target, handler);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{handler.GetMethodInfo().ToName()}' from event '{ev.ToName()}':\n{ex}");
            }

            try
            {
                ev.AddEventHandler(handler.Target, handler);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to add event handler '{handler.GetMethodInfo().ToName()}' to event '{ev.ToName()}':\n{ex}");
            }
        }

        public static void RemoveHandler(this Type type, string eventName, Delegate handler)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException(nameof(eventName));

            if (handler is null)
                throw new ArgumentNullException(nameof(handler));

            var ev = type.Event(eventName);

            if (ev is null)
                throw new ArgumentException($"Failed to find an event of name '{eventName}' in class '{type.ToName()}'");

            try
            {
                ev.RemoveEventHandler(handler.Target, handler);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to remove event handler '{handler.GetMethodInfo().ToName()}' from event '{ev.ToName()}':\n{ex}");
            }
        }

        public static void Raise(this EventInfo ev, object instance, params object[] args)
        {
            if (ev is null)
                throw new ArgumentNullException(nameof(ev));

            var evDelegateField = ev.DeclaringType.Field(ev.Name);

            if (evDelegateField is null)
            {
                Log.Warn($"Tried to raise an event with a missing delegate field: {ev.ToName()}");
                return;
            }

            var evDelegate = evDelegateField.GetValueFast<MulticastDelegate>(instance);

            if (evDelegate is null)
            {
                Log.Warn($"Cannot raise event '{ev.ToName()}', it's delegate is null (perhaps it doesn't have any listeners).");
                return;
            }

            try
            {
                evDelegate.DynamicInvoke(args);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occured while invoking event '{ev.ToName()}':\n{ex}");
            }
        }
    }
}