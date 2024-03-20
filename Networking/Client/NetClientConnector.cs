using Common.Utilities;

using WatsonTcp;

namespace Networking.Client
{
    public static class NetClientConnector
    {
        public static void ConnectIndefinitely(WatsonTcpClient client)
        {
            CodeUtils.OnThread(() =>
            {
                while (true)
                {
                    try
                    {
                        client.Connect();
                        break;
                    }
                    catch { continue; }
                }
            });
        }
    }
}