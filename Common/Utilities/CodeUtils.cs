using Common.Extensions;
using Common.Logging;
using Common.Values;

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Common.Utilities
{
    public static class CodeUtils
    {
        private struct DelayedExecutionInfo
        {
            public Action target;
            public DateTime added;
            public int delay;
        }

        private static ConcurrentQueue<DelayedExecutionInfo> delayedExecution;
        private static object queueLock;

        public static LogOutput Log { get; private set; }

        static CodeUtils()
        {
            Log = new LogOutput("Code Utils").Setup();

            delayedExecution = new ConcurrentQueue<DelayedExecutionInfo>();
            queueLock = new object();

            WhileTrue(() => true, () =>
            {
                try
                {
                    lock (queueLock)
                    {
                        while (delayedExecution.TryDequeue(out var delayedExecutionInfo))
                        {
                            if ((DateTime.Now - delayedExecutionInfo.added).TotalMilliseconds >= delayedExecutionInfo.delay)
                            {
                                delayedExecutionInfo.target.Call(null, ex => Log.Error($"Delayed execution of '{delayedExecutionInfo.target.Method.ToName()}' caught an exception:\n{ex}"));
                            }
                            else
                            {
                                delayedExecution.Enqueue(delayedExecutionInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }, 5);

            Log.Info($"Queue timer started.");
        }

        public static void For(int count, Action action)
            => For(0, count, action);

        public static void For(int start, int end, Action action)
        {
            for (int i = start; i < end; i++)
                action.Call(null, ex => Log.Error($"An exception occured while executing 'For' at index '{i}':\n{ex}"));
        }

        public static void OnFalse(Func<bool> validator, Action action)
        {
            while (validator.Call(ex => Log.Error($"'OnFalse' validator caught an exception:\n{ex}")))
                continue;

            action.Call(null, ex => Log.Error($"'OnFalse' action caught an exception:\n{ex}"));
        }

        public static void OnTrue(Func<bool> validator, Action action)
        {
            while (!validator.Call(ex => Log.Error($"'OnTrue' validator caught an exception:\n{ex}")))
                continue;

            action.Call(null, ex => Log.Error($"'OnTrue' action caught an exception:\n{ex}"));
        }

        public static void WhileTrue(Func<bool> validator, Action action)
        {
            while (validator.Call(ex => Log.Error($"'WhileTrue' validator caught an exception:\n{ex}")))
                action.Call(null, ex => Log.Error($"'WhileTrue' action caught an exception:\n{ex}"));
        }

        public static void WhileFalse(Func<bool> validator, Action action)
        {
            while (validator.Call(ex => Log.Error($"'WhileFalse' validator caught an exception:\n{ex}")))
                action.Call(null, ex => Log.Error($"'WhileFalse' action caught an exception:\n{ex}"));
        }

        public static void WhileTrue(Func<bool> validator, Action action, int period)
            => WhileTrue(validator, action, period, 0);

        public static void WhileTrue(Func<bool> validator, Action action, int period, int delay)
        {
            if (delay < 0)
                delay = 0;

            if (period < 0)
                throw new ArgumentOutOfRangeException(nameof(period));

            var timerRef = new ReferenceValue<Timer>(null);

            var timer = new Timer(timerRefValue =>
            {
                if (timerRefValue is null || timerRefValue is not ReferenceValue<Timer> refTimer || refTimer.Value is null)
                    return;

                if (!validator.Call(ex => Log.Error($"'WhileTrue' timer validator caught an exception:\n{ex}")))
                {
                    refTimer.Value.Dispose();
                    return;
                }
                else
                {
                    action.Call(null, ex => Log.Error($"'WhileTrue' timer action caught an exception:\n{ex}"));
                }

            }, timerRef, delay, period);

            timerRef.Value = timer;

            Log?.Verbose($"Started a new 'WhileTrue' timer (period of '{period} ms' with a delay of '{delay} ms'): {action.Method.ToName()}");
        }

        public static void OnThread(Action threadAction, Action callbackAction = null, ThreadPriority priority = ThreadPriority.Normal, bool isBackground = true)
        {
            var thread = new Thread(() =>
            {
                threadAction.Call(null, ex => Log.Error($"'OnThread' action caught an exception:\n{ex}"));
                callbackAction.Call(null, ex => Log.Error($"'OnThread' callback action caught an exception:\n{ex}"));
            });

            thread.Priority = priority;
            thread.IsBackground = isBackground;

            thread.Start();
        }

        public static void OnThread<T>(Func<T> threadFunc, Action<T> callbackAction, ThreadPriority priority = ThreadPriority.Normal, bool isBackground = true)
        {
            var thread = new Thread(() =>
            {
                var result = threadFunc.Call(ex => Log.Error($"'OnThread<T>' function caught an exception:\n{ex}"));
                callbackAction.Call(result, null, ex => Log.Error($"'OnThread<T>' callback caught an exception:\n{ex}"));
            });

            thread.Priority = priority;
            thread.IsBackground = isBackground;

            thread.Start();
        }

        public static void Delay(Action delayAction, int delay)
        {
            lock (queueLock)
                delayedExecution.Enqueue(new DelayedExecutionInfo
                {
                    added = DateTime.Now,

                    delay = delay,
                    target = delayAction
                });
        }

        public static void InlinedIf(bool condition, bool stopCondition, Action action, Action onAction)
        {
            if (stopCondition || !condition)
                return;

            action.Call(onAction, Log.Error);
        }

        public static void InlinedIf(Func<bool> condition, Func<bool> stopCondition, Action action, Action onAction)
        {
            if (stopCondition.Call(Log.Error) || !condition.Call(Log.Error))
                return;

            action.Call(onAction, Log.Error);
        }

        public static void InlinedElse(bool condition, bool stopCondition, Action trueAction, Action falseAction, Action onTrueAction, Action onFalseAction)
        {
            if (stopCondition)
                return;

            if (condition)
                trueAction.Call(onTrueAction, Log.Error);
            else
                falseAction.Call(onFalseAction, Log.Error);
        }

        public static void InlinedElse(Func<bool> condition, Func<bool> stopCondition, Action trueAction, Action falseAction, Action onTrueAction, Action onFalseAction)
        {
            if (stopCondition.Call(Log.Error))
                return;

            if (condition.Call(Log.Error))
                trueAction.Call(onTrueAction, Log.Error);
            else
                falseAction.Call(onFalseAction, Log.Error);
        }

        public static T ModifyStruct<T>(this T value, Action<T> modify) where T : struct
        {
            modify.Call(value);
            return value;
        }
    }
}