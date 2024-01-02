using Common.Extensions;
using Common.Values;

using System;
using System.Collections.Concurrent;
using System.Reflection;
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

        private static readonly ConcurrentQueue<DelayedExecutionInfo> delayedExecution;
        private static readonly object queueLock = new object();

        static CodeUtils()
        {
            delayedExecution = new ConcurrentQueue<DelayedExecutionInfo>();

            WhileTrue(() => true, () =>
            {
                lock (queueLock)
                {
                    while (delayedExecution.TryDequeue(out var delayedExecutionInfo))
                    {
                        if ((DateTime.Now - delayedExecutionInfo.added).TotalMilliseconds >= delayedExecutionInfo.delay)
                        {
                            delayedExecutionInfo.target.Call();
                        }
                        else
                        {
                            delayedExecution.Enqueue(delayedExecutionInfo);
                        }
                    }
                }
            }, 5);
        }

        public static void For(int count, Action action)
            => For(0, count, action);

        public static void For(int start, int end, Action action)
        {
            for (int i = start; i < end; i++)
                action();
        }

        public static void OnFalse(Func<bool> validator, Action action)
        {
            while (validator())
                continue;

            action();
        }

        public static void OnTrue(Func<bool> validator, Action action)
        {
            while (!validator())
                continue;

            action();
        }

        public static void WhileTrue(Func<bool> validator, Action action)
        {
            while (validator())
                action();
        }

        public static void WhileFalse(Func<bool> validator, Action action)
        {
            while (!validator())
                action();
        }

        public static void WhileTrue(Func<bool> validator, Action action, int period)
            => WhileTrue(validator, action, period, 0);

        public static void WhileTrue(Func<bool> validator, Action action, int period, int delay)
        {
            var timerRef = new ReferenceValue<Timer>(null);
            var timer = new Timer(timerRefValue =>
            {
                if (timerRefValue is null || timerRefValue is not ReferenceValue<Timer> refTimer)
                    return;

                if (refTimer.Value is null)
                    return;

                if (!validator())
                {
                    refTimer.Value.Dispose();
                    return;
                }
                else
                {
                    action();
                }

            }, timerRef, delay, period);

            timerRef.Value = timer;
        }

        public static void OnThread(Action threadAction, Action callbackAction = null)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                threadAction.Call();
                callbackAction?.Call();
            });
        }

        public static void OnThread<T>(Func<T> threadFunc, Action<T> callbackAction)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                callbackAction(threadFunc());
            });
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
    }
}