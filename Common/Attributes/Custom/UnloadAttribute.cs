using System;

namespace Common.Attributes.Custom
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class UnloadAttribute : Attribute { }
}