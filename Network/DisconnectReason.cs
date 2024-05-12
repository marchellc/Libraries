using System.Linq;

namespace Network
{
    public struct DisconnectReason
    {
        public object[] Arguments { get; }
        public string Message { get; }
        public bool ShouldReconnect { get; }

        public DisconnectReason(string msg, bool reconnect, params object[] args)
        {
            Message = msg;
            ShouldReconnect = reconnect;
            Arguments = args;
        }

        public override string ToString()
        {
            var baseMessage = Message;

            if (string.IsNullOrWhiteSpace(baseMessage))
                baseMessage = "Unknown reason";

            if (ShouldReconnect)
                baseMessage += " - reconnect available -";
            else
                baseMessage += " - reconnect unavailable -";

            if (Arguments.Length > 0)
                baseMessage += $"({string.Join(",", Arguments.Where(arg => arg != null))})";

            return baseMessage;
        }
    }
}