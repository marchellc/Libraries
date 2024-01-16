using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Utilities;

using Networking.Objects;

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Networking.Requests
{
    public class RequestManager : NetworkObject
    {
        private LockedDictionary<string, Tuple<RequestInfo, Tuple<MethodInfo, object>>> waitingRequests;
        private LockedDictionary<Type, Tuple<MethodInfo, object>> requestHandlers;
        private LogOutput log;

        private static ushort CmdGetResponseHash;
        private static ushort CmdSetResponseHash;

        private static ushort RpcGetResponseHash;
        private static ushort RpcSetResponseHash;

        public RequestManager(NetworkManager manager) : base(manager) { }

        public override void OnStart()
        {
            waitingRequests = new LockedDictionary<string, Tuple<RequestInfo, Tuple<MethodInfo, object>>>();
            requestHandlers = new LockedDictionary<Type, Tuple<MethodInfo, object>>();

            log = new LogOutput("Request Manager");
            log.Setup();

            log.Info("Enabled.");
        }

        public override void OnStop()
        {
            waitingRequests.Clear();
            waitingRequests = null;

            requestHandlers.Clear();
            requestHandlers = null;

            log.Info("Disabled.");
            log.Dispose();
            log = null;
        }

        public void Request<T>(object request, Action<ResponseInfo, T> responseHandler)
        {
            var requestId = Generator.Instance.GetString(10, true);
            var requestInfo = new RequestInfo
            {
                id = requestId,

                isResponded = false,
                isTimedOut = false,

                manager = this,

                value = request,

                response = null
            };

            waitingRequests[requestId] = new Tuple<RequestInfo, Tuple<MethodInfo, object>>(requestInfo, new Tuple<MethodInfo, object>(responseHandler.Method, responseHandler.Target));

            if (net.IsServer)
                CallRpcGetResponse(requestInfo);
            else
                CallCmdGetResponse(requestInfo);
        }

        public async Task<AsyncResponseInfo<T>> RequestAsync<T>(object request)
        {
            var responseInfo = default(ResponseInfo);
            var responseValue = default(T);
            var responseReceived = false;

            Request<T>(request, (res, val) =>
            {
                responseInfo = res;
                responseValue = val;
                responseReceived = true;
            });

            while (!responseReceived || responseInfo is null)
                await Task.Delay(20);

            return new AsyncResponseInfo<T>(responseInfo, responseValue);
        }

        public void Respond(RequestInfo request, object response, bool isSuccess)
        {
            var responseInfo = new ResponseInfo
            {
                id = request.id,
                isSuccess = isSuccess,
                manager = this,
                request = request,
                response = response
            };

            request.isResponded = true;
            request.isTimedOut = false;

            if (net.IsServer)
                CallRpcSetResponse(request, responseInfo);
            else
                CallCmdSetResponse(request, responseInfo);
        }

        public void Handle<T>(Action<RequestInfo, T> handler)
        {
            requestHandlers[typeof(T)] = new Tuple<MethodInfo, object>(handler.Method, handler.Target);
            log.Info($"Registered request handler of '{typeof(T).FullName}' to '{handler.Method.ToName()}'");
        }

        public void Remove<T>()
        {
            if (requestHandlers.Remove(typeof(T)))
                log.Info($"Removed request handler for '{typeof(T).FullName}'");
        }

        private void CallCmdGetResponse(RequestInfo request)
            => SendCmd(CmdGetResponseHash, request);

        private void CallCmdSetResponse(RequestInfo request, ResponseInfo response)
            => SendCmd(CmdSetResponseHash, request, response);

        private void CallRpcGetResponse(RequestInfo request)
            => SendRpc(RpcGetResponseHash, request);

        private void CallRpcSetResponse(RequestInfo request, ResponseInfo response)
            => SendRpc(RpcSetResponseHash, request, response);

        private void RpcSetResponse(RequestInfo request, ResponseInfo response)
        {
            if (!waitingRequests.TryGetValue(request.id, out var handler))
                return;

            log.Info($"Received response for request of ID {request.id}: {response.isSuccess}");

            handler.Item1.isResponded = true;
            handler.Item1.isTimedOut = false;
            handler.Item1.manager = this;

            handler.Item1.receivedAt = DateTime.Now;

            handler.Item2.Item1.Call(handler.Item2.Item1, [response, response.response]);
        }

        private void RpcGetResponse(RequestInfo request)
        {
            if (request.value is null || !requestHandlers.TryGetValue(request.value.GetType(), out var handler))
                return;

            log.Info($"Received request of ID {request.id}");

            request.manager = this;
            request.isResponded = false;
            request.isTimedOut = false;
            request.receivedAt = DateTime.Now;

            handler.Item1.Call(handler.Item2, [request, request.value]);
        }

        private void CmdSetResponse(RequestInfo request, ResponseInfo response)
        {
            if (!waitingRequests.TryGetValue(request.id, out var handler))
                return;

            log.Info($"Received response for request of ID {request.id}: {response.isSuccess}");

            handler.Item1.isResponded = true;
            handler.Item1.isTimedOut = false;
            handler.Item1.manager = this;

            handler.Item1.receivedAt = DateTime.Now;

            handler.Item2.Item1.Call(handler.Item2.Item2, [response, response.response]);
        }

        private void CmdGetResponse(RequestInfo request)
        {
            if (request.value is null || !requestHandlers.TryGetValue(request.value.GetType(), out var handler))
                return;

            log.Info($"Received request of ID {request.id}");

            request.manager = this;
            request.isTimedOut = false;
            request.isResponded = false;
            request.receivedAt = DateTime.Now;

            handler.Item1.Call(handler.Item2, [request, request.value]);
        }
    }
}