using System;

namespace Common.Instances
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class InstantiateAttribute : Attribute { }
}