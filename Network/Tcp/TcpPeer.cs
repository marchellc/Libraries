using Common.IO.Collections;
using Common.Reflection;
using Network.Interfaces;
using System;
using System.Net;

namespace Network.Tcp
{
    public class TcpPeer : IPeer
    {
        private bool isConnected;

        private IController controller;

        private TcpTransport transport;
        private IPEndPoint target;

        private LockedList<IFeature> features;

        public event Action OnReady;

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
            this.features = new LockedList<IFeature>();
        }

        public T AddFeature<T>() where T : IFeature, new()
        {
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i] is T t)
                    return t;
            }

            var feature = new T();

            features.Add(feature);
            feature.Start(this);

            return feature;
        }

        public T GetFeature<T>() where T : IFeature
        {
            for (int i = 0; i < features.Count; i++)
            {
                if (features[i] is T t)
                    return t;
            }

            return default;
        }

        public void RemoveFeature<T>() where T : IFeature
        {
            var removed = features.RemoveRange(f => f is T);

            for (int i = 0; i < removed.Count; i++)
                removed[i].Stop();
        }

        public void Start()
        {
            isConnected = true;

            transport = new TcpTransport(this, controller);
            transport.Initialize();

            for (int i = 0; i < features.Count; i++)
                features[i].Start(this);

            OnReady.Call();
        }

        public void Stop()
        {
            isConnected = false;

            for (int i = 0; i < features.Count; i++)
                features[i].Stop();

            features.Clear();
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
    }
}