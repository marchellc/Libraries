using System;

namespace Common.IO.Data
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class DataWriterAttribute : Attribute
    {
        public Type ReplacedType { get; set; }
    }
}