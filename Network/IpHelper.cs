using System;

namespace Network
{
    public static class IpHelper
    {
        public static void FixIp(ref string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException(nameof(ip));

            if (ip.Contains(":"))
            {
                var split = ip.Split(':');

                if (split.Length != 2)
                    throw new InvalidOperationException($"Invalid IP format: {ip}");

                if (split[0] == "localhost" || split[0] == "local")
                    ip = $"127.0.0.1:{split[1]}";
            }
            else
            {
                if (ip == "localhost" || ip == "local")
                    ip = "127.0.0.1";
            }
        }
    }
}