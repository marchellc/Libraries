using Common.Extensions;
using Common.IO.Collections;
using Common.IO.Data;
using Common.Pooling.Pools;
using Common.Logging;
using Common.Utilities;

using Networking.Enums;
using Networking.Interfaces;
using Networking.Internal.Messages;

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Networking
{
    public class NetworkClient
    {
        private volatile UdpClient client;
        private volatile IPEndPoint remote;

        private LockedDictionary<Type, List<Delegate>> listeners;

        private ConcurrentQueue<NetworkMessage> waitingMessages;
        private ConcurrentQueue<INetworkMessage> batch;

        private DateTime lastSend;
        private DateTime heartbeat;

        private bool isSending;

        private readonly INetworkMessage[] messageBuffer;

        public event Action OnStopped;
        public event Action OnStarted;

        public event Action OnConnected;

        public event Action<DisconnectReason> OnDisconnected;

        public event Action<INetworkMessage[]> OnBatch;
        public event Action<INetworkMessage> OnMessage;

        public LogOutput Log { get; }

        public static NetworkClient Instance { get; } = new NetworkClient();

        public bool IsConnected
        {
            get => client != null && client.Client != null && client.Client.Connected;
        }

        public UdpClient Client
        {
            get => client;
        }

        public Socket Socket
        {
            get => client?.Client;
        }

        public IPEndPoint Remote
        {
            get => remote;
        }

        public NetworkClient()
        {
            messageBuffer = new INetworkMessage[byte.MaxValue];

            Log = new LogOutput("Network Client");
            Log.Setup();
        }

        public void Connect(string ip, int port, Action<ConnectionResult> callback = null)
        {
            if (client != null)
                Disconnect();

            CodeUtils.OnThread(() =>
            {
                client = new UdpClient();

                client.DontFragment = true;
                client.MulticastLoopback = true;

                client.EnableBroadcast = false;
                client.ExclusiveAddressUse = false;

                listeners = new LockedDictionary<Type, List<Delegate>>();

                waitingMessages = new ConcurrentQueue<NetworkMessage>();
                batch = new ConcurrentQueue<INetworkMessage>();

                try
                {
                    OnStarted.Call();

                    Log.Info($"Connecting to: {ip}:{port}");

                    client.Connect(ip, port);

                    remote = (IPEndPoint)client.Client.RemoteEndPoint;

                    NetworkTransport.ClientReceive(client, new NetworkTransport.NetworkClientTransportEvents
                    {
                        Client = client,

                        OnStarted = () =>
                        {
                            callback.Call(ConnectionResult.Success);
                            OnConnected.Call();
                        },

                        GetRemote = () => remote,
                        SetRemote = sender => remote = sender,

                        OnData = OnData,
                        OnStopped = OnTransportStopped
                    }, true);

                    CodeUtils.WhileTrue(() => IsConnected, OnUpdate, 100);

                    Log.Info($"Connected to: {remote}");
                }
                catch (Exception ex)
                {
                    if (ex is SocketException socketException 
                        && (socketException.SocketErrorCode is SocketError.TimedOut || socketException.SocketErrorCode is SocketError.ConnectionRefused
                            || socketException.SocketErrorCode is SocketError.HostNotFound || socketException.SocketErrorCode is SocketError.HostUnreachable))
                    {
                        Log.Warn($"Destination client ({ip}:{port}) has rejected connection: {socketException.SocketErrorCode}");
                        callback.Call(ConnectionResult.Rejected);
                        return;
                    }
                    else
                    {
                        Log.Error($"An error occured while connecting: {ex}");
                        callback.Call(ConnectionResult.Unknown);
                        return;
                    }
                }
            });
        }

        public void Disconnect(DisconnectReason reason = DisconnectReason.Unknown)
        {
            if (client != null)
            {
                Log.Info("Disconnecting ..");

                try
                {
                    client.Close();
                }
                catch (Exception ex)
                {
                    Log.Error($"An error has occured while attempting to disconnect: {ex}");
                }

                OnDisconnected.Call(reason);

                listeners.Clear();
                listeners = null;

                waitingMessages.Clear();
                waitingMessages = null;

                batch.Clear();
                batch = null;

                client.Dispose();
                client = null;

                lastSend = default;
                heartbeat = default;
                isSending = false;
                remote = null;

                Array.Clear(messageBuffer, 0, messageBuffer.Length);

                OnStopped.Call();

                Log.Warn($"Disconnected: {reason}");
            }
        }

        public void Batch<T>(T message = default, bool isPriority = false) where T : INetworkMessage
        {
            while (isSending)
                continue;

            batch.Enqueue(message);

            if (isPriority || batch.Count >= 8 || ((DateTime.Now - lastSend).TotalMilliseconds >= 500))
            {
                InternalSendBatch();
                return;
            }
        }

        public bool Listen<T>(Action<T> listener)
        {
            if (!listeners.TryGetValue(typeof(T), out var messageListeners))
                listeners[typeof(T)] = messageListeners = new List<Delegate>();

            if (messageListeners.Any(msgListener => TypeInstanceComparer.IsEqualTo(msgListener.Target, listener.Target) && msgListener.Method == listener.Method))
                return false;

            messageListeners.Add(listener);
            return true;
        }

        public bool StopListen<T>(Action<T> listener)
        {
            if (!listeners.TryGetValue(typeof(T), out var messageListeners) || messageListeners.Count < 1)
                return false;

            return messageListeners.RemoveAll(msgListener => TypeInstanceComparer.IsEqualTo(msgListener.Target, listener.Target) && msgListener.Method == listener.Method) > 0;
        }

        private void InternalSendBatch()
        {
            if (isSending)
                return;

            isSending = true;

            try
            {
                if (batch.Count < 1)
                    return;

                Log.Verbose($"Batch - sending {batch.Count} messages");

                var writer = PoolablePool<DataWriter>.Shared.Rent();

                while (batch.TryDequeue(out var networkMessage))
                {
                    writer.WriteUShort(networkMessage.GetType().ToShortCode());
                    writer.WriteAnonymous(networkMessage);
                }

                var data = ArrayPool<byte>.Shared.Rent(writer.DataSize);

                Array.Copy(writer.Data, data, writer.DataSize);

                PoolablePool<DataWriter>.Shared.Return(writer);

                Log.Verbose($"Sending {data.Length} bytes");

                try
                {
                    var sentBytes = client.Send(data, data.Length);

                    if (sentBytes != data.Length)
                        Log.Warn($"Sent less bytes than requested! ({sentBytes} / {data.Length})");
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occured while sending the current batch!\n{ex}");
                }

                ArrayPool<byte>.Shared.Return(data);
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured while sending the current batch!\n{ex}");
            }

            isSending = false;
            lastSend = DateTime.Now;
        }

        private void OnUpdate()
        {
            if (DateTime.Now > heartbeat)
            {
                Batch<HeartbeatMessage>(default, true);
                heartbeat = DateTime.Now.AddSeconds(1);
            }

            while (waitingMessages.TryDequeue(out var networkMessage))
            {
                if (listeners.TryGetValue(networkMessage.Message.GetType(), out var messageListeners) && messageListeners.Count > 0)
                {
                    foreach (var listener in messageListeners)
                        listener.Method.Call(listener.Target, networkMessage.Message);
                }
                else
                {
                    if ((DateTime.Now - networkMessage.Time).TotalSeconds > 30)
                        continue;

                    waitingMessages.Enqueue(networkMessage);
                }
            }
        }

        private void OnData(byte[] data)
        {
            var reader = PoolablePool<DataReader>.Shared.Rent();

            reader.Set(data);
            reader.Position = 0;

            var size = reader.ReadByte();

            if (size < 1)
            {
                PoolablePool<DataReader>.Shared.Return(reader);
                return;
            }

            var hasError = false;

            reader.ReadIntoArrayCustom(messageBuffer, 0, () =>
            {
                if (hasError) return default;

                var messageId = reader.ReadUShort();

                if (!TypeSearch.TryFind(messageId, out var messageType))
                {
                    Log.Warn($"Received an unregistered message ID: {messageId}");
                    hasError = true;
                    return default;
                }

                return reader.ReadAnonymous<INetworkMessage>(messageType);
            });

            if (hasError)
            {
                PoolablePool<DataReader>.Shared.Return(reader);
                return;
            }

            OnBatch.Call(messageBuffer);

            for (byte i = 0; i < size; i++)
            {
                try
                {
                    var messageObj = messageBuffer[i];

                    OnMessage.Call(messageObj);

                    if (listeners.TryGetValue(messageObj.GetType(), out var messageListeners) && messageListeners.Count > 0)
                    {
                        foreach (var listener in messageListeners)
                            listener.Method.Call(listener.Target, messageObj);
                    }
                    else
                    {
                        waitingMessages.Enqueue(new NetworkMessage
                        {
                            Time = DateTime.Now,
                            Message = messageObj,
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"An error ocurred while processing message '{messageBuffer[i].GetType().Name}': {ex}");
                    break;
                }
            }

            PoolablePool<DataReader>.Shared.Return(reader);
        }

        private void OnTransportStopped(TransportError error, object result)
        {
            Log.Warn($"Network Transport has stopped! ({error}:{result?.ToString() ?? "null"})");
            Disconnect(DisconnectReason.TransportError);
        }
    }
}