using System;
using System.Reflection;

namespace Common.Utilities.Threading
{
    public class ThreadAction
    {
        private static long IdValue = 0;

        internal ThreadResult threadResult;
        internal TimeSpan duration;
        internal Exception exception;
        internal object result;

        public readonly MethodInfo TargetMethod;

        public readonly IThreadManager TargetThread;

        public readonly object TargetObject;
        public readonly object[] TargetArgs;

        public readonly bool IsMeasure;

        public readonly long Id;

        public ThreadResult ThreadResult
        {
            get => threadResult;
        }

        public TimeSpan Duration
        {
            get => duration;
        }

        public Exception Exception
        {
            get => exception;
        }

        public object Result
        {
            get => result;
        }

        public bool IsSuccess
        {
            get => threadResult is ThreadResult.Success;
        }

        public bool IsFinished
        {
            get => threadResult != ThreadResult.NotRun;
        }

        public ThreadAction(MethodInfo method, object target, object[] args, IThreadManager threadManager, bool measureDuration)
        {
            TargetMethod = method;
            TargetObject = target;
            TargetThread = threadManager;
            TargetArgs = args;

            IsMeasure = measureDuration;

            threadResult = ThreadResult.NotRun;

            if (IdValue >= long.MaxValue)
                IdValue = 0;

            Id = IdValue++;
        }

        public T GetResult<T>()
        {
            if (!IsSuccess || result is null)
                return default;

            return (T)result;
        }

        public void OnRun(Exception exception, object result, TimeSpan duration)
        {
            if (this.threadResult != ThreadResult.NotRun)
                throw new InvalidOperationException($"This action has already been run!");

            this.exception = exception;
            this.duration = duration;
            this.result = result;

            if (exception != null)
                this.threadResult = ThreadResult.Exception;
            else
                this.threadResult = ThreadResult.Success;
        }
    }
}