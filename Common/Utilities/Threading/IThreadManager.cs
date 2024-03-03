using System;

namespace Common.Utilities.Threading
{
    public interface IThreadManager
    {
        int Size { get; }

        bool IsRunning { get; }

        void Run(ThreadAction threadAction, Action callback);
    }
}