using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class ComparativeOperator : IRValue
    {
        public enum CompareOperation
        {
            Equals,
            NotEquals,
            More,
            Less,
            MoreEqual,
            LessEqual
        }

        public IType Type => new BooleanType();

        public CompareOperation Operation { get; private set; }

        public IRValue Left { get; private set; }
        public IRValue Right { get; private set; }

        private bool isFinal;

        public ComparativeOperator(CompareOperation operation, IRValue left, IRValue right)
        {
            Operation = operation;
            Left = left;
            Right = right;

            if(left.Type is RecordType)
            {
                
            }
            else
            {
                if (left.Type is not Primitive)
                    throw new UnexpectedTypeException(left.Type);
                if (right.Type is not Primitive)
                    throw new UnexpectedTypeException(right.Type);
            }
        }
    }

    public sealed partial class LogicalOperator : IRValue
    {
        public enum LogicalOperation
        {
            And,
            Or
        }

        public IType Type => new BooleanType();

        public LogicalOperation Operation { get; private set; }

        public IRValue Right { get; private set; }
        public IRValue Left { get; private set; }

        public LogicalOperator(LogicalOperation operation, IRValue right, IRValue left)
        {
            Operation = operation;
            Right = right;
            Left = left;

            if (!Primitive.Boolean.IsCompatibleWith(Left.Type))
                throw new UnexpectedTypeException(Primitive.Boolean, Left.Type);
            if (!Primitive.Boolean.IsCompatibleWith(Right.Type))
                throw new UnexpectedTypeException(Primitive.Boolean, Right.Type);
        }
    }

    public sealed partial class GetValueAtIndex : IRValue
    {
        public IType Type { get; private set; }

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public GetValueAtIndex(IRValue array, IRValue index)
        {
            Array = array;
            Index = index;

            if (Array.Type is not ArrayType)
                throw new UnexpectedTypeException(Array.Type);

            if (Index.Type is not IntegerType)
                throw new UnexpectedTypeException(Array.Type);
            Type = ((ArrayType)Array.Type).ElementType;
        }
    }

    public sealed partial class SetValueAtIndex : IRValue
    {
        public IType Type => Value.Type;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public IRValue Value { get; private set; }

        public SetValueAtIndex(IRValue array, IRValue index, IRValue value)
        {
            Array = array;
            Index = index;
            Value = value;

            if (Array.Type is not ArrayType)
                throw new UnexpectedTypeException(Array.Type);
            if (Index.Type is not IntegerType)
                throw new UnexpectedTypeException(Array.Type);

            if (!((ArrayType)Array.Type).ElementType.IsCompatibleWith(value.Type))
                throw new UnexpectedTypeException(Value.Type);
        }
    }

    public sealed partial class GetPropertyValue : IRValue
    {
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public RecordDeclaration.RecordProperty Property { get; private set; }

        public GetPropertyValue(IRValue record, RecordDeclaration.RecordProperty property)
        {
            Record = record;
            Property = property;
        }
    }

    public sealed partial class SetPropertyValue : IRValue
    {
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public RecordDeclaration.RecordProperty Property { get; private set; }

        public IRValue Value { get; private set; }

        public SetPropertyValue(IRValue record, RecordDeclaration.RecordProperty property, IRValue value)
        {
            Record = record;
            Property = property;
            Value = value;

            if (Property.IsReadonly)
                throw new CannotMutateReadonlyPropertyException(Property);
            if (!Property.Type.IsCompatibleWith(Value.Type))
                throw new UnexpectedTypeException(Property.Type, Value.Type);
        }
    }
}
