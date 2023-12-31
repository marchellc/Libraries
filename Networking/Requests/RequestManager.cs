using Common.Extensions;
using Common.IO.Collections;
using Common.Utilities;

using Networking.Objects;

using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Networking.Requests
{
    public class RequestManager : NetworkObject
    {
        private LockedDictionary<string, Tuple<RequestInfo, Tuple<MethodInfo, object>>> waitingRequests;
        private LockedDictionary<Type, Tuple<MethodInfo, object>> requestHandlers;
        private Timer timer;

        private ushort CmdGetResponseHash;
        private ushort CmdSetResponseHash;

        private ushort RpcGetResponseHash;
        private ushort RpcSetResponseHash;

        public RequestManager(int id, NetworkManager manager) : base(id, manager) { }

        public override void OnStart()
        {
            waitingRequests = new LockedDictionary<string, Tuple<RequestInfo, Tuple<MethodInfo, object>>>();
            timer = new Timer(_ => Update(), null, 100, 250);     
        }

        public override void OnStop()
        {
            waitingRequests.Clear();
            waitingRequests = null;

            timer?.Dispose();
            timer = null;
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

            waitingRequests[requestId] = new Tuple<RequestInfo, Tuple<MethodInfo, object>>(requestInfo, new Tuple<MethodInfo, object>(responseHandler.GetMethodInfo(), responseHandler.Target));

            if (net.isServer)
                CallRpcGetResponse(requestInfo);
            else
                CallCmdGetResponse(requestInfo);
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

            if (net.isServer)
                CallRpcSetResponse(request, responseInfo);
            else
                CallCmdSetResponse(request, responseInfo);
        }

        public void CallCmdGetResponse(RequestInfo request)
            => SendCmd(CmdGetResponseHash, request);

        public void CallCmdSetResponse(RequestInfo request, ResponseInfo response)
            => SendCmd(CmdSetResponseHash, request, response);

        public void CallRpcGetResponse(RequestInfo request)
            => SendRpc(RpcGetResponseHash, request);

        public void CallRpcSetResponse(RequestInfo request, ResponseInfo response)
            => SendRpc(RpcSetResponseHash, request, response);

        public void RpcSetResponse(RequestInfo request, ResponseInfo response)
        {
            if (!waitingRequests.TryGetValue(request.id, out var handler))
                return;

            handler.Item1.isResponded = true;
            handler.Item1.isTimedOut = false;
            handler.Item1.manager = this;

            handler.Item1.receivedAt = DateTime.Now;

            handler.Item2.Item1.Call(handler.Item2.Item1, [response, response.response]);
        }

        public void RpcGetResponse(RequestInfo request)
        {
            if (request.value is null || !requestHandlers.TryGetValue(request.value.GetType(), out var handler))
                return;

            request.manager = this;
            request.isResponded = false;
            request.isTimedOut = false;
            request.receivedAt = DateTime.Now;

            handler.Item1.Call(handler.Item2, [request, request.value]);
        }

        public void CmdSetResponse(RequestInfo request, ResponseInfo response)
        {
            if (!waitingRequests.TryGetValue(request.id, out var handler))
                return;

            handler.Item1.isResponded = true;
            handler.Item1.isTimedOut = false;
            handler.Item1.manager = this;

            handler.Item1.receivedAt = DateTime.Now;

            handler.Item2.Item1.Call(handler.Item2.Item1, [response, response.response]);
        }

        public void CmdGetResponse(RequestInfo request)
        {
            if (request.value is null || !requestHandlers.TryGetValue(request.value.GetType(), out var handler))
                return;

            request.manager = this;
            request.isTimedOut = false;
            request.isResponded = false;
            request.receivedAt = DateTime.Now;

            handler.Item1.Call(handler.Item2, [request, request.value]);
        }

        private void Update()
        {
            foreach (var pair in waitingRequests)
            {
                if ((DateTime.Now - pair.Value.Item1.sentAt).TotalSeconds >= 10)
                {
                    pair.Value.Item1.isTimedOut = true;
                    continue;
                }
            }

            var timedOut = waitingRequests.Where(pair => pair.Value.Item1.isTimedOut);

            foreach (var req in timedOut)
            {
                req.Value.Item1.isResponded = false;
                req.Value.Item1.isTimedOut = true;
                req.Value.Item1.manager = this;
                req.Value.Item1.response = null;
                 
                req.Value.Item2.Item1.Call(req.Value.Item2.Item2, [req.Value.Item1, null]);

                waitingRequests.Remove(req.Key);
            }
        }
    }
}