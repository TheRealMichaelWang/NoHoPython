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

        public ComparativeOperator(CompareOperation operation, IRValue left, IRValue right)
        {
            Operation = operation;
            Left = left;
            Right = right;

            if(left.Type is IPropertyContainer propertyContainer && !propertyContainer.HasProperty("compare"))
            {
                Left = ArithmeticCast.CastTo(new AnonymousProcedureCall(new GetPropertyValue(left, "compare"), new List<IRValue>()
                {
                    right
                }), Primitive.Integer);
                Right = new IntegerLiteral(0);
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
            Right = ArithmeticCast.CastTo(right, Primitive.Boolean);
            Left = ArithmeticCast.CastTo(left, Primitive.Boolean);
        }
    }

    public sealed partial class GetValueAtIndex : IRValue
    {
        public static IRValue ComposeGetValueAtIndex(IRValue array, IRValue index)
        {
            if (array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("getAtIndex"))
                return new AnonymousProcedureCall(new GetPropertyValue(array, "getAtIndex"), new List<IRValue>()
                {
                    index
                });
            else
                return new GetValueAtIndex(array, index);
        }

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

    public sealed partial class SetValueAtIndex : IRValue, IRStatement
    {
        public static IRValue ComposeSetValueAtIndex(IRValue array, IRValue index, IRValue value)
        {
            if (array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("setAtIndex"))
                return new AnonymousProcedureCall(new GetPropertyValue(array, "setAtIndex"), new List<IRValue>()
                {
                    index,
                    value
                });
            else
                return new SetValueAtIndex(array, index, value);
        }

        public IType Type => Value.Type;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public IRValue Value { get; private set; }

        public SetValueAtIndex(IRValue array, IRValue index, IRValue value)
        {
            Array = array;

            if (array.Type is ArrayType arrayType)
                Value = ArithmeticCast.CastTo(value, arrayType.ElementType);
            else
                throw new UnexpectedTypeException(array.Type);

            Index = ArithmeticCast.CastTo(index, Primitive.Integer);
        }
    }

    public sealed partial class GetPropertyValue : IRValue
    {
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public Property Property { get; private set; }

        public GetPropertyValue(IRValue record, string propertyName)
        {
            Record = record;
            if (record.Type is IPropertyContainer propertyContainer)
                Property = propertyContainer.FindProperty(propertyName);
            else
                throw new UnexpectedTypeException(record.Type);
        }
    }

    public sealed partial class SetPropertyValue : IRValue, IRStatement
    {
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public Property Property { get; private set; }

        public IRValue Value { get; private set; }

        public SetPropertyValue(IRValue record, string propertyName, IRValue value)
        {
            Record = record;

            if (record.Type is IPropertyContainer propertyContainer)
            {
                Property = propertyContainer.FindProperty(propertyName);

                if (Property.IsReadOnly)
                    throw new CannotMutateReadonlyPropertyException(Property);
                Value = ArithmeticCast.CastTo(value, Property.Type);
            }
            else
                throw new UnexpectedTypeException(record.Type);
        }
    }
}
