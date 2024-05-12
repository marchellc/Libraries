using Common.Extensions;
using Common.IO.Collections;
using Common.Logging;
using Common.Pooling.Pools;
using Common.Utilities;

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using DynamicMethod = Common.Utilities.Dynamic.DynamicMethod;

namespace Common.Extensions
{
    public static class MethodExtensions
    {
        private static readonly BindingFlags _flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly LockedDictionary<Type, MethodInfo[]> _methods = new LockedDictionary<Type, MethodInfo[]>();
        private static readonly LockedDictionary<MethodInfo, DynamicMethod> _dynamic = new LockedDictionary<MethodInfo, DynamicMethod>();
        private static readonly LockedDictionary<MethodBase, ParameterInfo[]> _params = new LockedDictionary<MethodBase, ParameterInfo[]>();

        public static readonly LogOutput Log = new LogOutput("Method Extensions").Setup();

        public static readonly OpCode[] OneByteCodes;
        public static readonly OpCode[] TwoByteCodes;

        public static bool EnableLogging;

        static MethodExtensions()
        {
            OneByteCodes = new OpCode[225];
            TwoByteCodes = new OpCode[31];

            foreach (var field in typeof(OpCodes).GetAllFields())
            {
                if (!field.IsStatic || field.FieldType != typeof(OpCode))
                    continue;

                var opCode = field.Get<OpCode>();

                if (opCode.OpCodeType is OpCodeType.Nternal)
                    continue;

                if (opCode.Size == 1)
                    OneByteCodes[opCode.Value] = opCode;
                else
                    TwoByteCodes[opCode.Value & byte.MaxValue] = opCode;
            }
        }

        public static MethodInfo ToGeneric(this MethodInfo method, params Type[] args)
            => method.MakeGenericMethod(args);

        public static MethodInfo ToGeneric<T>(this MethodInfo method)
            => method.ToGeneric(typeof(T));

        public static MethodInfo[] GetAllMethods(this Type type)
        {
            if (_methods.TryGetValue(type, out var methods))
                return methods;

            return _methods[type] = type.GetMethods(_flags);
        }

        public static ParameterInfo[] Parameters(this MethodBase method)
        {
            if (_params.TryGetValue(method, out var parameters))
                return parameters;

            return _params[method] = method.GetParameters();
        }

        public static MethodInfo Method(this Type type, string name, bool ignoreCase = false)
            => GetAllMethods(type).FirstOrDefault(m => ignoreCase ? m.Name.ToLower() == name.ToLower() : m.Name == name);

        public static MethodInfo Method(this Type type, string name, bool ignoreCase, params Type[] typeArguments)
            => GetAllMethods(type).FirstOrDefault(m => (ignoreCase ? m.Name.ToLower() == name.ToLower() : m.Name == name) && m.Parameters().Select(p => p.ParameterType).IsMatch(typeArguments));

        public static MethodInfo[] MethodsWithAttribute<T>(this Type type) where T : Attribute
            => GetAllMethods(type).Where(m => m.IsDefined(typeof(T), true)).ToArray();

        public static bool TryCreateDelegate<TDelegate>(this MethodInfo method, object target, out TDelegate del) where TDelegate : Delegate
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && !TypeInstanceValidator.IsValidInstance(method.DeclaringType, target, false))
                throw new ArgumentNullException(nameof(target));

            try
            {
                del = Delegate.CreateDelegate(typeof(TDelegate), target, method) as TDelegate;
                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{typeof(TDelegate).FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate(this MethodInfo method, Type delegateType, out Delegate del)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic)
                throw new ArgumentException($"Use the other overload for non-static methods!");

            try
            {
                del = Delegate.CreateDelegate(delegateType, method);
                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{delegateType.FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate(this MethodInfo method, object target, Type delegateType, out Delegate del)
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic && !TypeInstanceValidator.IsValidInstance(method.DeclaringType, target, false))
                throw new ArgumentNullException(nameof(target));

            try
            {
                del = Delegate.CreateDelegate(delegateType, target, method);
                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{delegateType.FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static bool TryCreateDelegate<TDelegate>(this MethodInfo method, out TDelegate del) where TDelegate : Delegate
        {
            if (method is null)
                throw new ArgumentNullException(nameof(method));

            if (!method.IsStatic)
                throw new ArgumentException($"Use the other overload for non-static methods!");

            try
            {
                del = Delegate.CreateDelegate(typeof(TDelegate), method) as TDelegate;
                return del != null;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to create delegate '{typeof(TDelegate).FullName}' for method '{method.ToName()}':\n{ex}");

                del = null;
                return false;
            }
        }

        public static object Call(this MethodInfo method, params object[] args)
            => InternalCall(method, null, args);

        public static object Call(this MethodInfo method, object target, params object[] args)
            => InternalCall(method, target, args);

        public static T Call<T>(this MethodInfo method, params object[] args)
            => (T)InternalCall(method, null, args);

        public static T Call<T>(this MethodInfo method, object target, params object[] args)
            => (T)InternalCall(method, target, args);

        public static object TryCall(this MethodInfo method, params object[] args)
        {
            try
            {
                return InternalCall(method, null, args);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occured while calling method '{method.ToName()}':\n{ex}");
                return null;
            }
        }

        public static object TryCall(this MethodInfo method, object target, params object[] args)
        {
            try
            {
                return InternalCall(method, target, args);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occured while calling method '{method.ToName()}':\n{ex}");
                return null;
            }
        }

        public static T TryCall<T>(this MethodInfo method, params object[] args)
        {
            try
            {
                return (T)InternalCall(method, null, args);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occured while calling method '{method.ToName()}':\n{ex}");
                return default;
            }
        }

        public static T TryCall<T>(this MethodInfo method, object target, params object[] args)
        {
            try
            {
                return (T)InternalCall(method, target, args);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occured while calling method '{method.ToName()}':\n{ex}");
                return default;
            }
        }

        public static MethodBase[] GetMethodCalls(this MethodBase method)
            => GetInstructions(method).Where(i => i.Operand != null && i.Operand is MethodBase)
                                      .Select(i => (MethodBase)i.Operand)
                                      .ToArray();

        public static Instruction[] GetInstructions(this MethodBase method)
        {
            var methodBody = method.GetMethodBody();

            if (methodBody is null)
                return Array.Empty<Instruction>();

            var methodIl = methodBody.GetILAsByteArray();
            var genericArgs = method is ConstructorInfo ? Array.Empty<Type>() : method.GetGenericArguments();
            var typeArgs = method.DeclaringType is null ? Array.Empty<Type>() : method.DeclaringType.GetGenericArguments();
            var methodParams = Parameters(method);
            var methodArgs = methodParams.Select(p => p.ParameterType).ToArray();
            var locals = methodBody.LocalVariables;
            var module = method.Module;
            var buffer = new ByteBuffer(methodIl);
            var instructions = ListPool<Instruction>.Shared.Rent();

            Instruction instruction = null;

            while (buffer.Position < buffer.Size)
            {
                var previous = instruction;

                instruction = new Instruction(buffer.Position, ReadOpCode());

                if (previous != null)
                {
                    instruction.Previous = previous;
                    previous.Next = instruction;
                }

                switch (instruction.Code.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        instruction.Operand = buffer.Position - buffer.ReadInt32();
                        break;

                    case OperandType.InlineField:
                        instruction.Operand = module.ResolveField(buffer.ReadInt32(), typeArgs, methodArgs);
                        break;

                    case OperandType.InlineI:
                        instruction.Operand = buffer.ReadInt32();
                        break;

                    case OperandType.InlineI8:
                        instruction.Operand = buffer.ReadInt64();
                        break;

                    case OperandType.InlineMethod:
                        instruction.Operand = module.ResolveMethod(buffer.ReadInt32(), typeArgs, methodArgs);
                        break;

                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlinePhi:
                        throw new NotSupportedException();

                    case OperandType.InlineR:
                        instruction.Operand = buffer.ReadDouble();
                        break;

                    case OperandType.InlineSig:
                        instruction.Operand = module.ResolveSignature(buffer.ReadInt32());
                        break;

                    case OperandType.InlineString:
                        instruction.Operand = module.ResolveString(buffer.ReadInt32());
                        break;

                    case OperandType.InlineSwitch:
                        {
                            var length = buffer.ReadInt32();

                            var branches = new int[length];
                            var offsets = new int[length];

                            for (int i = 0; i < length; i++)
                                offsets[i] = buffer.ReadInt32();

                            for (int j = 0; j < length; j++)
                                branches[j] = buffer.Position + offsets[j];

                            instruction.Operand = branches;
                            break;
                        }
                    case OperandType.InlineTok:
                        instruction.Operand = module.ResolveMember(buffer.ReadInt32(), typeArgs, methodArgs);
                        break;

                    case OperandType.InlineType:
                        instruction.Operand = module.ResolveType(buffer.ReadInt32(), typeArgs, methodArgs);
                        break;

                    case OperandType.InlineVar:
                        instruction.Operand = GetVariable(buffer.ReadInt16());
                        break;

                    case OperandType.ShortInlineBrTarget:
                        instruction.Operand = buffer.Position - ((sbyte)buffer.ReadByte());
                        break;

                    case OperandType.ShortInlineI:
                        {
                            var flag6 = instruction.Code == OpCodes.Ldc_I4_S;

                            if (flag6)
                                instruction.Operand = (sbyte)buffer.ReadByte();
                            else
                                instruction.Operand = buffer.ReadByte();

                            break;
                        }

                    case OperandType.ShortInlineR:
                        instruction.Operand = buffer.ReadSingle();
                        break;

                    case OperandType.ShortInlineVar:
                        instruction.Operand = GetVariable((int)buffer.ReadByte());
                        break;

                    default:
                        throw new NotSupportedException();
                }

                instructions.Add(instruction);
                continue;
            }

            ParameterInfo GetParameter(int index)
            {
                if (!method.IsStatic)
                    index--;

                return methodParams[index];
            }

            object GetVariable(int index)
            {
                if (instruction.Code.Name.Contains("loc"))
                    return locals[index];
                else
                    return GetParameter(index);
            }

            OpCode ReadOpCode()
            {
                var opCodeByte = buffer.ReadByte();

                if (opCodeByte == 254)
                    return TwoByteCodes[buffer.ReadByte()];
                else
                    return OneByteCodes[opCodeByte];
            }

            return ListPool<Instruction>.Shared.ToArrayReturn(instructions);
        }

        private static object InternalCall(MethodInfo method, object target, object[] args)
        {
            if (EnableLogging)
                Log.Verbose($"Calling method '{method.ToName()}' (args={args.Length} target={target?.GetType().FullName ?? "null"}");

            if (!_dynamic.TryGetValue(method, out var dynamicMethod))
                dynamicMethod = _dynamic[method] = DynamicMethod.Create(method);

            if (method.IsStatic)
                return dynamicMethod.InvokeStatic(args);
            else
                return dynamicMethod.Invoke(target, args);
        }
    }
}