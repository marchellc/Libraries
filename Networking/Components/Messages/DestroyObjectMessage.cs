using Common.IO.Data;

namespace Networking.Components.Messages
{
    public struct DestroyObjectMessage : IData
    {
        public ushort ParentIdentityId;
        public ushort TargetObject;

        public DestroyObjectMessage(ushort parentIdentityId, ushort targetObject)
        {
            ParentIdentityId = parentIdentityId;
            TargetObject = targetObject;
        }

        public void Deserialize(DataReader reader)
        {
            ParentIdentityId = reader.ReadUShort();
            TargetObject = reader.ReadUShort();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteUShort(ParentIdentityId);
            writer.WriteUShort(TargetObject);
        }
    }
}