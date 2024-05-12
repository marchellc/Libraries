using Common.Extensions;
using Common.Pooling;

using System;
using System.Threading.Tasks;

namespace Network.Callbacks
{
    public class AsyncNetworkCallback : PoolableItem
    {
        private bool _isActive;
        private object _result;

        public void Execute(object result)
        {
            if (_isActive)
                throw new InvalidOperationException($"This callback has already been executed.");

            _isActive = true;
            _result = result;
        }

        public override void OnUnPooled()
        {
            base.OnUnPooled();

            _isActive = false;
            _result = null;
        }

        public async Task<T> AwaitAsync<T>(int timeout, Action execute)
        {
            execute.Call();

            var time = DateTime.Now;

            while (!_isActive)
            {
                await Task.Delay(10);

                if (timeout > 0 && (DateTime.Now - time).TotalMilliseconds >= timeout)
                    break;
            }

            if (!_isActive)
                throw new TimeoutException($"This operation has timed out.");

            if (!_result.TryTypeCast<T>(out var castValue))
                throw new InvalidOperationException($"An invalid object was provided as the operation's result.");

            return castValue;
        }
    }
}