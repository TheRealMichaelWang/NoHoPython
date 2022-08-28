using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class IfElseBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueBlock { get; private set; }
        public CodeBlock IfFalseBlock { get; private set; }

        public IfElseBlock(IRValue condition, CodeBlock ifTrueBlock, CodeBlock ifFalseBlock)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueBlock = ifTrueBlock;
            IfFalseBlock = ifFalseBlock;
        }
    }

    public sealed class IfBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueblock { get; private set; }

        public IfBlock(IRValue condition, CodeBlock ifTrueblock)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueblock = ifTrueblock;
        }
    }

    public sealed class WhileBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock whileTrueBlock { get; private set; }

        public WhileBlock(IRValue condition, CodeBlock whileTrueBlock)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            this.whileTrueBlock = whileTrueBlock;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IfElseValue : IRValue
    {
        public IType Type { get; private set; }

        public IRValue Condition { get; private set; }
        public IRValue IfTrueValue { get; private set; }
        public IRValue IfFalseValue { get; private set; }

        public IfElseValue(IType type, IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue)
        {
            Type = type;
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueValue = ArithmeticCast.CastTo(ifTrueValue, Type);
            IfFalseValue = ArithmeticCast.CastTo(ifFalseValue, Type);
        }
    }
}