using Common.Extensions;
using Common.IO.Collections;

using System;
using System.Threading.Tasks;

namespace Networking.Requests
{
    public class RequestManager : NetComponent
    {
        private readonly LockedDictionary<Type, Action<RequestInfo, object>> handlers = new LockedDictionary<Type, Action<RequestInfo, object>>();
        private readonly LockedDictionary<ulong, RequestInfo> requests = new LockedDictionary<ulong, RequestInfo>();

        private ulong requestId = 0;

        public event Action<RequestInfo> OnReceived;
        public event Action<RequestInfo> OnResponded;

        public override void Start()
        {
            base.Start();
            Listener.Listen(new RequestInfo.RequestListener());
        }

        public override void Stop()
        {
            base.Stop();

            Listener.Clear<RequestInfo>();

            handlers.Clear();
            requests.Clear();

            requestId = 0;
        }

        public void Listen<T>(Action<RequestInfo, T> handler)
            => handlers[typeof(T)] = (req, value) => handler.Call(req, (T)value);

        public async Task<T> SendAsync<T>(object request)
        {
            try
            {
                var info = new RequestInfo(++requestId, request);
                var response = false;

                info.Manager = this;
                info.handler = (req, value) =>
                {
                    response = true;
                    info = req;
                };

                Send(info);

                Log.Verbose($"Sent async request '{info.Id}' ({request.GetType().FullName})");

                while (!response)
                    await Task.Delay(50);

                return info.IsSuccess ? (T)info.Response : default;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return default;
            }
        }

        public void Send<T>(object request, Action<RequestInfo, T> response)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (response is null)
                throw new ArgumentNullException(nameof(response));

            try
            {
                var info = new RequestInfo(++requestId, request);

                info.Manager = this;
                info.handler = (req, value) => response.Call(req, (T)value);

                requests[info.Id] = info;

                Send(info);

                Log.Verbose($"Sent request '{info.Id}' ({request.GetType().FullName})");
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        internal void OnResponse(RequestInfo request)
        {
            try
            {
                if (request.Response is null || !request.HasResponse)
                {
                    Log.Warn($"Received an invalid response");
                    return;
                }

                if (!requests.TryGetValue(requestId, out var cachedRequest))
                {
                    Log.Warn($"Received an unknown response");
                    return;
                }

                cachedRequest.handler.Call(request, request.Response);

                OnReceived.Call(request);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        internal void OnRequest(RequestInfo request)
        {
            try
            {
                if (request.Value is null)
                {
                    Log.Warn($"Received a request with an empty object!");
                    return;
                }

                var type = request.Value.GetType();

                if (!handlers.TryGetValue(type, out var handler))
                {
                    Log.Warn($"No handlers present for request '{type.FullName}'");
                    return;
                }

                handler.Call(request, request.Value);

                OnResponded.Call(request);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}