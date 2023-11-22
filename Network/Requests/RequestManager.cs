using Common.Pooling;
using Common.Reflection;

using Network.Data;
using Network.Logging;

using System;
using System.Collections.Generic;
using System.Threading;

namespace Network.Requests
{
    public class RequestManager : NetworkFeature
    {
        private readonly Dictionary<Type, Delegate> requestHandlers = new Dictionary<Type, Delegate>();

        private readonly List<ResponseMessage> responses = new List<ResponseMessage>();
        private readonly List<ResponseHandler> responseHandlers = new List<ResponseHandler>();

        private readonly object responseLock = new object();

        private byte curId = 0;
        private Timer timer;

        public event Action<RequestMessage> OnRequestReceived;
        public event Action<ResponseMessage> OnResponseReceived;
        public event Action<RequestMessage, ResponseHandler> OnRequestTimedOut;
        public event Action<RequestMessage, ResponseMessage> OnResponseSent;

        public override void Start()
        {
            base.Start();

            timer = new Timer(_ => UpdateResponses(), null, 1500, 50);
        }

        public override void Stop()
        {
            timer.Dispose();
            timer = null;

            requestHandlers.Clear();
            responseHandlers.Clear();
            responses.Clear();

            curId = 0;

            base.Stop();
        }

        public override void Receive(Message message)
        {
            base.Receive(message);

            if (message.Value is null)
                return;

            if (message.Value is RequestMessage requestMessage)
            {
                requestMessage.Manager = this;

                OnRequestReceived.Call(requestMessage);

                if (requestMessage.Object is null)
                    return;

                if (!requestHandlers.TryGetValue(requestMessage.Object.GetType(), out var handler))
                    return;

                object result = null;

                try
                {
                    result = handler.DynamicInvoke(new object[] { requestMessage.Object });
                }
                catch (Exception ex)
                {
                    NetworkLog.Add(NetworkLogLevel.Error, "REQUESTS", $"An exception occured while handling request '{requestMessage.Id}' in '{handler.Method.Name}':\n{ex}");
                    return;
                }

                var msg = new ResponseMessage(requestMessage.Id, result, requestMessage);

                Peer.Send(msg);

                OnResponseSent.Call(requestMessage, msg);
            }
            else if (message.Value is ResponseMessage responseMessage)
            {
                lock (responseLock)
                    responses.Add(responseMessage);

                OnResponseReceived.Call(responseMessage);
            }
        }

        public void Handle<TMessage>(Func<TMessage, object> handler) where TMessage : IWritable
            => requestHandlers[typeof(TMessage)] = handler;

        public RequestMessage Request<TMessage, TResponse>(TMessage message, Action<RequestMessage, ResponseMessage, TResponse> responseHandler, byte timeout = 0) 
            where TMessage : IWritable
            where TResponse : IReadable
        {
            if (curId >= byte.MaxValue)
                curId = 0;

            curId++;

            var req = new RequestMessage(curId, DateTime.Now, message);

            lock (responses)
                responseHandlers.Add(new ResponseHandler
                {
                    Id = curId,

                    Added = DateTime.Now,

                    Handler = responseHandler,
                    Request = req,
                    Timeout = timeout
                });

            Peer.Send(req);

            return req;
        }

        private void UpdateResponses()
        {
            lock (responseLock)
            {
                var removing = PoolExtensions.GetList<ResponseHandler>();

                for (int i = 0; i < responseHandlers.Count; i++)
                {
                    var handler = responseHandlers[i];
                    var responded = false;

                    for (int x = 0; x < responses.Count; x++)
                    {
                        if (responses[x].Id == handler.Id)
                        {
                            try
                            {
                                handler.Handler.DynamicInvoke(new object[] { handler.Request, responses[x], responses[x].Response });
                            }
                            catch (Exception ex)
                            {
                                NetworkLog.Add(NetworkLogLevel.Error, "REQUESTS", $"Failed while executing response to request ID={responses[x].Id} in '{handler.Handler.Method.Name}':\n{ex}");
                            }

                            responded = true;
                            break;
                        }
                    }

                    if (responded)
                    {
                        removing.Add(handler);
                        continue;
                    }

                    if (handler.Timeout > 0 && (DateTime.Now - handler.Added).TotalMinutes >= handler.Timeout)
                    {
                        try
                        {
                            handler.Handler.DynamicInvoke(new object[] 
                            {
                                handler.Request, 

                                new ResponseMessage
                                {
                                    Id = handler.Id,

                                    RequestReceived = DateTime.MinValue,
                                    RequestSent = handler.Request.Sent,

                                    Response = null,

                                    ResponseReceived = DateTime.MinValue,
                                    ResponseSent = DateTime.MinValue,

                                    Status = ResponseStatus.TimedOut
                                },

                                null
                            });

                            OnRequestTimedOut.Call(handler.Request, handler);
                        }
                        catch (Exception ex)
                        {
                            NetworkLog.Add(NetworkLogLevel.Error, "REQUESTS", $"Failed while executing timed-out response to request ID={handler.Id} in '{handler.Handler.Method.Name}':\n{ex}");
                        }

                        removing.Add(handler);
                        continue;
                    }
                }

                for (int y = 0; y < removing.Count; y++)
                    responseHandlers.Remove(removing[y]);

                removing.Return();
                removing = null;
            }
        }
    }
}