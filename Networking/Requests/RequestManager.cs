using Common.Extensions;
using Common.IO.Collections;
using Common.Utilities;

using Networking.Components;

using System;
using System.Threading.Tasks;

namespace Networking.Requests
{
    public class RequestManager : NetworkComponent
    {
        private LockedDictionary<string, RequestInfo> requests;
        private LockedDictionary<string, Action<ResponseInfo, object>> responses;
        private LockedDictionary<Type, Action<RequestInfo, object>> listeners;

        public override void OnStart()
        {
            base.OnStart();

            requests ??= new LockedDictionary<string, RequestInfo>();
            responses ??= new LockedDictionary<string, Action<ResponseInfo, object>>();
            listeners ??= new LockedDictionary<Type, Action<RequestInfo, object>>();
        }

        public override void OnDestroy()
        {
            requests.Clear();
            responses.Clear();
            listeners.Clear();
        }

        public void Listen<T>(Action<RequestInfo, T> listener)
            => listeners[typeof(T)] = (req, msg) =>
            {
                if (msg != null)
                    listener.Call(req, (T)msg, null, Log.Error);
                else
                    listener.Call(req, default, null, Log.Error);
            };

        public async Task<(T response, bool isSuccess)> RequestAsync<T>(object request)
        {
            ResponseInfo response = default;
            T responseValue = default;
            bool isReceived = false;

            Request<T>(request, (res, msg) =>
            {
                responseValue = msg;
                response = res;

                isReceived = true;
            });

            while (!isReceived)
                await Task.Delay(100);

            return (responseValue, response.IsSuccess);
        }

        public void Request<T>(object request, Action<ResponseInfo, T> responseHandler)
        {
            var requestId = Generator.Instance.GetString();
            
            while (requests.ContainsKey(requestId))
                requestId = Generator.Instance.GetString();

            var requestInfo = new RequestInfo(requestId, request);

            requestInfo.Manager = this;

            requests[requestId] = requestInfo;
            responses[requestId] = (response, msg) =>
            {
                if (msg is null)
                {
                    responseHandler.Call(response, default, null, Log.Error);
                    return;
                }

                responseHandler.Call(response, (T)msg, null, Log.Error);
            };

            if (Client.IsServer)
                CallRpcSendResponse(requestInfo);
            else
                CallCmdSendResponse(requestInfo);
        }

        public void Respond(RequestInfo request, object response, bool isSuccess)
        {
            request.Response = new ResponseInfo(request.Id, response, isSuccess);
            request.IsResponded = true;
            request.Manager = this;

            if (Client.IsServer)
                CallRpcReceiveResponse(request.Response);
            else
                CallCmdReceiveResponse(request.Response);
        }

        public void CallRpcSendResponse(RequestInfo request)
            => InvokeRpc("RpcSendResponse", request);

        public void CallRpcReceiveResponse(ResponseInfo response)
            => InvokeRpc("RpcReceiveResponse", response);

        public void CallCmdSendResponse(RequestInfo request)
            => InvokeCmd("CmdSendResponse", request);

        public void CallCmdReceiveResponse(ResponseInfo response)
            => InvokeCmd("CmdReceiveResponse", response);

        private void RpcSendResponse(RequestInfo request)
            => ProcessRequest(request);

        private void RpcReceiveResponse(ResponseInfo response)
            => ProcessResponse(response);

        private void CmdSendResponse(RequestInfo request)
            => ProcessRequest(request);

        private void CmdReceiveResponse(ResponseInfo response)
            => ProcessResponse(response);

        private void ProcessRequest(RequestInfo request)
        {
            if (request.Request is null)
            {
                Log.Warn($"Received a null request!");
                return;
            }

            var requestType = request.Request.GetType();

            if (!listeners.TryGetValue(requestType, out var listener))
            {
                Log.Warn($"No listeners were found for type {requestType.FullName}!");
                return;
            }

            listener.Call(request, request.Request, null, Log.Error);
        }

        private void ProcessResponse(ResponseInfo response)
        {
            if (!responses.TryGetValue(response.Id, out var responseHandler))
            {
                Log.Warn($"No response handlers for request {response.Id}");
                return;
            }

            response.Manager = this;

            requests.Remove(response.Id);
            responses.Remove(response.Id);

            responseHandler.Call(response, response.Response, null, Log.Error);
        }
    }
}