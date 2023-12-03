using System.IO;
using System.Reflection;

namespace Network.Interfaces.Synchronization
{
    public interface ISynchronizedValue
    {
        ISynchronizedRoot Root { get; }
        ISynchronizationManager Manager { get; }

        PropertyInfo Property { get; }

        short Id { get; }

        bool IsSynchronized { get; }

        void Initialize(ISynchronizedRoot root, PropertyInfo property, short id);

        void Update(BinaryReader reader);
        void Update(BinaryWriter writer);
    }
}