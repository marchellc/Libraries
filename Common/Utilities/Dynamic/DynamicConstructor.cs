using System;
using System.Reflection.Emit;
using System.Reflection;

using Common.Extensions;

namespace Common.Utilities.Dynamic
{
    public class DynamicConstructor
    {
        public delegate object DynamicMethodDelegate(object[] args);

        public ConstructorInfo Constructor { get; private set; }
        public DynamicMethodDelegate Proxy { get; private set; }

        private DynamicConstructor(ConstructorInfo constructor, DynamicMethodDelegate proxy)
        {
            Constructor = constructor;
            Proxy = proxy;
        }

        public object Invoke()
            => Invoke(null);

        public T Invoke<T>() where T : class
            => Invoke() as T;

        public object Invoke(params object[] parameters)
            => Proxy.Invoke(parameters);

        public T Invoke<T>(params object[] parameters) where T : class
            => Invoke(parameters) as T;

        public static DynamicConstructor Create<T>()
            => Create(typeof(T));

        public static DynamicConstructor Create(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var constructors = type.GetAllConstructors();

            if (constructors == null || constructors.Length == 0)
                throw new ArgumentException("Type has no constructor");

            return Create(constructors[0]);
        }

        public static DynamicConstructor Create(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException("constructor");

            var proxy = CreateProxyMethod(constructor);
            return new DynamicConstructor(constructor, proxy);
        }

        public static explicit operator DynamicConstructor(ConstructorInfo constructorInfo)
            => Create(constructorInfo);

        private static DynamicMethodDelegate CreateProxyMethod(ConstructorInfo constructor)
        {
            var method = new System.Reflection.Emit.DynamicMethod(constructor.DeclaringType.Name + "_ctor_Proxy", typeof(object), new Type[] { typeof(object[]) }, constructor.DeclaringType);
            var wrapper = new ILGeneratorHelper(method.GetILGenerator());
            var parameters = constructor.GetParameters();
            var parameterTypes = new Type[parameters.Length + 1];

            parameterTypes[0] = constructor.DeclaringType;

            for (int i = 0; i < parameters.Length; i++)
                parameterTypes[i + 1] = parameters[i].ParameterType;

            wrapper.DeclareLocals(parameterTypes);

            for (int i = 1; i < parameterTypes.Length; i++)
            {
                wrapper.EmitLoadArg(0);
                wrapper.EmitLoadCons(i - 1);
                wrapper.Emit(OpCodes.Ldelem_Ref);
                wrapper.EmitCast(parameterTypes[i]);
                wrapper.EmitStoreLocal(i);
            }

            for (int i = 1; i < parameterTypes.Length; i++)
                wrapper.EmitLoadLoc(i);

            wrapper.Emit_NewObj(constructor);
            wrapper.EmitStoreLocal(0);
            wrapper.EmitLoadLoc(0);
            wrapper.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(DynamicMethodDelegate)) as DynamicMethodDelegate;
        }
    }
}