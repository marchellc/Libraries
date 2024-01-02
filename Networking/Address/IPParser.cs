using System.Linq;
using System.Globalization;
using System.Net;
using System;

namespace Networking.Address
{
    public static class IPParser
    {
        public static bool TryParse(string ip, out IPInfo info)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ip))
                {
                    info = default;
                    return false;
                }

                if (ip.Count(c => c is ':') > 1)
                {
                    info = default;
                    return false;
                }

                if (ip.Contains(":"))
                {
                    var ipParts = ip.Split(':');

                    if (ipParts.Length != 2
                        || !int.TryParse(ipParts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var portNum))
                    {
                        info = default;
                        return false;
                    }

                    if (ipParts[0].ToLower() == "localhost")
                        ipParts[0] = "127.0.0.1";

                    if (!IPAddress.TryParse(ip, out var ipObj))
                    {
                        info = default;
                        return false;
                    }

                    info = new IPInfo(GetType(ipObj), portNum, ipObj);
                    return true;
                }
                else if (ip.ToLower() == "localhost")
                {
                    ip = "127.0.0.1";
                }

                if (!IPAddress.TryParse(ip, out var ipValue))
                {
                    info = default;
                    return false;
                }

                info = new IPInfo(GetType(ipValue), 0, ipValue);
                return true;
            }
            catch 
            {
                info = default;
                return false;
            }
        }

        private static IPType GetType(IPAddress address)
        {
            var ipStr = address.ToString();

            if (ipStr == IPAddress.Any.ToString() || ipStr == IPAddress.Loopback.ToString())
                return IPType.Local;

            return IPType.Remote;
        }
    }
}