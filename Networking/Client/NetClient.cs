using Common.Extensions;
using Common.IO.Collections;
using Common.IO.Data;
using Common.Logging;

using Networking.Data;

using Networking.Enums;
using Networking.Interfaces;

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WatsonTcp;

namespace Networking.Client
{
    public class NetClient : 
        IClient
    {
        private readonly LockedList<IComponent> components;
        private readonly LockedList<Type> preload;

        private IPEndPoint localIp;
        private IPEndPoint remoteIp;

        private NetListener listener;
        private NetSender sender;

        private WatsonTcpClient client;

        public readonly LogOutput Log;

        public IPEndPoint LocalAddress => localIp;
        public IPEndPoint RemoteAddress => remoteIp;

        public NetListener Listener => listener;

        public ISender Sender => sender;

        public ClientType Type => ClientType.Client;

        public bool IsRunning => client != null;
        public bool IsConnected => client != null && client.Connected;

        public IComponent[] Components => components.ToArray();

        public event Action OnConnected;
        public event Action OnDisconnected;

        public static NetClient Instance { get; } = new NetClient();

        public NetClient()
        {
            components = new LockedList<IComponent>();
            preload = new LockedList<Type>();

            Log = new LogOutput("NetClient");
            Log.Setup();
        }

        public void Start()
        { }

        public void Stop()
        {
            try
            {
                if (client != null)
                {
                    if (client.Connected)
                    {
                        Stop();
                        return;
                    }

                    client.Events.MessageReceived -= OnServerData;
                    client.Events.ServerConnected -= OnServerConnected;
                    client.Events.ServerDisconnected -= OnServerDisconnected;

                    client.Dispose();
                    client = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop the active Telepathy client!\n{ex}");
            }
        }

        public void Connect(IPEndPoint target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            if (IsConnected)
            {
                Log.Warn($"Attempting to connect to another IP while connected! Stopping the current client ..");
                Stop();
            }

            Log.Info($"Connecting to: {target}");

            if (client is null)
            {
                client = new WatsonTcpClient(target.Address.ToString(), target.Port);

                client.Settings.NoDelay = true;

                client.Keepalive.EnableTcpKeepAlives = true;
                client.Keepalive.TcpKeepAliveInterval = 1;
                client.Keepalive.TcpKeepAliveTime = 1;

                client.Events.MessageReceived += OnServerData;
                client.Events.ServerConnected += OnServerConnected;
                client.Events.ServerDisconnected += OnServerDisconnected;
            }

            remoteIp = target;

            NetClientConnector.ConnectIndefinitely(client);
        }

        public T Get<T>() where T : IComponent
        {
            if (components.TryGetFirst(comp => comp is T, out var found))
                return (T)found;

            return default;
        }

        public void Add<T>() where T : IComponent
        {
            if (!IsConnected)
            {
                if (preload.Contains(typeof(T)))
                {
                    preload.Remove(typeof(T));
                    return;
                }

                preload.Add(typeof(T));
                return;
            }

            if (components.Any(comp => comp is T))
                return;

            var component = typeof(T).Construct<T>();
            InitializeComponent(component);
        }

        public bool Remove<T>() where T : IComponent
        {
            var component = Get<T>();

            if (component is null)
                return false;

            DestroyComponent(component);
            return true;
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
            {
                Log.Warn($"Attempted to send data over an inactive socket.");
                return;
            }

            try
            {
                Task.Run(async () => await client.SendAsync(data));
            }
            catch (Exception ex)
            {
                Log.Error($"TCP failed to send data to server!\n{ex}");
            }
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

        private void OnServerConnected(object _, ConnectionEventArgs ev)
        {
            try
            {
                localIp = new IPEndPoint(IPAddress.Loopback, 0);

                Log.Info($"Connection to server established on '{remoteIp}'!");

                sender = new NetSender(this);
                listener = new NetListener();

                InitializeComponent(listener);

                foreach (var preloadType in preload)
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

                OnConnected.Call();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle server connection!\n{ex}");
            }
        }

        private void OnServerDisconnected(object _, DisconnectionEventArgs ev)
        {
            try
            {
                Log.Warn($"Disconnected from the server!");

                foreach (var component in components)
                    DestroyComponent(component, false);

                sender = null;
                listener = null;

                components.Clear();

                if (ev.Reason != DisconnectReason.Shutdown)
                {
                    Log.Info("Attempting to reconnect ..");

                    NetClientConnector.ConnectIndefinitely(client);
                    return;
                }

                preload.Clear();

                localIp = null;
                remoteIp = null;

                Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle server disconnection!\n{ex}");
            }
        }

        private void OnServerData(object _, MessageReceivedEventArgs ev)
        {
            DataReader.Read(ev.Data, reader =>
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
                    Log.Error($"An error occured while reading incoming data!\n{ex}");
                }
            });
        }
    }
}