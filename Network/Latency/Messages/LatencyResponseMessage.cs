using Common.IO.Data;

namespace Network.Latency.Messages
{
    public struct LatencyResponseMessage : IData
    {
        public static readonly LatencyResponseMessage Instance = new LatencyResponseMessage();

        public void Deserialize(DataReader reader) { }
        public void Serialize(DataWriter writer) { }
    }
}
