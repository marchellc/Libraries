using System;

namespace Network.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class NetworkTypeAttribute : Attribute { }
}