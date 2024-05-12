using Common.Extensions;
using Common.IO.Data;
using Common.Pooling;

using System.Collections.Generic;

namespace Network.Data
{
    public class DataPack : PoolableItem
    {
        private readonly HashSet<object> _pack = new HashSet<object>();

        public IReadOnlyCollection<object> Pack
        {
            get => _pack;
        }

        public DataPack Write(params object[] args)
        {
            _pack.AddRange(args);
            return this;
        }

        public void Write(DataWriter dataWriter)
        {
            dataWriter.WriteEnumerable(_pack);
        }

        public void Read(DataReader dataReader)
        {
            _pack.Clear();
            _pack.AddRange(dataReader.ReadList<object>());
        }

        public void Clear()
            => _pack.Clear();

        public override void OnPooled()
        {
            base.OnPooled();
            _pack.Clear();
        }
    }
}
