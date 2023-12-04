using Network.Interfaces.Requests;
using Network.Interfaces.Transporting;
using Network.Features;
using Network.Extensions;

using System;
using System.IO;
using System.Threading;

using Common.IO.Collections;
using Common.Reflection;
using Common.Extensions;

using System.Collections.Generic;

namespace Network.Requests
{
    public class RequestManager : Feature, IRequestManager
    {
        public const byte REQ_BYTE = 10;
        public const byte RES_BYTE = 11;

        private LockedDictionary<byte, RequestCache> requests;
        private LockedDictionary<Type, Delegate> handlers;

        private HashSet<IResponse> responses;
        private object responseLock = new object();

        private Timer timer;

        private byte requestId;

        public event Action<IRequest> OnRequest;
        public event Action<IRequest> OnRequested;
        public event Action<IResponse> OnResponse;
        public event Action<IRequest, IResponse> OnResponded;

        public override void OnStarted()
        {
            this.requests = new LockedDictionary<byte, RequestCache>(byte.MaxValue);
            this.responses = new HashSet<IResponse>(byte.MaxValue);
            this.handlers = new LockedDictionary<Type, Delegate>();
            this.timer = new Timer(UpdateResponses, null, 0, 200);
            this.requestId = 0;

            Transport.CreateHandler(REQ_BYTE, HandleRequest);
            Transport.CreateHandler(RES_BYTE, HandleResponse);

            Log.Info($"Started!");
        }

        public override void OnStopped()
        {
            Transport.RemoveHandler(REQ_BYTE, HandleRequest);
            Transport.RemoveHandler(RES_BYTE, HandleResponse);

            this.requests.Clear();
            this.handlers.Clear();
            this.responses.Clear();
            this.timer?.Dispose();
            this.requests = null;
            this.handlers = null;
            this.responses = null;
            this.timer = null;
            this.requestId = 0;

            Log.Info("Stopped!");
        }

        public void CreateHandler<T, TResponse>(Func<IRequest, T, TResponse> handler)
            where T : IMessage
            where TResponse : IMessage
        {
            handlers[typeof(T)] = handler;
        }

        public void CreateHandler<T>(Action<IRequest, T> handler) where T : IMessage
        {
            handlers[typeof(T)] = handler;
        }

        public void RemoveHandler<T>(Action<IRequest, T> handler) where T : IMessage
        {
            handlers.Remove(typeof(T));
        }

        public void RemoveHandler<T, TResponse>(Func<IRequest, T, TResponse> handler)
            where T : IMessage
            where TResponse : IMessage
        {
            handlers.Remove(typeof(T));
        }

        public IRequest Request<T, TResponse>(T request, byte timeout, Action<IResponse, TResponse> responseHandler)
            where T : IMessage
            where TResponse : IMessage
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            var requestId = GetNextId();
            var requestInfo = new Request(this, requestId, DateTime.Now, DateTime.MinValue, request);

            requests[requestId] = new RequestCache
            {
                Request = requestInfo,
                Requested = requestInfo.Sent,
                Timeout = timeout,
                ResponseHandler = responseHandler
            };

            OnRequested.Call(requestInfo);

            Transport.Send(REQ_BYTE, bw =>
            {
                bw.Write(requestInfo.Id);
                bw.WriteDate(requestInfo.Sent);
                bw.WriteObject(requestInfo.Object, Transport);
            });

            Log.Debug($"Sent request of ID {requestId} ({typeof(T).FullName})");

            return requestInfo;
        }

        public IResponse Respond<T>(IRequest request, T response, ResponseStatus status) where T : IMessage
        {
            var responseInfo = new Response(this, request, DateTime.Now, DateTime.MinValue, status, response);

            Transport.Send(RES_BYTE, bw =>
            {
                bw.Write(request.Id);
                bw.WriteDate(responseInfo.Sent);
                bw.Write((byte)responseInfo.Status);

                if (responseInfo.Status is ResponseStatus.Ok && response != null)
                    bw.WriteObject(response, Transport);
            });

            OnResponded.Call(request, responseInfo);

            request.OnResponded(responseInfo);

            Log.Debug($"Sent a response to request of ID '{request.Id}' ({typeof(T).FullName})");

            return responseInfo;
        }

        private byte GetNextId()
        {
            if (requestId >= byte.MaxValue)
                requestId = 0;

            return requestId++;
        }

        private void HandleResponse(BinaryReader br)
        {
            var requestId = br.ReadByte();

            if (!requests.TryGetValue(requestId, out var request))
            {
                Log.Warn($"Received a response for an unknown request ID: {requestId}");
                return;
            }

            var responseSent = br.ReadDate();
            var responseStatus = (ResponseStatus)br.ReadByte();

            Log.Debug($"Received response for request {requestId}: {responseStatus}");

            object response = null;

            if (responseStatus is ResponseStatus.Ok)
                response = br.ReadObject(Transport);

            var responseInfo = new Response(this,
                request.Request,

                responseSent,

                DateTime.Now,

                responseStatus,
                response);

            OnResponse.Call(responseInfo);

            lock (responseLock)
                responses.Add(responseInfo);
        }

        private void HandleRequest(BinaryReader br)
        {
            var request = new Request(this, 

                br.ReadByte(), 
                br.ReadDate(), 

                DateTime.Now, 

                br.ReadObject(Transport));

            OnRequest.Call(request);

            if (request.Object is null)
            {
                Log.Error($"Received a request with a null object");
                return;
            }

            var type = request.Object.GetType();

            Log.Debug($"Received request {request.Id} ({type.FullName})");

            if (!handlers.TryGetValue(type, out var handler))
            {
                Log.Error($"Received a request with no assigned handler: {type.FullName}");
                return;
            }

            var handlerType = handler.GetType();

            if (handlerType.FullName.Contains("Func"))
            {
                try
                {
                    var responseObject = handler.DynamicInvoke(new object[] { request, request.Object });

                    if (responseObject is null || responseObject is not IMessage)
                    {
                        Log.Info($"Request Handler returned a null or not an IMessage, sending failed response.");

                        Transport.Send(RES_BYTE, bw =>
                        {
                            bw.Write(request.Id);
                            bw.WriteDate(DateTime.Now);
                            bw.Write((byte)ResponseStatus.Failed);
                        });

                        OnResponded.Call(request, new Response(this, request, DateTime.Now, DateTime.MinValue, ResponseStatus.Failed, null));
                    }
                    else
                    {
                        Log.Info($"Request Handler returned a valid value, sending Ok response.");

                        Transport.Send(RES_BYTE, bw =>
                        {
                            bw.Write(request.Id);
                            bw.WriteDate(request.Received);
                            bw.Write((byte)ResponseStatus.Ok);
                            bw.WriteObject(responseObject, Transport);
                        });

                        OnResponded.Call(request, new Response(this, request, DateTime.Now, DateTime.MinValue, ResponseStatus.Ok, responseObject));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to invoke request handler '{handlerType.FullName}' ({handler.Method.Name}):\n{ex}");
                }
            }
            else
            {
                try
                {
                    handler.DynamicInvoke(new object[] { request, request.Object });
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to invoke request handler '{handlerType.FullName}' ({handler.Method.Name}):\n{ex}");
                }
            }
        }

        private void UpdateResponses(object _)
        {
            foreach (var request in requests)
            {
                if (request.Value.Request is null)
                {
                    Log.Warn($"Request cache contains a null request!");
                    continue;
                }

                if (request.Value.Timeout > 0 && (DateTime.Now - request.Value.Requested).Seconds >= request.Value.Timeout)
                {
                    lock (responseLock)
                        responses.Add(new Response(this, request.Value.Request, request.Value.Requested, DateTime.MinValue, ResponseStatus.TimedOut, null));

                    Log.Warn($"Request '{request.Value.Request.Id}' has timed out!");
                }
            }

            lock (responseLock)
            {
                foreach (var response in responses)
                {
                    if (response.Request is null)
                    {
                        Log.Warn($"Found a response with a null request!");
                        continue;
                    }

                    if (requests.TryGetValue(response.Request.Id, out var request))
                    {
                        try
                        {
                            OnResponse.Call(response);

                            request.ResponseHandler.DynamicInvoke(new object[] { response, response.Object });
                            response.OnHandled();

                            Log.Debug($"Handled response '{response.Request.Id}'");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to handle request '{response.Request.Id}' response:\n{ex}");
                        }
                    }
                    else
                    {
                        Log.Warn($"Received a response with a missing request! ({response.Request.Id})");
                    }

                    requests.Remove(response.Request.Id);
                }

                responses.RemoveWhere(r => r.IsHandled);
            }
        }
    }
}
