using Common.Utilities;

using Networking.Kcp;

using System.Net;

namespace Networking.Client
{
    public static class NetClientConnector
    {
        public static void ConnectIndefinitely(KcpClient client, IPEndPoint target)
        {
            CodeUtils.OnThread(() =>
            {
                while (true)
                {
                    try
                    {
                        client.Connect(target.Address.ToString(), (ushort)target.Port);
                        break;
                    }
                    catch { continue; }
                }
            });
        }
    }
}