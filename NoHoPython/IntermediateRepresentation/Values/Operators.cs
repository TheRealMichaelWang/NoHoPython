using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax.Parsing;
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

        public bool IsConstant => false;
        public IType Type => new BooleanType();

        public CompareOperation Operation { get; private set; }

        public IRValue Left { get; private set; }
        public IRValue Right { get; private set; }

        public ComparativeOperator(CompareOperation operation, IRValue left, IRValue right)
        {
            Operation = operation;
            Left = left;
            Right = right;

            if (left.Type is IPropertyContainer propertyContainer && !propertyContainer.HasProperty("compare"))
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

        public bool IsConstant => false;
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
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("getAtIndex")
                ? new AnonymousProcedureCall(new GetPropertyValue(array, "getAtIndex"), new List<IRValue>()
                {
                    index
                })
                : (IRValue)new GetValueAtIndex(array, index);
        }

        public bool IsConstant => false;
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
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("setAtIndex")
                ? new AnonymousProcedureCall(new GetPropertyValue(array, "setAtIndex"), new List<IRValue>()
                {
                    index,
                    value
                })
                : (IRValue)new SetValueAtIndex(array, index, value);
        }

        public bool IsConstant => false;
        public IType Type => Value.Type;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public IRValue Value { get; private set; }

        public SetValueAtIndex(IRValue array, IRValue index, IRValue value)
        {
            Array = array;

            Value = array.Type is ArrayType arrayType
                ? ArithmeticCast.CastTo(value, arrayType.ElementType)
                : throw new UnexpectedTypeException(array.Type);

            Index = ArithmeticCast.CastTo(index, Primitive.Integer);
        }
    }

    public sealed partial class GetPropertyValue : IRValue
    {
        public bool IsConstant => false;
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public Property Property { get; private set; }

        public GetPropertyValue(IRValue record, string propertyName)
        {
            Record = record;
            Property = record.Type is IPropertyContainer propertyContainer
                ? propertyContainer.FindProperty(propertyName)
                : throw new UnexpectedTypeException(record.Type);
        }
    }

    public sealed partial class SetPropertyValue : IRValue, IRStatement
    {
        public bool IsConstant => false;
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

namespace NoHoPython.Syntax.Values
{
    partial class BinaryOperator
    {
        private static Dictionary<TokenType, ArithmeticOperator.ArithmeticOperation> ArithmeticTokens = new()
        {
            {TokenType.Add, ArithmeticOperator.ArithmeticOperation.Add},
            {TokenType.Subtract, ArithmeticOperator.ArithmeticOperation.Subtract},
            {TokenType.Multiply, ArithmeticOperator.ArithmeticOperation.Multiply},
            {TokenType.Divide, ArithmeticOperator.ArithmeticOperation.Divide},
            {TokenType.Modulo, ArithmeticOperator.ArithmeticOperation.Modulo},
            {TokenType.Caret, ArithmeticOperator.ArithmeticOperation.Exponentiate},
        };

        private static Dictionary<TokenType, ComparativeOperator.CompareOperation> ComparativeTokens = new()
        {
            {TokenType.More, ComparativeOperator.CompareOperation.More},
            {TokenType.Less, ComparativeOperator.CompareOperation.Less},
            {TokenType.MoreEqual, ComparativeOperator.CompareOperation.MoreEqual},
            {TokenType.LessEqual, ComparativeOperator.CompareOperation.LessEqual},
            {TokenType.Equals, ComparativeOperator.CompareOperation.Equals},
            {TokenType.NotEquals, ComparativeOperator.CompareOperation.NotEquals}
        };

        private static Dictionary<TokenType, LogicalOperator.LogicalOperation> LogicalTokens = new()
        {
            {TokenType.And, LogicalOperator.LogicalOperation.And},
            {TokenType.Or, LogicalOperator.LogicalOperation.Or }
        };

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType)
        {
            return ArithmeticTokens.ContainsKey(Operator)
                ? ArithmeticOperator.ComposeArithmeticOperation(ArithmeticTokens[Operator], Left.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Right.GenerateIntermediateRepresentationForValue(irProgramBuilder, null))
                : ComparativeTokens.ContainsKey(Operator)
                ? new ComparativeOperator(ComparativeTokens[Operator], Left.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Right.GenerateIntermediateRepresentationForValue(irProgramBuilder, null))
                : LogicalTokens.ContainsKey(Operator)
                ? (IRValue)new LogicalOperator(LogicalTokens[Operator], Left.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Right.GenerateIntermediateRepresentationForValue(irProgramBuilder, null))
                : throw new InvalidOperationException();
        }
    }

    partial class GetValueAtIndex
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => IntermediateRepresentation.Values.GetValueAtIndex.ComposeGetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irProgramBuilder, expectedType == null ? null : new ArrayType(expectedType)), Index.GenerateIntermediateRepresentationForValue(irProgramBuilder, Primitive.Integer));
    }

    partial class SetValueAtIndex
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => IntermediateRepresentation.Values.SetValueAtIndex.ComposeSetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irProgramBuilder, expectedType == null ? null : new ArrayType(expectedType)), Index.GenerateIntermediateRepresentationForValue(irProgramBuilder, Primitive.Integer), Value.GenerateIntermediateRepresentationForValue(irProgramBuilder, expectedType));
    }

    partial class GetPropertyValue
    {

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => new IntermediateRepresentation.Values.GetPropertyValue(Record.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Property);
    }

    partial class SetPropertyValue
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => new IntermediateRepresentation.Values.SetPropertyValue(Record.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Property, Value.GenerateIntermediateRepresentationForValue(irProgramBuilder, null));
    }
}