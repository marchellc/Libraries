using Common.IO.Data;
using Common.Pooling.Pools;

using Network.Features;

using System;

namespace Network.Data
{
    public class DataManager : NetworkFeatureBase, IDataManager
    {
        private readonly DataMode m_Mode;
        private DataManagerSettings m_Settings;
        private long m_Sent;
        private long m_Received;

        public DataMode Mode => m_Mode;
        public DataManagerSettings Settings { get => m_Settings; set => m_Settings = value; }

        public long TotalBytesSent => m_Sent;
        public long TotalBytesReceived => m_Received;

        public DataManager(DataMode mode = DataMode.SendQueue, DataManagerSettings? settings = null)
        {
            m_Mode = mode;
            m_Settings = settings.HasValue ? settings.Value : new DataManagerSettings(true);
        }

        public DataPack Deserialize(byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < 4)
                return null;

            var pack = PoolablePool<DataPack>.Shared.Rent();
            var reader = DataReader.Get(data);

            pack.Read(reader);

            reader.Return();
            reader = null;

            if (pack.Pack.Count < 1)
            {
                PoolablePool<DataPack>.Shared.Return(pack);
                return null;
            }

            m_Received += data.LongLength;
            return pack;
        }

        public byte[] Serialize(DataPack dataPack)
        {
            if (dataPack is null)
                throw new ArgumentNullException(nameof(dataPack));

            if (dataPack.Pack.Count < 1)
                return null;

            var writer = DataWriter.Get();

            dataPack.Write(writer);

            var bytes = writer.Data;

            writer.Return();
            writer = null;

            PoolablePool<DataPack>.Shared.Return(dataPack);

            dataPack = null;

            m_Sent += bytes.LongLength;
            return bytes;
        }
    }
}
