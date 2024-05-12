using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Common.Utilities
{
    public class ILGeneratorHelper
    {
        public ILGenerator Generator { get; }
        public LocalBuilder[] Locals { get; private set; }

        public ILGeneratorHelper(ILGenerator generator)
            => Generator = generator;

        public void DeclareLocals(Type[] localTypes)
        {
            Locals = new LocalBuilder[localTypes.Length];

            for (int i = 0; i < localTypes.Length; i++)
                Locals[i] = Generator.DeclareLocal(localTypes[i]);
        }

        public void Emit_NewObj(ConstructorInfo constructor)
            => Generator.Emit(OpCodes.Newobj, constructor);

        public void Emit(OpCode opCode)
            => Generator.Emit(opCode);

        public void EmitStoreLocal(int value)
        {
            switch (value)
            {
                case 0:
                    Generator.Emit(OpCodes.Stloc_0);
                    break;

                case 1:
                    Generator.Emit(OpCodes.Stloc_1);
                    break;

                case 2:
                    Generator.Emit(OpCodes.Stloc_2);
                    break;

                case 3:
                    Generator.Emit(OpCodes.Stloc_3);
                    break;

                default:
                    if (IsSByte(value))
                        Generator.Emit(OpCodes.Stloc_S, (sbyte)value);
                    else
                        Generator.Emit(OpCodes.Stloc, value);

                    break;
            }
        }

        public void EmitLoadField(FieldInfo field)
            => Generator.Emit(OpCodes.Ldfld, field);

        public void EmitStoreField(FieldInfo field)
            => Generator.Emit(OpCodes.Stfld, field);

        public void EmitBox(Type type)
            => Generator.Emit(OpCodes.Box, type);

        public void EmitUnbox_Any(Type type)
            => Generator.Emit(OpCodes.Unbox_Any, type);

        public void EmitCall(MethodInfo info)
        {
            if (info.IsStatic)
                Generator.EmitCall(OpCodes.Call, info, null);
            else
                Generator.EmitCall(OpCodes.Callvirt, info, null);
        }

        public void EmitLoadLoc(int localIndex)
        {
            if (Locals[localIndex].LocalType.IsByRef)
                Generator.Emit(OpCodes.Ldloca_S, Locals[localIndex]);
            else
                Generator.Emit(OpCodes.Ldloc, Locals[localIndex]);
        }

        public void EmitCast(Type type)
        {
            if (type.IsValueType)
                Generator.Emit(OpCodes.Unbox_Any, type);
            else
                Generator.Emit(OpCodes.Castclass, type);
        }

        public void EmitLoadArg(int argumentIndex)
        {
            switch (argumentIndex)
            {
                case 0:
                    Generator.Emit(OpCodes.Ldarg_0);
                    break;

                case 1:
                    Generator.Emit(OpCodes.Ldarg_1);
                    break;

                case 2:
                    Generator.Emit(OpCodes.Ldarg_2);
                    break;

                case 3:
                    Generator.Emit(OpCodes.Ldarg_3);
                    break;

                default:
                    if (IsSByte(argumentIndex))
                        Generator.Emit(OpCodes.Ldarg_S, (sbyte)argumentIndex);
                    else
                        Generator.Emit(OpCodes.Ldarg, argumentIndex);

                    break;
            }
        }

        public void EmitLoadLocal(int localIndex)
        {
            switch (localIndex)
            {
                case 0:
                    Generator.Emit(OpCodes.Ldloc_0);
                    break;

                case 1:
                    Generator.Emit(OpCodes.Ldloc_1);
                    break;

                case 2:
                    Generator.Emit(OpCodes.Ldloc_2);
                    break;

                case 3:
                    Generator.Emit(OpCodes.Ldloc_3);
                    break;

                default:
                    if (IsSByte(localIndex))
                        Generator.Emit(OpCodes.Ldloc_S, (sbyte)localIndex);
                    else
                        Generator.Emit(OpCodes.Ldloc, localIndex);

                    break;
            }
        }

        public void EmitLoadCons(int value)
        {
            switch (value)
            {
                case -1:
                    Generator.Emit(OpCodes.Ldc_I4_M1);
                    break;

                case 0:
                    Generator.Emit(OpCodes.Ldc_I4_0);
                    break;

                case 1:
                    Generator.Emit(OpCodes.Ldc_I4_1);
                    break;

                case 2:
                    Generator.Emit(OpCodes.Ldc_I4_2);
                    break;

                case 3:
                    Generator.Emit(OpCodes.Ldc_I4_3);
                    break;

                case 4:
                    Generator.Emit(OpCodes.Ldc_I4_4);
                    break;

                case 5:
                    Generator.Emit(OpCodes.Ldc_I4_5);
                    break;

                case 6:
                    Generator.Emit(OpCodes.Ldc_I4_6);
                    break;

                case 7:
                    Generator.Emit(OpCodes.Ldc_I4_7);
                    break;

                case 8:
                    Generator.Emit(OpCodes.Ldc_I4_8);
                    break;

                default:
                    if (IsSByte(value))
                        Generator.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    else
                        Generator.Emit(OpCodes.Ldc_I4, value);

                    break;
            }
        }

        private static bool IsSByte(int value)
            => (value > -129 && value < 128);
    }
}