using System;

namespace Common.Instances
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class InstanceAttribute : Attribute
    {
        public Type[] Types { get; }

        public InstanceAttribute(params Type[] types)
            => Types = types;
    }
}