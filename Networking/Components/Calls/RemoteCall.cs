using Common.Extensions;

using System;
using System.Linq;
using System.Reflection;

namespace Networking.Components.Calls
{
    public class RemoteCall
    {
        public MethodInfo Method { get; }
        public Type[] Arguments { get; }

        public RemoteCallType Type { get; }

        public RemoteCall(MethodInfo method, RemoteCallType type)
        {
            Method = method;
            Type = type;
            Arguments = method.Parameters().Select(p => p.ParameterType).ToArray();
        }

        public void Execute(object instance, object[] args)
            => Method.Call(instance, args);

        public bool ValidateArguments(object[] messageArgs, out Type expectedType, out Type receivedType, out int argumentPos)
        {
            expectedType = null;
            receivedType = null;

            argumentPos = 0;

            if (messageArgs.Length != Arguments.Length)
                return false;

            for (int i = 0; i < messageArgs.Length; i++)
            {
                argumentPos = i;
                expectedType = Arguments[i];

                if (messageArgs[i] is null && Arguments[i].IsValueType)
                    return false;

                if (messageArgs[i] is null)
                    continue;

                if (messageArgs[i].GetType() != Arguments[i])
                {
                    receivedType = messageArgs[i].GetType();
                    return false;
                }
            }

            return true;
        }

        public bool ValidateCall(bool isClient)
            => isClient ? Type is RemoteCallType.Rpc : Type is RemoteCallType.Command;
    }
}