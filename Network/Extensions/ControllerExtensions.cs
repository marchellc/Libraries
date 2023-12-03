using System.Net;

using Network.Interfaces.Controllers;

namespace Network.Extensions
{
    public static class ControllerExtensions
    {
        public static bool TrySetAddress(this IController controller, IPEndPoint endPoint)
        {
            if (controller is null)
                return false;

            controller.Target = endPoint;
            return true;
        }

        public static bool TrySetAddress(this IController controller, IPAddress address, int port)
        {
            if (controller is null)
                return false;

            controller.Target = new IPEndPoint(address, port);
            return true;
        }

        public static bool TrySetAddress(this IController controller, string ip, int port)
        {
            if (controller is null)
                return false;

            IpHelper.FixIp(ref ip);

            if (!IPAddress.TryParse(ip, out var address))
                return false;

            controller.Target = new IPEndPoint(address, port);
            return true;
        }

        public static bool TrySetAddress(this IController controller, string ip)
        {
            if (controller is null)
                return false;

            IpHelper.FixIp(ref ip);

            if (!ip.Contains(":"))
                return false;

            var split = ip.Split(':');

            if (split.Length != 2)
                return false;

            if (!IPAddress.TryParse(split[0], out var address))
                return false;

            if (!int.TryParse(split[1], out var port))
                return false;

            controller.Target = new IPEndPoint(address, port);
            return true;
        }
    }
}