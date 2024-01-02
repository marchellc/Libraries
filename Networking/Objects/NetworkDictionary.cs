using Common.Extensions;
using Common.IO.Collections;

using Networking.Data;
using Networking.Utilities;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Networking.Objects
{
    public class NetworkDictionary<TKey, TValue> : NetworkVariable, IDictionary<TKey, TValue>
    {
        private readonly LockedDictionary<TKey, TValue> dict = new LockedDictionary<TKey, TValue>();

        public NetworkDictionary()
        {
            dict = new LockedDictionary<TKey, TValue>();

            TypeLoader.GetWriter(typeof(TKey));
            TypeLoader.GetWriter(typeof(TValue));

            TypeLoader.GetReader(typeof(TKey));
            TypeLoader.GetReader(typeof(TValue));
        }

        public TValue this[TKey key]
        {
            get => dict[key];
            set 
            {
                dict[key] = value;

                var writer = parent.net.GetWriter();

                writer.Write(key);
                writer.Write(value);

                pending.Enqueue(new NetworkDictionaryUpdateMessage(NetworkDictionaryUpdateMessage.DictionaryOp.Set, writer));
            }
        }

        public ICollection<TKey> Keys => dict.Keys;
        public ICollection<TValue> Values => dict.Values;

        public int Count => dict.Count;
        public bool IsReadOnly => dict.IsReadOnly;

        public void Add(TKey key, TValue value)
        {
            dict.Add(key, value);

            var writer = parent.net.GetWriter();

            writer.Write(key);
            writer.Write(value);

            pending.Enqueue(new NetworkDictionaryUpdateMessage(NetworkDictionaryUpdateMessage.DictionaryOp.Add, writer));
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dict.Add(item);

            var writer = parent.net.GetWriter();

            writer.Write(item.Key);
            writer.Write(item.Value);

            pending.Enqueue(new NetworkDictionaryUpdateMessage(NetworkDictionaryUpdateMessage.DictionaryOp.Add, writer));
        }

        public void Clear()
        {
            dict.Clear();
            pending.Enqueue(new NetworkDictionaryUpdateMessage(NetworkDictionaryUpdateMessage.DictionaryOp.Clear, null));
        }

        public bool Remove(TKey key)
        {
            if (dict.Remove(key))
            {
                var writer = parent.net.GetWriter();

                writer.Write(key);

                pending.Enqueue(new NetworkDictionaryUpdateMessage(NetworkDictionaryUpdateMessage.DictionaryOp.Remove, writer));

                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var index = dict.FindKeyIndex(item.Key);

            if (index != -1 && dict.Remove(item))
            {
                var writer = parent.net.GetWriter();

                writer.Write(index);

                pending.Enqueue(new NetworkDictionaryUpdateMessage(NetworkDictionaryUpdateMessage.DictionaryOp.Remove, writer));

                return true;
            }

            return false;
        }


        public bool Contains(KeyValuePair<TKey, TValue> item) => dict.Contains(item);
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => dict.CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();

        public override void Process(IMessage msg)
        {
            if (msg is not NetworkDictionaryUpdateMessage updateMsg)
                return;

            switch (updateMsg.op)
            {
                case NetworkDictionaryUpdateMessage.DictionaryOp.Set:
                    {
                        var key = updateMsg.reader.Read<TKey>();
                        var value = updateMsg.reader.Read<TValue>();

                        dict[key] = value;

                        break;
                    }

                case NetworkDictionaryUpdateMessage.DictionaryOp.Clear:
                    {
                        dict.Clear();
                        break;
                    }

                case NetworkDictionaryUpdateMessage.DictionaryOp.Remove:
                    {
                        var keyIndex = updateMsg.reader.ReadInt();
                        var key = dict.ElementAtOrDefault(keyIndex).Key;

                        if (key is null)
                            return;

                        dict.Remove(key);

                        break;
                    }

                case NetworkDictionaryUpdateMessage.DictionaryOp.Add:
                    {
                        var key = updateMsg.reader.Read<TKey>();
                        var value = updateMsg.reader.Read<TValue>();

                        dict.Add(key, value);

                        break;
                    }
            }

            updateMsg.FinishReader();
        }
    }

    public struct NetworkDictionaryUpdateMessage : IMessage
    {
        public enum DictionaryOp
        {
            Remove,
            Clear,
            Add,
            Set,
        }

        public DictionaryOp op;

        public Reader reader;
        public Writer writer;

        public NetworkDictionaryUpdateMessage(DictionaryOp op, Writer writer)
        {
            this.op = op;
            this.writer = writer;
        }

        public void Deserialize(Reader reader)
        {
            this.op = (DictionaryOp)reader.ReadByte();

            if (this.op == DictionaryOp.Clear)
                return;

            this.reader = reader.ReadReader();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteByte((byte)this.op);

            if (this.writer != null)
            {
                writer.WriteWriter(this.writer);

                this.writer.Clear();

                if (this.writer.pool != null)
                    this.writer.Return();

                this.writer = null;
            }
        }

        public void FinishReader()
        {
            if (reader != null)
            {
                reader.Clear();

                if (reader.pool != null)
                    reader.Return();

                reader = null;
            }
        }
    }
}
