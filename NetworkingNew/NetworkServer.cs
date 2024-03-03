using Common.IO.Collections;

using System.Net;
using System.Net.Sockets;

namespace Networking
{
    public class NetworkServer
    {
        private Socket server;
        private LockedDictionary<IPEndPoint, NetworkPeer> peers;

        public bool IsRunning
        {
            get => server != null && server.Client.IsBound;
        }

        public int Connections
        {
            get => peers.Count;
        }

        public int Port
        {
            get => ((IPEndPoint)server.Client.LocalEndPoint).Port;
        }

        public static NetworkServer Instance { get; } = new NetworkServer();

        public void Start(int port)
        {
            if (server != null)
                Stop();

            server = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            server.Client.Bind(new IPEndPoint(IPAddress.Any, port))
        }
        
        public void Stop()
        {

        }
    }
}