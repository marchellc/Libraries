using Common.IO.Collections;
using Common.Utilities;

using Network.Controllers;
using Network.Features;
using Network.Peers;

using System;

namespace Network.Requests
{
    public class RequestManager : NetworkFeatureBase, IRequestManager
    {
        private int m_Sent = 0;
        private int m_Received = 0;

        private readonly LockedDictionary<string, Delegate> m_Waiting = new LockedDictionary<string, Delegate>();
        private readonly LockedDictionary<Type, Delegate> m_Handlers = new LockedDictionary<Type, Delegate>();

        public int Waiting => m_Waiting.Count;

        public int Sent => m_Sent;
        public int Received => m_Received;

        public override void OnDisabled()
        {
            base.OnDisabled();

            m_Sent = 0;
            m_Received = 0;
            m_Waiting.Clear();
        }

        public override void OnEnabled()
        {
            base.OnEnabled();

            m_Sent = 0;
            m_Received = 0;
            m_Waiting.Clear();
        }

        public void RegisterHandler<TRequest>(Action<IRequest, TRequest> handler)
            => m_Handlers[typeof(TRequest)] = handler;

        public void RegisterHandler<TRequest, TResponse>(Func<IRequest, TRequest, TResponse> handler)
            => m_Handlers[typeof(TRequest)] = handler;

        public void RemoveHandler<TRequest>(Action<IRequest, TRequest> handler)
            => m_Handlers.Remove(typeof(TRequest));

        public void RemoveHandler<TRequest, TResponse>(Func<IRequest, TRequest, TResponse> handler)
            => m_Handlers.Remove(typeof(TRequest));

        public void Send(IResponse response, IRequest request)
        {
            if (request.AcceptedBy is null)
                throw new InvalidOperationException($"Attempted to respond to an invalid request.");

            if (request.AcceptedBy is INetworkController { Type: ControllerType.Client } client)
                client.Send(response);
            else
            {
                if (request.AcceptedBy is INetworkPeer peer)
                    peer.Send(response);
                else
                    throw new InvalidOperationException($"Invalid network object.");
            }

            m_Sent++;
        }

        public void Send(object response, IRequest request, bool isSuccess)
            => Send(new ResponseBase(isSuccess, request.Id, response), request);

        public void Send(IRequest request, Action<IResponse> callback)
        {
            m_Waiting[request.Id] = callback;

            this.ExecuteIfClient(client => client.Send(request));
            this.ExecuteIfServerPeer(peer => peer.Send(request));
        }

        public void Send(object request, Action<IResponse> callback)
            => Send(new RequestBase(Generator.Instance.GetString(), request), callback);

        public bool Accepts(object data)
            => data != null && (data is IRequest || data is IResponse);

        public bool Process(object data)
        {
            m_Received++;

            if (data is IResponse response)
            {
                if (m_Waiting.TryGetValue(response.RequestId, out var callback))
                    callback.DynamicInvoke(response);

                m_Waiting.Remove(response.RequestId);
                return true;
            }

            if (data is IRequest request)
            {
                var type = request.Request.GetType();

                request.Accept(Controller);

                if (m_Handlers.TryGetValue(type, out var handler))
                {
                    var handlerType = handler.GetType();

                    if (handlerType.FullName.StartsWith("System.Func"))
                    {
                        var handlerResult = handler.DynamicInvoke(request, request.Request);

                        if (handlerResult != null)
                        {
                            if (handlerResult is IResponse handlerResponse)
                            {
                                Send(handlerResponse, request);
                                return true;
                            }

                            Send(handlerResult, request, true);
                            return true;
                        }
                        else
                        {
                            Send(handlerResult, request, true);
                            return true;
                        }
                    }
                    else
                    {
                        handler.DynamicInvoke(request, request.Request);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}