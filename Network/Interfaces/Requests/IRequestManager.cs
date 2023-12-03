using Network.Interfaces.Features;
using Network.Interfaces.Transporting;

using System;

namespace Network.Interfaces.Requests
{
    public interface IRequestManager : IFeature
    {
        public event Action<IRequest> OnRequest;
        public event Action<IRequest> OnRequested;
        public event Action<IResponse> OnResponse;
        public event Action<IRequest, IResponse> OnResponded;

        void CreateHandler<T, TResponse>(Func<IRequest, T, TResponse> handler)
            where T : IMessage
            where TResponse : IMessage;

        void CreateHandler<T>(Action<IRequest, T> handler)
            where T : IMessage;

        void RemoveHandler<T>(Action<IRequest, T> handler)
            where T : IMessage;

        void RemoveHandler<T, TResponse>(Func<IRequest, T, TResponse> handler)
            where T : IMessage
            where TResponse: IMessage;

        IRequest Request<T, TResponse>(T request, byte timeout, Action<IResponse, TResponse> responseHandler)
            where T : IMessage
            where TResponse : IMessage;

        IResponse Respond<T>(IRequest request, T response, ResponseStatus status)
            where T : IMessage;
    }
}
