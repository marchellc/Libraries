using Common.IO.Data;
using Common.Logging;
using Common.Pooling.Pools;

using System;

namespace Networking.Data
{
    public struct NetPack : IData
    {
        public static readonly LogOutput Log = new LogOutput("NetPack").Setup();

        public int ReadSize;
        public int ValidSize;

        public IData[] Pack;

        public NetPack(IData[] pack)
        {
            Pack = pack;
            ReadSize = pack.Length;
            ValidSize = pack.Length;
        }

        public void Deserialize(DataReader reader)
        {
            ReadSize = reader.ReadInt();

            var list = ListPool<IData>.Shared.Rent();

            for (int i = 0; i < ReadSize; i++)
            {
                try
                {
                    var type = reader.ReadType();
                    var data = reader.ReadAnonymous<IData>(type);

                    list.Add(data);
                }
                catch (Exception ex)
                {
                    Log.Error($"Caught an exception while reading data at i={i}:\n{ex}");
                }
            }

            ValidSize = list.Count;
            Pack = ListPool<IData>.Shared.ToArrayReturn(list);
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteInt(ValidSize);

            for (int i = 0; i < Pack.Length; i++)
            {
                writer.WriteType(Pack[i].GetType());
                writer.WriteAnonymous(Pack[i]);
            }
        }
    }
}