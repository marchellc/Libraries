using System.Reflection;

namespace Networking.Objects
{
    public class NetworkMethod
    {
        public readonly int parentId;
        public readonly int methodId;

        public readonly MethodInfo targetRpc;
        public readonly MethodInfo callRpc;

        public readonly object reference;

        public NetworkMethod(int parentId, int methodId, MethodInfo targetRpc, MethodInfo callRpc, object reference)
        {
            this.parentId = parentId;
            this.methodId = methodId;
            this.targetRpc = targetRpc;
            this.callRpc = callRpc;
            this.reference = reference;
        }
    }
}