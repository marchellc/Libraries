using System;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;

namespace Networking.Kcp
{
    public static class KcpCommon
    {
        public static bool ResolveHostname(string hostname, out IPAddress[] addresses)
        {
            try
            {
                addresses = Dns.GetHostAddresses(hostname);
                return addresses.Length >= 1;
            }
            catch (SocketException exception)
            {
                KcpLog.Info($"[KCP] Failed to resolve host: {hostname} reason: {exception}");
                addresses = null;
                return false;
            }
        }

        public static void ConfigureSocketBuffers(Socket socket, int recvBufferSize, int sendBufferSize)
        {
            int initialReceive = socket.ReceiveBufferSize;
            int initialSend    = socket.SendBufferSize;

            try
            {
                socket.ReceiveBufferSize = recvBufferSize;
                socket.SendBufferSize = sendBufferSize;
            }
            catch (SocketException)
            {
                KcpLog.Warning($"[KCP] failed to set Socket RecvBufSize = {recvBufferSize} SendBufSize = {sendBufferSize}");
            }

            KcpLog.Info($"[KCP] RecvBuf = {initialReceive}=>{socket.ReceiveBufferSize} ({socket.ReceiveBufferSize / initialReceive}x) SendBuf = {initialSend}=>{socket.SendBufferSize} ({socket.SendBufferSize / initialSend}x)");
        }

        public static int ConnectionHash(EndPoint endPoint) =>
            endPoint.GetHashCode();

        static readonly RNGCryptoServiceProvider cryptoRandom = new RNGCryptoServiceProvider();
        static readonly byte[] cryptoRandomBuffer = new byte[4];

        public static uint GenerateCookie()
        {
            cryptoRandom.GetBytes(cryptoRandomBuffer);
            return BitConverter.ToUInt32(cryptoRandomBuffer, 0);
        }
    }
}
