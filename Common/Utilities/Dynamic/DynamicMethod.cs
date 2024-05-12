using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Common.Utilities.Dynamic
{
    public class DynamicMethod
    {
        public delegate object DynamicMethodDelegate(object instance, object[] args);

        public MethodInfo Method { get; private set; }
        public DynamicMethodDelegate Proxy { get; private set; }

        private DynamicMethod(MethodInfo methodInfo, DynamicMethodDelegate proxy)
        {
            Method = methodInfo;
            Proxy = proxy;
        }

        public object Invoke()
            => Invoke(null, new object[0]);

        public T InvokeStatic<T>()
            => (T)InvokeStatic();

        public object InvokeStatic(params object[] parameters)
            => InvokeStatic(null, parameters);

        public T InvokeStatic<T>(params object[] parameters)
            => (T)InvokeStatic(parameters);

        public object Invoke(object instance, params object[] parameters)
            => Proxy.Invoke(instance, parameters);

        public T Invoke<T>(object instance, params object[] parameters)
            => (T)Invoke(instance, parameters);

        public static DynamicMethod Create(MethodInfo methodInfo)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");

            var proxy = CreateMethodProxy(methodInfo);
            return new DynamicMethod(methodInfo, proxy);
        }

        public static explicit operator DynamicMethod(MethodInfo methodInfo)
            => Create(methodInfo);

        private static DynamicMethodDelegate CreateMethodProxy(MethodInfo methodInfo)
        {
            var method = new System.Reflection.Emit.DynamicMethod(methodInfo.Name + "_Proxy", typeof(object), new Type[] { typeof(object), typeof(object[]) }, methodInfo.DeclaringType);
            var wrapper = new ILGeneratorHelper(method.GetILGenerator());
            var parameters = methodInfo.GetParameters();
            var parameterTypes = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                parameterTypes[i] = parameters[i].ParameterType;

            wrapper.DeclareLocals(parameterTypes);

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                wrapper.EmitLoadArg(1);
                wrapper.EmitLoadCons(i);
                wrapper.Emit(OpCodes.Ldelem_Ref);
                wrapper.EmitCast(parameterTypes[i]);
                wrapper.EmitStoreLocal(i);
            }

            if (!methodInfo.IsStatic)
                wrapper.Emit(OpCodes.Ldarg_0);

            for (int i = 0; i < parameterTypes.Length; i++)
                wrapper.EmitLoadLoc(i);

            wrapper.EmitCall(methodInfo);

            if (methodInfo.ReturnType == typeof(void))
                wrapper.Emit(OpCodes.Ldnull);
            else if (methodInfo.ReturnType.IsValueType)
                wrapper.EmitBox(methodInfo.ReturnType);

            wrapper.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(DynamicMethodDelegate)) as DynamicMethodDelegate;
        }
    }
}
