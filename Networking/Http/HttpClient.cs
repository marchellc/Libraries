using Common.Logging;
using Common.Utilities;
using Common.Extensions;
using Common;

using System;
using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Collections.Generic;

namespace Networking.Http
{
    public class HttpClient
    {
        public System.Net.Http.HttpClient Client { get; }
        public System.Net.Http.HttpClientHandler ClientHandler { get; }

        public ConcurrentQueue<HttpClientRequest> Requests { get; }

        public LogOutput Log { get; set; }

        public int MaxRetries { get; set; }

        public bool IsRunning { get; set; }
        public bool IsLogging { get; set; }

        public HttpClient()
        {
            MaxRetries = 5;

            Log = new LogOutput("Http Client").Setup();

            ClientHandler = new System.Net.Http.HttpClientHandler();

            ClientHandler.UseDefaultCredentials = true;
            ClientHandler.UseProxy = false;

            Client = new System.Net.Http.HttpClient(ClientHandler);

            Requests = new ConcurrentQueue<HttpClientRequest>();
        }

        public void Get<TContent>(string host, Action<HttpClientRequest> callback, TContent content, params KeyValuePair<string, string>[] headers)
            => Make(host, HttpMethod.Get, new StringContent(JsonSerializer.Serialize(content)), callback, headers);

        public void Get(string host, Action<HttpClientRequest> callback, string content, params KeyValuePair<string, string>[] headers)
            => Make(host, HttpMethod.Get, string.IsNullOrWhiteSpace(content) ? null : new StringContent(content), callback, headers);

        public void Get(string host, Action<HttpClientRequest> callback, HttpContent content, params KeyValuePair<string, string>[] headers)
            => Make(host, HttpMethod.Get, content, callback, headers);

        public void Post(string host, Action<HttpClientRequest> callback, HttpContent content, params KeyValuePair<string, string>[] headers)
            => Make(host, HttpMethod.Post, content, callback, headers);

        public void Post(string host, Action<HttpClientRequest> callback, string content, params KeyValuePair<string, string>[] headers)
            => Make(host, HttpMethod.Post, string.IsNullOrWhiteSpace(content) ? new StringContent(content) : null, callback, headers);

        public void Post<TContent>(string host, Action<HttpClientRequest> callback, TContent content, params KeyValuePair<string, string>[] headers)
            => Make(host, HttpMethod.Post, new StringContent(JsonSerializer.Serialize(content)), callback, headers);

        public void Make(string host, HttpMethod method, HttpContent content, Action<HttpClientRequest> callback, params KeyValuePair<string, string>[] headers)
        {
            var request = new HttpRequestMessage(method, host);

            for (int i = 0; i < headers.Length; i++)
                request.Headers.Add(headers[i].Key, headers[i].Value);

            request.Headers.Add("User-Agent", $"{ModuleInitializer.GetAppName()}/1.0.0");

            if (content != null)
                request.Content = content;

            Enqueue(host, callback, request);
        }

        public void Enqueue(string host, Action<HttpClientRequest> callback, HttpRequestMessage requestMessage)
            => Requests.Enqueue(new HttpClientRequest(host, requestMessage, callback));

        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException($"The client is already running.");

            IsRunning = true;

            CodeUtils.WhileTrue(() => IsRunning, Process, 10);
        }

        public void Stop()
        {
            if (!IsRunning)
                throw new InvalidOperationException($"The client is not running.");

            IsRunning = false;

            CodeUtils.Delay(() =>
            {
                Requests.Clear();
            }, 5);
        }

        private void OnFailed(HttpClientRequest clientReq, Exception exception)
        {
            if (IsLogging)
                Log?.Error($"Request to '{clientReq.Target}' failed with error:\n{exception}");

            if (MaxRetries > 0 && clientReq.Retries >= MaxRetries)
                return;

            Requests.Enqueue(clientReq);

            clientReq.OnRequeued();
        }

        private void OnFailed(HttpClientRequest clientReq, HttpStatusCode statusCode, string reason)
        {
            if (IsLogging)
                Log?.Error($"Request to '{clientReq.Target}' failed with code '{statusCode}' ({(int)statusCode}), info: '{reason ?? "no info"}'");

            if (MaxRetries > 0 && clientReq.Retries >= MaxRetries)
                return;

            Requests.Enqueue(clientReq);

            clientReq.OnRequeued();
        }

        private async void Process()
        {
            if (Requests.TryDequeue(out var clientReq))
            {
                try
                {
                    clientReq.RefreshRequest();

                    try
                    {
                        var response = await Client.SendAsync(clientReq.Request);

                        if (!response.IsSuccessStatusCode)
                        {
                            OnFailed(clientReq, response.StatusCode, response.ReasonPhrase);
                            return;
                        }

                        HttpResponseData responseData = null;

                        var responseValue = await response.Content.ReadAsStringAsync();

                        try
                        {
                            responseData = JsonSerializer.Deserialize<HttpResponseData>(responseValue);

                            if (responseData != null)
                            {
                                if (!responseData.Success)
                                {
                                    OnFailed(clientReq, (HttpStatusCode)responseData.Code, responseData.Data);
                                    return;
                                }

                                clientReq.OnReceived(response, responseData.Data, responseData);
                                return;
                            }
                        }
                        catch { }

                        clientReq.OnReceived(response, responseValue, responseData);
                    }
                    catch (Exception ex)
                    {
                        OnFailed(clientReq, ex);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error while processing request to '{clientReq.Target}':\n{ex}");
                }
            }
        }
    }
}