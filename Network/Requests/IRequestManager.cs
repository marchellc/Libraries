using Network.Data;
using Network.Features;

using System;

namespace Network.Requests
{
    public interface IRequestManager : INetworkFeature, IDataTarget
    {
        int Waiting { get; }
        int Sent { get; }
        int Received { get; }

        void Send(IResponse response, IRequest request);
        void Send(object response, IRequest request, bool isSuccess);

        void Send(IRequest request, Action<IResponse> callback);
        void Send(object request, Action<IResponse> callback);

        void RegisterHandler<TRequest>(Action<IRequest, TRequest> handler);
        void RegisterHandler<TRequest, TResponse>(Func<IRequest, TRequest, TResponse> handler);

        void RemoveHandler<TRequest>(Action<IRequest, TRequest> handler);
        void RemoveHandler<TRequest, TResponse>(Func<IRequest, TRequest, TResponse> handler);
    }
}