using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Networking.Kcp
{
    public static class KcpExtensions
    {
        public static string ToHexString(this ArraySegment<byte> segment) =>
            BitConverter.ToString(segment.Array, segment.Offset, segment.Count);

        public static bool SendToNonBlocking(this Socket socket, ArraySegment<byte> data, EndPoint remoteEP)
        {
            try
            {
                if (!socket.Poll(0, SelectMode.SelectWrite)) 
                    return false;

                socket.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, remoteEP);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) 
                    return false;

                throw;
            }
        }

        public static bool SendNonBlocking(this Socket socket, ArraySegment<byte> data)
        {
            try
            {
                if (!socket.Poll(0, SelectMode.SelectWrite)) 
                    return false;

                socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) 
                    return false;

                throw;
            }
        }

        public static bool ReceiveFromNonBlocking(this Socket socket, byte[] recvBuffer, out ArraySegment<byte> data, ref EndPoint remoteEP)
        {
            data = default;

            try
            {
                if (!socket.Poll(0, SelectMode.SelectRead)) 
                    return false;

                int size = socket.ReceiveFrom(recvBuffer, 0, recvBuffer.Length, SocketFlags.None, ref remoteEP);
                data = new ArraySegment<byte>(recvBuffer, 0, size);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock) 
                    return false;

                throw;
            }
        }

        public static bool ReceiveNonBlocking(this Socket socket, byte[] recvBuffer, out ArraySegment<byte> data)
        {
            data = default;

            try
            {
                if (!socket.Poll(0, SelectMode.SelectRead)) 
                    return false;

                int size = socket.Receive(recvBuffer, 0, recvBuffer.Length, SocketFlags.None);
                data = new ArraySegment<byte>(recvBuffer, 0, size);
                return true;
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.WouldBlock)
                    return false;

                throw;
            }
        }
    }
}
