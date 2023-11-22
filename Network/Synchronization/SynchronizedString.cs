using Common.Extensions;

using System.IO;

namespace Network.Synchronization
{
    public class SynchronizedString : SynchronizedProperty
    {
        private string value = string.Empty;

        public string Value
        {
            get => value;
            set
            {
                this.value = value;
                Update();
            }
        }

        public override void Update(BinaryReader reader)
        {
            base.Update(reader);
            value = reader.ReadStringEx();
        }

        public override void Update(BinaryWriter writer)
        {
            base.Update(writer);
            writer.WriteString(value);
        }

        public override void OnDestroyed()
        {
            base.OnDestroyed();
            value = null;
        }
    }
}