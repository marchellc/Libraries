using System;

namespace Common.Utilities
{
    public class Disposable : IDisposable
    {
        private bool isDiposed;

        public bool IsDisposed
        {
            get => isDiposed;
        }

        public virtual void OnDispose() { }

        public void Dispose()
        {
            if (isDiposed)
                throw new ObjectDisposedException(GetType().FullName);

            isDiposed = true;

            OnDispose();

            GC.SuppressFinalize(this);
        }
    }
}