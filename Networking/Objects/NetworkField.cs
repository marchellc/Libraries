using Networking.Data;

namespace Networking.Objects
{
    public class NetworkField<T> : NetworkVariable
    {
        private T value;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                this.pending.Enqueue(new NetworkFieldUpdateMessage<T>(value));
            }
        }

        public override void Process(IMessage msg)
        {
            if (msg is not NetworkFieldUpdateMessage<T> updateMsg)
                return;

            value = updateMsg.value;
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
