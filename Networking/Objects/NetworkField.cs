using Common.Extensions;

using Networking.Data;

using System;

namespace Networking.Objects
{
    public class NetworkField<T> : NetworkVariable
    {
        private T value;

        public event Action<T, T> OnChanged;

        public T Value
        {
            get => value;
            set
            {
                if (this.value is null || value is null)
                    return;

                if (this.value is null && value != null
                    || (this.value != null && value is null)
                    || (!this.value.Equals(value)))
                {
                    var curValue = this.value;

                    this.value = value;
                    this.pending.Enqueue(new NetworkFieldUpdateMessage<T>(value));

                    OnChanged.Call(curValue, this.value);
                }
            }
        }

        public T ValueDirect { get => value; set => this.value = value; }

        public override void Process(IMessage msg)
        {
            if (msg is not NetworkFieldUpdateMessage<T> updateMsg)
                return;

            var prevValue = value;
            value = updateMsg.value;

            OnChanged.Call(prevValue, value);
        }
    }

    public struct NetworkFieldUpdateMessage<T> : IMessage
    {
        public T value;

        public NetworkFieldUpdateMessage(T value)
        {
            this.value = value;
        }

        public void Deserialize(Reader reader)
        {
            value = reader.Read<T>();
        }

        public void Serialize(Writer writer)
        {
            writer.Write<T>(value);
        }
    }
}
