using Common.IO.Collections;
using Common.Logging;

using Network.Interfaces.Controllers;
using Network.Interfaces.Transporting;
using Network.Interfaces.Features;

using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Network.Tcp
{
    public class TcpPeer : IPeer
    {
        private volatile bool isConnected;

        private IController controller;

        private TcpTransport transport;
        private IPEndPoint target;
        private LogOutput log;

        private ConcurrentBag<IFeature> features;

        public int Id { get; }
        public int TickRate { get; set; }

        public bool IsConnected => isConnected;
        public bool IsRunning => isConnected;

        public bool IsManual { get; set; } = false;

        public ITransport Transport => transport;

        public IPEndPoint Target { get => target; set => throw new InvalidOperationException(); }

        public TcpPeer(int connectionId, IController controller, IPEndPoint endPoint)
        {
            this.Id = connectionId;
            this.controller = controller;
            this.target = endPoint;
            this.features = new ConcurrentBag<IFeature>();
            this.transport = new TcpTransport(this, controller);
            this.log = new LogOutput($"PEER :: {Id}");
        }

        public T AddFeature<T>() where T : IFeature, new()
        {
            log.Info($"Adding feature: {typeof(T).FullName}");

            foreach (var f in features)
            {
                log.Trace($"Scanning: {f.GetType().FullName}");

                if (f is T t)
                {
                    log.Trace($"Found");
                    return t;
                }
            }

            var feature = new T();

            features.Add(feature);

            if (!feature.IsRunning)
                feature.Start(this);

            log.Debug($"Added feature '{typeof(T).FullName}'");

            return feature;
        }

        public T GetFeature<T>() where T : IFeature
        {
            for (int i = 0; i < features.Count; i++)
            {
                if (features.ElementAt(i) is T t)
                    return t;
            }

            return default;
        }

        public void RemoveFeature<T>() where T : IFeature
        {
            foreach (var f in features)
            {
                if (f is T)
                    f.Stop();
            }

            features = new ConcurrentBag<IFeature>(features.Where(f => f is not T));
        }

        public void Start()
        {
            if (isConnected)
                return;

            isConnected = true;

            transport.Initialize();
        }

        public void Stop()
        {
            isConnected = false;

            for (int i = 0; i < features.Count; i++)
                features.ElementAt(i).Stop();

            features = null;

            target = null;

            transport.Shutdown();
            transport = null;
        }

        public void Disconnect()
        {
            if (controller is IClient client)
                client.Stop();
            else if (controller is IServer server)
                server.Disconnect(Id);
        }

        public void Tick() { }

        public Type[] GetFeatures()
            => features.Select(f => f.GetType()).ToArray();
    }
}