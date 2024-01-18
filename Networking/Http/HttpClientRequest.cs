using Common.Extensions;
using Common.Pooling;

using System;
using System.Net.Http;

namespace Networking.Http
{
    public class HttpClientRequest : PoolableItem
    {
        public string Target
        {
            get;
            internal set;
        }

        public string ResponseValue
        {
            get;
            internal set;
        }

        public int Retries
        {
            get;
            internal set;
        }

        public HttpRequestMessage Request
        {
            get;
            internal set;
        }

        public HttpResponseMessage Response
        {
            get;
            internal set;
        }

        public HttpResponseData ResponseData
        {
            get;
            internal set;
        }

        public Action<HttpClientRequest> OnResponse
        {
            get;
            internal set;
        }

        public HttpClientRequest(string target, HttpRequestMessage msg, Action<HttpClientRequest> onResponse)
        {
            Target = target;
            Request = msg;
            OnResponse = onResponse;
            Retries = 0;
        }

        public void OnRequeued()
            => Retries++;

        public void OnReceived(HttpResponseMessage responseMessage, string response, HttpResponseData responseData)
        {
            Response = responseMessage;
            ResponseValue = response;
            ResponseData = responseData;

            OnResponse.Call(this);

            try
            {
                Request.Dispose();
                Request = null;

                Response.Dispose();
                Response = null;

                ResponseData = null;
                ResponseValue = null;
            }
            catch { }
        }

        public void RefreshRequest()
        {
            if (Retries > 0 && Request != null)
            {
                var newRequest = new HttpRequestMessage(Request.Method, Request.RequestUri);

                newRequest.Content = Request.Content;
                newRequest.Headers.Clear();

                Request.Headers.ForEach(pair => newRequest.Headers.TryAddWithoutValidation(pair.Key, pair.Value));

                Request.Dispose();
                Request = newRequest;
            }
        }
    }
}