using System;

namespace Network.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public class NetworkTypeAttribute : Attribute { }
}