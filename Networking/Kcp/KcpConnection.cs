using System;
using System.Net;

namespace Networking.Kcp
{
    public class KcpConnection : KcpPeer
    {
        public readonly EndPoint remoteEndPoint;

        protected readonly Action<KcpConnection> OnConnectedCallback;
        protected readonly Action<ArraySegment<byte>, KcpChannel> OnDataCallback;
        protected readonly Action OnDisconnectedCallback;
        protected readonly Action<KcpErrorCode, string> OnErrorCallback;
        protected readonly Action<ArraySegment<byte>> RawSendCallback;

        public KcpConnection(
            Action<KcpConnection> OnConnected,
            Action<ArraySegment<byte>, KcpChannel> OnData,
            Action OnDisconnected,
            Action<KcpErrorCode, string> OnError,
            Action<ArraySegment<byte>> OnRawSend,
            KcpConfig config,
            uint cookie,
            EndPoint remoteEndPoint)
                : base(config, cookie)
        {
            OnConnectedCallback = OnConnected;
            OnDataCallback = OnData;
            OnDisconnectedCallback = OnDisconnected;
            OnErrorCallback = OnError;
            RawSendCallback = OnRawSend;

            this.remoteEndPoint = remoteEndPoint;
        }

        protected override void OnAuthenticated()
        {
            SendHello();
            OnConnectedCallback(this);
        }

        protected override void OnData(ArraySegment<byte> message, KcpChannel channel) =>
            OnDataCallback(message, channel);

        protected override void OnDisconnected() =>
            OnDisconnectedCallback();

        protected override void OnError(KcpErrorCode error, string message) =>
            OnErrorCallback(error, message);

        protected override void RawSend(ArraySegment<byte> data) =>
            RawSendCallback(data);

        public void RawInput(ArraySegment<byte> segment)
        {
            if (segment.Count <= 5) 
                return;

            byte channel = segment.Array[segment.Offset + 0];

            KcpUtils.Decode32U(segment.Array, segment.Offset + 1, out uint messageCookie);

            if (state == KcpState.Authenticated)
            {
                if (messageCookie != cookie)
                {
                    KcpLog.Warning($"[KCP] ServerConnection: dropped message with invalid cookie: {messageCookie} expected: {cookie} state: {state}");
                    return;
                }
            }

            ArraySegment<byte> message = new ArraySegment<byte>(segment.Array, segment.Offset + 1+4, segment.Count - 1-4);

            switch (channel)
            {
                case (byte)KcpChannel.Reliable:
                    {
                        OnRawInputReliable(message);
                        break;
                    }
                case (byte)KcpChannel.Unreliable:
                    {
                        OnRawInputUnreliable(message);
                        break;
                    }
                default:
                    {
                        KcpLog.Warning($"[KCP] ServerConnection: invalid channel header: {channel}, likely internet noise");
                        break;
                    }
            }
        }
    }
}
