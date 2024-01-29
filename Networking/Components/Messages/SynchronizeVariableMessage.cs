using Common.IO.Data;

namespace Networking.Components.Messages
{
    public struct SynchronizeVariableMessage : IData
    {
        public ushort TargetIdentity;
        public ushort TargetObject;
        public string TargetVariable;
        public object TargetValue;

        public SynchronizeVariableMessage(ushort targetIdentity, ushort targetObject, string targetVariable, object targetValue)
        {
            TargetIdentity = targetIdentity;
            TargetObject = targetObject;
            TargetVariable = targetVariable;
            TargetValue = targetValue;
        }

        public void Deserialize(DataReader reader)
        {
            TargetIdentity = reader.ReadUShort();
            TargetObject = reader.ReadUShort();
            TargetVariable = reader.ReadString();
            TargetValue = reader.ReadObject();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteUShort(TargetIdentity);
            writer.WriteUShort(TargetObject);
            writer.WriteString(TargetVariable);
            writer.WriteObject(TargetValue);
        }
    }
}