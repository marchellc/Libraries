using System;
using System.Reflection;

namespace Common.Utilities.Dynamic
{
    public class DynamicProperty
    {
        public PropertyInfo Property { get; private set; }

        public DynamicMethod GetMethod { get; private set; }
        public DynamicMethod SetMethod { get; private set; }

        private DynamicProperty(PropertyInfo propertyInfo, DynamicMethod getMethod, DynamicMethod setMethod)
        {
            Property = propertyInfo;

            GetMethod = getMethod;
            SetMethod = setMethod;
        }

        public object GetValue(object instance, params object[] indexes)
            => GetMethod.Invoke(instance, indexes);

        public T GetValue<T>(object instance, params object[] indexes)
            => (T)GetValue(instance, indexes);

        public void SetValue(object instance, object value)
            => SetMethod.Invoke(instance, value);

        public void SetValue(object instance, object value, object[] indexes)
        {
            if (indexes != null)
            {
                var args = new object[indexes.Length + 1];
                Array.Copy(indexes, args, indexes.Length);
                args[args.Length - 1] = value;
                SetMethod.Invoke(instance, args);
            }
            else
            {
                SetMethod.Invoke(instance, value);
            }
        }

        public static DynamicProperty Create(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");

            DynamicMethod getMethod = null;

            if (propertyInfo.CanRead)
                getMethod = DynamicMethod.Create(propertyInfo.GetMethod);

            DynamicMethod setMethod = null;

            if (propertyInfo.CanWrite)
                setMethod = DynamicMethod.Create(propertyInfo.SetMethod);

            return new DynamicProperty(propertyInfo, getMethod, setMethod);
        }

        public static explicit operator DynamicProperty(PropertyInfo propertyInfo)
            => Create(propertyInfo);
    }
}
