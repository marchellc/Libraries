using Common.Extensions;
using Common.IO.Collections;
using Common.IO.Data;
using Common.Logging;
using Common.Utilities;

using Networking.Data;
using Networking.Enums;
using Networking.Interfaces;
using Networking.Server;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Networking.Peer
{
    public class NetPeer : IPeer
    {
        private NetServer server;
        private NetSender sender;
        private NetListener listener;

        private IEnumerable<Type> preloads;

        private LockedList<IComponent> components;

        public readonly LogOutput Log;

        public IServer Server { get; }

        public Guid Id { get; }

        public IPEndPoint LocalAddress { get; }
        public IPEndPoint RemoteAddress { get; }

        public NetListener Listener => listener;

        public ISender Sender => sender;

        public ClientType Type => ClientType.Peer;

        public bool IsRunning => sender != null && server != null && server.IsRunning;
        public bool IsConnected => sender != null;

        public IComponent[] Components => components.ToArray();

        public NetPeer(NetServer server, Guid clientId, IPEndPoint remoteAddress, IPEndPoint localAddress, IEnumerable<Type> preloads)
        {
            this.server = server;
            this.preloads = preloads;

            Id = clientId;

            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;

            components = new LockedList<IComponent>();

            Log = new LogOutput($"Peer ({remoteAddress})");
            Log.Setup();
        }

        public void Add<T>() where T : IComponent
        {
            if (components.Any(comp => comp is T))
                return;

            var component = typeof(T).Construct<T>();
            InitializeComponent(component);
        }

        public T Get<T>() where T : IComponent
        {
            if (components.TryGetFirst(comp => comp is T, out var component))
                return (T)component;

            return default;
        }

        public bool Remove<T>() where T : IComponent
        {
            if (!components.TryGetFirst(comp => comp is T, out var component))
                return false;

            DestroyComponent(component);
            return true;
        }

        public void Send(byte[] data)
        {
            try
            {
                server.Send(Id, data);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to send data to clientId={Id}:\n{ex}");
            }
        }

        public void Start()
        {
            sender = new NetSender(this);
            listener = new NetListener();

            InitializeComponent(listener);

            if (preloads is null)
                return;

            foreach (var preloadType in preloads)
            {
                try
                {
                    var component = preloadType.Construct<IComponent>();
                    InitializeComponent(component);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to load preloaded component type '{preloadType.FullName}':\n{ex}");
                }
            }
        }

        public void Stop()
        {
            foreach (var component in components)
                DestroyComponent(component, false);

            components.Clear();
            components = null;

            listener.Clear();
            listener = null;

            preloads = null;

            sender = null;
            server = null;
        }

        public void Process(byte[] data)
        {
            DataReader.Read(data, reader =>
            {
                try
                {
                    var pack = reader.Read<NetPack>();

                    if (pack.ReadSize != pack.ValidSize)
                        Log.Warn($"Received a corrupted data pack! ({pack.ValidSize} / {pack.ReadSize})");

                    for (int i = 0; i < pack.ValidSize; i++)
                    {
                        try
                        {
                            var handled = false;

                            foreach (var component in components)
                            {
                                try
                                {
                                    if (component is ITarget target && target.TryProcess(pack.Pack[i]))
                                    {
                                        handled = true;
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($"Component '{component.GetType().FullName}' failed to handle message i={i} / {pack.ValidSize} ({pack.Pack[i].GetType().FullName}):\n{ex}");
                                }
                            }

                            if (!handled)
                                Log.Warn($"There aren't any valid data handlers present for message '{pack.Pack[i].GetType().FullName}'");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Failed to handle message i={i} / {pack.ValidSize} ({pack.Pack[i].GetType().FullName}):\n{ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to process incoming data!\n{ex}");
                }
            });
        }

        private void InitializeComponent(IComponent component)
        {
            if (!components.Contains(component))
                components.Add(component);

            try
            {
                if (!component.IsRunning)
                {
                    component.Client = this;
                    component.Sender = sender;
                    component.Listener = listener;
                    component.IsRunning = true;
                    component.Start();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to initialize component '{component.GetType().FullName}'!\n{ex}");
            }
        }

        private void DestroyComponent(IComponent component, bool remove = true)
        {
            try
            {
                if (component.IsRunning)
                {
                    component.Stop();
                    component.IsRunning = false;
                    component.Sender = null;
                    component.Listener = null;
                    component.Client = null;
                }

                if (remove)
                    components.Remove(component);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop component '{component.GetType().FullName}'!\n{ex}");
            }
        }
    }
}
