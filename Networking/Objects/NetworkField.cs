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
                pending.Add(new NetworkFieldUpdateMessage(value));
            }
        }
    }

    public struct NetworkFieldUpdateMessage : IMessage
    {
        public object value;

        public NetworkFieldUpdateMessage(object value)
        {
            this.value = value;
        }

        public void Deserialize(Reader reader)
        {
            value = reader.ReadAnonymous();
        }

        public void Serialize(Writer writer)
        {
            writer.WriteAnonymous(value);
        }
    }
}
