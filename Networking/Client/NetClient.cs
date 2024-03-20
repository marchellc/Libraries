using Common.Extensions;
using Common.IO.Collections;
using Common.IO.Data;
using Common.Logging;
using Common.Utilities;

using Networking.Data;
using Networking.Enums;
using Networking.Interfaces;
using Networking.Kcp;

using System;
using System.Linq;
using System.Net;

namespace Networking.Client
{
    public class NetClient : 
        IClient
    {
        private static readonly KcpConfig config;

        private readonly LockedList<IComponent> components;
        private readonly LockedList<Type> preload;

        private IPEndPoint localIp;
        private IPEndPoint remoteIp;

        private NetListener listener;
        private NetSender sender;

        private KcpClient client;

        public readonly LogOutput Log;

        public IPEndPoint LocalAddress => localIp;
        public IPEndPoint RemoteAddress => remoteIp;

        public NetListener Listener => listener;

        public ISender Sender => sender;

        public ClientType Type => ClientType.Client;

        public bool IsRunning => client != null;
        public bool IsConnected => client != null && client.connected;

        public IComponent[] Components => components.ToArray();

        public event Action OnConnected;
        public event Action OnDisconnected;

        public static NetClient Instance { get; }

        static NetClient()
        {
            config = new KcpConfig(
                        NoDelay: true,
                        DualMode: false,
                        CongestionWindow: false,

                        Interval: 10,
                        Timeout: 5000,

                        SendWindowSize: KcpSocket.WND_SND * 1000,
                        ReceiveWindowSize: KcpSocket.WND_RCV * 1000,
                        MaxRetransmits: KcpSocket.DEADLINK * 2);

            Instance = new NetClient();
        }

        public NetClient()
        {
            components = new LockedList<IComponent>();
            preload = new LockedList<Type>();

            Log = new LogOutput("NetClient");
            Log.Setup();

            CodeUtils.WhileTrue(() => true, Tick, (int)config.Interval);
        }

        public void Start()
        { }

        public void Stop()
        {
            try
            {
                if (client != null)
                {
                    if (client.connected)
                    {
                        Stop();
                        return;
                    }

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

            client = new KcpClient(
                OnServerConnected,
                OnServerData,
                OnServerDisconnected,
                OnServerError,

                config);

            remoteIp = target;
            NetClientConnector.ConnectIndefinitely(client, remoteIp);
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
                Log.Verbose($"Sending {data.Length} bytes");
                client.Send(data.ToSegment(), KcpChannel.Reliable);
            }
            catch (Exception ex)
            {
                Log.Error($"KCP failed to send data to server!\n{ex}");
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

        private void OnServerConnected()
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

        private void OnServerDisconnected()
        {
            try
            {
                Log.Warn($"Disconnected from the server!");

                foreach (var component in components)
                    DestroyComponent(component, false);

                sender = null;
                listener = null;

                components.Clear();

                Log.Info("Attempting to reconnect ..");
                NetClientConnector.ConnectIndefinitely(client, remoteIp);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle server disconnection!\n{ex}");
            }
        }

        private void OnServerError(KcpErrorCode errorCode, string msg)
        {
            Log.Error($"Caught an error ({errorCode}): {msg ?? "no message"}");
        }

        private void OnServerData(ArraySegment<byte> data, KcpChannel channel)
        {
            Log.Verbose($"Received {data.Count} @ {channel}");
            DataReader.Read(data.ToArray(), reader =>
            {
                try
                {
                    var msg = reader.ReadObject();

                    if (msg is null)
                    {
                        Log.Warn($"Received a null message.");
                        return;
                    }

                    if (msg is not IData data)
                    {
                        Log.Warn($"Received an unknown message type: {msg.GetType().FullName}");
                        return;
                    }

                    try
                    {
                        var handled = false;

                        foreach (var component in components)
                        {
                            try
                            {
                                if (component is ITarget target && target.TryProcess(data))
                                {
                                    handled = true;
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Component '{component.GetType().FullName}' failed to handle message {msg.GetType().FullName}:\n{ex}");
                            }
                        }

                        if (!handled)
                            Log.Warn($"There aren't any valid data handlers present for message '{msg.GetType().FullName}'");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to handle message {msg.GetType().FullName}:\n{ex}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occured while reading incoming data!\n{ex}");
                }
            });
        }

        private void Tick()
        {
            if (client is null)
                return;

            try
            {
                client.Tick();
            }
            catch { }
        }
    }
}