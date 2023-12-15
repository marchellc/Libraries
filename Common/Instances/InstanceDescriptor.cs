using Common.Values;

using System;

namespace Common.Instances
{
    public class InstanceDescriptor
    {
        public WeakValue<object> Reference { get; }

        public Type Type { get; }

        public int HashCode { get; }

        public InstanceDescriptor(object value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            Type = value.GetType();
            Reference = new WeakValue<object>(value);

            try
            {
                HashCode = value.GetHashCode();
            }
            catch { HashCode = -1; }
        }
    }
}