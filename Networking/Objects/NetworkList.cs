using Common.IO.Collections;
using Common.Utilities;

using Networking.Data;
using Networking.Utilities;

using System.Collections;
using System.Collections.Generic;

namespace Networking.Objects
{
    public class NetworkList<T> : NetworkVariable, IList<T>
    {
        private readonly LockedList<T> list;

        public NetworkList()
        {
            list = new LockedList<T>();

            // make sure that we have a writer & reader
            TypeLoader.GetWriter(typeof(T));
            TypeLoader.GetReader(typeof(T));
        }

        public T this[int index]
        {
            get => list[index];
            set => Insert(index, value);
        }

        public int Count => list.Count;

        public bool IsReadOnly => list.IsReadOnly;

        public void Add(T item)
        {
            list.Add(item);

            var writer = parent.net.GetWriter();

            writer.Write(item);

            pending.Add(new NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Add, writer));
        }

        public void Clear()
        {
            list.Clear();
            pending.Add(new NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Clear, null));
        }

        public void Insert(int index, T item)
        {
            if (index.IsValidIndex(list.Count))
            {
                list[index] = item;

                var writer = parent.net.GetWriter();

                writer.WriteInt(index);
                writer.Write(item);

                pending.Add(new NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Set, writer));
            }
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);

            if (index == -1)
                return false;

            if (list.Remove(item))
            {
                var writer = parent.net.GetWriter();

                writer.WriteInt(index);

                pending.Add(new NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Remove, writer));

                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            if (index.IsValidIndex(list.Count))
            {
                list.RemoveAt(index);

                var writer = parent.net.GetWriter();

                writer.WriteInt(index);

                pending.Add(new NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Remove, writer));
            }
        }

        public bool Contains(T item) => list.Contains(item);

        public int IndexOf(T item) => list.IndexOf(item);

        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        public override void Process(IDeserialize deserialize)
        {
            if (deserialize is not NetworkListUpdateMessage updateMsg)
                return;

            switch (updateMsg.op)
            {
                case NetworkListUpdateMessage.ListOp.Set:
                    {
                        var index = updateMsg.reader.ReadInt();
                        var value = updateMsg.reader.Read<T>();

                        list[index] = value;

                        break;
                    }

                case NetworkListUpdateMessage.ListOp.Clear:
                    {
                        list.Clear();
                        break;
                    }

                case NetworkListUpdateMessage.ListOp.Remove:
                    {
                        var index = updateMsg.reader.ReadInt();

                        list.RemoveAt(index);

                        break;
                    }

                case NetworkListUpdateMessage.ListOp.Add:
                    {
                        var value = updateMsg.reader.Read<T>();

                        list.Add(value);

                        break;
                    }
            }

            updateMsg.FinishReader();
        }
    }

    public struct NetworkListUpdateMessage : IMessage
    {
        public enum ListOp
        {
            Remove,
            Clear,
            Add,
            Set,
        }

        public ListOp op;

        public Reader reader;
        public Writer writer;

        public NetworkListUpdateMessage(ListOp op, Writer writer)
        {
            this.op = op;
            this.writer = writer;
        }

        public void Deserialize(Reader reader)
        {
            this.op = (ListOp)reader.ReadByte();
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
