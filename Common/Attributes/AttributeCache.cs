using System;
using System.Reflection;

namespace Common.Attributes
{
    public struct AttributeCache
    {
        public MemberInfo Member;
        public Type Type;
        public Assembly Assembly;
        public Attribute Attribute;
        public AttributeUsageAttribute Usage;
        public AttributeTargets Target;
        public object Instance;
    }
}