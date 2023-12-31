using System;

namespace Networking.Objects
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class NetVarAttribute : Attribute { }
}