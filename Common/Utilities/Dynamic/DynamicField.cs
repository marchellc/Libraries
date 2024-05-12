using System;
using System.Reflection.Emit;
using System.Reflection;

namespace Common.Utilities.Dynamic
{
    public class DynamicField
    {
        public delegate object GetFieldDelegate(object instance);
        public delegate void SetFieldDelegate(object instance, object value);

        public FieldInfo Field { get; }

        public GetFieldDelegate GetProxy { get; }
        public SetFieldDelegate SetProxy { get; }

        private DynamicField(FieldInfo fieldInfo, GetFieldDelegate getProxy, SetFieldDelegate setProxy)
        {
            Field = fieldInfo;
            GetProxy = getProxy;
            SetProxy = setProxy;
        }

        public object GetValue(object instance)
            => GetProxy.Invoke(instance);

        public T GetValue<T>(object instance)
            => (T)GetValue(instance);

        public void SetValue(object instance, object value)
            => SetProxy.Invoke(instance, value);

        public static DynamicField Create(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
                throw new ArgumentNullException("fieldInfo");

            var getProxy = CreateGetMethodProxy(fieldInfo);
            var setProxy = CreateSetMethodProxy(fieldInfo);

            return new DynamicField(fieldInfo, getProxy, setProxy);
        }

        public static explicit operator DynamicField(FieldInfo fieldInfo)
            => Create(fieldInfo);

        private static GetFieldDelegate CreateGetMethodProxy(FieldInfo fieldInfo)
        {
            var method = new System.Reflection.Emit.DynamicMethod(fieldInfo.Name + "_Get_Proxy", typeof(object), new[] { typeof(object) }, fieldInfo.DeclaringType);
            var wrapper = new ILGeneratorHelper(method.GetILGenerator());

            wrapper.DeclareLocals(new Type[] { fieldInfo.FieldType });

            wrapper.EmitLoadArg(0);
            wrapper.EmitLoadField(fieldInfo);
            wrapper.EmitStoreLocal(0);
            wrapper.EmitLoadLoc(0);

            if (fieldInfo.FieldType.IsValueType)
                wrapper.EmitBox(fieldInfo.FieldType);

            wrapper.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(GetFieldDelegate)) as GetFieldDelegate;
        }

        private static SetFieldDelegate CreateSetMethodProxy(FieldInfo info)
        {
            var method = new System.Reflection.Emit.DynamicMethod(info.Name + "_Set_Proxy", typeof(void), new[] { typeof(object), typeof(object) }, info.DeclaringType);
            var wrapper = new ILGeneratorHelper(method.GetILGenerator());

            wrapper.DeclareLocals(new Type[] { info.FieldType });
            wrapper.EmitLoadArg(0);
            wrapper.EmitLoadArg(1);

            if (info.FieldType.IsValueType)
                wrapper.EmitUnbox_Any(info.FieldType);

            wrapper.EmitStoreField(info);
            wrapper.Emit(OpCodes.Ret);

            return method.CreateDelegate(typeof(SetFieldDelegate)) as SetFieldDelegate;
        }
    }
}
