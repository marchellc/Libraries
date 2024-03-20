using System.Net;

namespace Networking.Utilities
{
    public static class IpParser
    {
        public static IPEndPoint ParseEndPoint(string address, char splitter = ':')
        {
            var split = address.Split(splitter);

            var ip = split[0];
            var port = split[1];

            return new IPEndPoint(
                IPAddress.Parse(ip),
                int.Parse(port));
        }
    }
}
