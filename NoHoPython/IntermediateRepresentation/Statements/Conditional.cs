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
            Condition = condition;
            IfTrueBlock = ifTrueBlock;
            IfFalseBlock = ifFalseBlock;

            if (!Primitive.Boolean.IsCompatibleWith(condition.Type))
                throw new UnexpectedTypeException(Primitive.Boolean, condition.Type);
        }
    }

    public sealed class IfBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueblock { get; private set; }

        public IfBlock(IRValue condition, CodeBlock ifTrueblock)
        {
            Condition = condition;
            IfTrueblock = ifTrueblock;

            if (!Primitive.Boolean.IsCompatibleWith(condition.Type))
                throw new UnexpectedTypeException(Primitive.Boolean, condition.Type);
        }
    }

    public sealed class WhileBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock whileTrueBlock { get; private set; }

        public WhileBlock(IRValue condition, CodeBlock whileTrueBlock)
        {
            Condition = condition;
            this.whileTrueBlock = whileTrueBlock;

            if (!Primitive.Boolean.IsCompatibleWith(condition.Type))
                throw new UnexpectedTypeException(Primitive.Boolean, condition.Type);
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
            Condition = condition;
            IfTrueValue = ifTrueValue;
            IfFalseValue = ifFalseValue;

            if (!Primitive.Boolean.IsCompatibleWith(condition.Type))
                throw new UnexpectedTypeException(Primitive.Boolean, condition.Type);
            else if (!Type.IsCompatibleWith(IfTrueValue.Type))
                throw new UnexpectedTypeException(Type, IfTrueValue.Type);
            else if (!Type.IsCompatibleWith(IfFalseValue.Type))
                throw new UnexpectedTypeException(Type, IfFalseValue.Type);
        }
    }
}