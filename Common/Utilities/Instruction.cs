using System.Reflection.Emit;

namespace Common.Utilities
{
    public class Instruction
    {
        private int _offset;
        private object _operand;

        private OpCode _opCode;

        private Instruction _previous;
        private Instruction _next;

        public int Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public object Operand
        {
            get => _operand;
            set => _operand = value;
        }

        public OpCode Code
        {
            get => _opCode;
            set => _opCode = value;
        }

        public Instruction Previous
        {
            get => _previous;
            set => _previous = value;
        }

        public Instruction Next
        {
            get => _next;
            set => _next = value;
        }

        public Instruction(int offset, OpCode opCode, object operand)
            : this(offset, opCode)
            => _operand = operand;

        public Instruction(int offset, OpCode opCode)
        {
            _offset = offset;
            _opCode = opCode;
        }

        public Instruction(OpCode opCode, object operand)
            : this(0, opCode, operand) { }

        public Instruction(OpCode opCode)
            : this(0, opCode) { }

        public int GetSize()
        {
            var size = _opCode.Size;

            switch (_opCode.OperandType)
            {
                case OperandType.InlineBrTarget:
                case OperandType.InlineField:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineString:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    size += 4;
                    break;

                case OperandType.InlineI8:
                case OperandType.InlineR:
                    size += 8;
                    break;

                case OperandType.InlineSwitch:
                    size += (1 + ((int[])_operand).Length) * 4;
                    break;

                case OperandType.InlineVar:
                    size += 2;
                    break;

                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    size++;
                    break;
            }

            return size;
        }

        public override string ToString()
            => _opCode.Name;
    }
}
