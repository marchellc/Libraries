using Common.IO.Data;

namespace Networking.Components.Calls
{
    public struct RemoteCallMessage : IData
    {
        public ushort TargetIdentity;
        public ushort TargetObject;
        public string TargetMethod;
        public object[] TargetArgs;

        public RemoteCallMessage(ushort targetIdentity, ushort targetObject, string targetMethod, object[] targetArgs)
        {
            TargetIdentity = targetIdentity;
            TargetObject = targetObject;
            TargetMethod = targetMethod;
            TargetArgs = targetArgs;
        }

        public void Deserialize(DataReader reader)
        {
            TargetIdentity = reader.ReadUShort();
            TargetObject = reader.ReadUShort();
            TargetMethod = reader.ReadString();
            TargetArgs = reader.ReadArray<object>();
        }

        public void Serialize(DataWriter writer)
        {
            writer.WriteUShort(TargetIdentity);
            writer.WriteUShort(TargetObject);
            writer.WriteString(TargetMethod);
            writer.WriteEnumerable(TargetArgs);
        }
    }
}