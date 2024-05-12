using Network.Features;

namespace Network.Data
{
    public interface IDataManager : INetworkFeature
    {
        DataMode Mode { get; }
        DataManagerSettings Settings { get; set; }

        long TotalBytesSent { get; }
        long TotalBytesReceived { get; }

        byte[] Serialize(DataPack dataPack);

        DataPack Deserialize(byte[] data);
    }
}