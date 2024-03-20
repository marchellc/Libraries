using System;

namespace Networking.Entities.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NetEntityRemoteTypeAttribute : Attribute
    {
        public string Name { get; }

        public bool IsNamespace { get; }

        public NetEntityRemoteTypeAttribute(string name, bool hasNamespace = false)
        {
            Name = name;
            IsNamespace = hasNamespace;
        }
    }
}