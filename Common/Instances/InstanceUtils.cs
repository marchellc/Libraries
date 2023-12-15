using Common.Extensions;

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Common.Instances
{
    public static class InstanceUtils
    {
        public static Func<object> CompileConstructor(ConstructorInfo constructor)
        {
            if (constructor is null)
                throw new ArgumentNullException(nameof(constructor));

            return Expression.Lambda<Func<object>>(Expression.New(constructor)).Compile();
        }

        public static ConstructorInfo FindConstructor(Type type)
        {
            foreach (var constructor in type.GetAllConstructors())
            {
                if (constructor.GetParameters().Length > 0)
                    continue;

                return constructor;
            }

            return null;
        }
    }
}