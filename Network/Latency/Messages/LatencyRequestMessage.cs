using Common.IO.Data;

namespace Network.Latency.Messages
{
    public struct LatencyRequestMessage : IData
    {
        public static readonly LatencyRequestMessage Instance = new LatencyRequestMessage();

        public void Deserialize(DataReader reader) { }
        public void Serialize(DataWriter writer) { }
    }
}