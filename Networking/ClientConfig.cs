namespace Networking
{
    public class ClientConfig
    {
        public int IncomingAmount { get; set; } = 10;
        public int OutgoingAmount { get; set; } = 10;

        public int MaxReconnectionAttempts { get; set; } = 15;
    }
}