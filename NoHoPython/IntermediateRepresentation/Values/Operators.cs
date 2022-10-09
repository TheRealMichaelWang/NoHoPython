using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
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

        public IType Type => new BooleanType();

        public IAstElement ErrorReportedElement { get; private set; }

        public CompareOperation Operation { get; private set; }

        public IRValue Left { get; private set; }
        public IRValue Right { get; private set; }

        public ComparativeOperator(CompareOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement)
        {
            Operation = operation;
            Left = left;
            Right = right;
            ErrorReportedElement = errorReportedElement;

            if (left.Type is IPropertyContainer propertyContainer && !propertyContainer.HasProperty("compare"))
            {
                Left = ArithmeticCast.CastTo(new AnonymousProcedureCall(new GetPropertyValue(left, "compare", errorReportedElement), new List<IRValue>()
                {
                    right
                }, errorReportedElement), Primitive.Integer);
                Right = new IntegerLiteral(0, errorReportedElement);
            }
            else
            {
                if (left.Type is not Primitive)
                    throw new UnexpectedTypeException(left.Type, left.ErrorReportedElement);
                if (right.Type is not Primitive)
                    throw new UnexpectedTypeException(right.Type, right.ErrorReportedElement);
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

        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => new BooleanType();

        public LogicalOperation Operation { get; private set; }

        public IRValue Right { get; private set; }
        public IRValue Left { get; private set; }

        public LogicalOperator(LogicalOperation operation, IRValue right, IRValue left, IAstElement errorReportedElement)
        {
            Operation = operation;
            ErrorReportedElement = errorReportedElement;
            Right = ArithmeticCast.CastTo(right, Primitive.Boolean);
            Left = ArithmeticCast.CastTo(left, Primitive.Boolean);
        }
    }

    public sealed partial class GetValueAtIndex : IRValue
    {
        public static IRValue ComposeGetValueAtIndex(IRValue array, IRValue index, Syntax.Values.GetValueAtIndex getValueAtIndex)
        {
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("getAtIndex")
                ? new AnonymousProcedureCall(new GetPropertyValue(array, "getAtIndex", getValueAtIndex), new List<IRValue>()
                {
                    index
                }, array.ErrorReportedElement)
                : (IRValue)new GetValueAtIndex(array, index, getValueAtIndex);
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get; private set; }

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public GetValueAtIndex(IRValue array, IRValue index, IAstElement errorReportedElement)
        {
            Array = array;
            Index = index;
            ErrorReportedElement = errorReportedElement;

            if (Array.Type is not ArrayType)
                throw new UnexpectedTypeException(Array.Type, Array.ErrorReportedElement);

            if (Index.Type is not IntegerType)
                throw new UnexpectedTypeException(Array.Type, Array.ErrorReportedElement);
            Type = ((ArrayType)Array.Type).ElementType;
        }
    }
    
    public sealed partial class SetValueAtIndex : IRValue, IRStatement
    {
        public static IRValue ComposeSetValueAtIndex(IRValue array, IRValue index, IRValue value, Syntax.Values.SetValueAtIndex setValueAtIndex)
        {
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("setAtIndex")
                ? new AnonymousProcedureCall(new GetPropertyValue(array, "setAtIndex", setValueAtIndex), new List<IRValue>()
                {
                    index,
                    value
                }, array.ErrorReportedElement)
                : (IRValue)new SetValueAtIndex(array, index, value, setValueAtIndex);
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => Value.Type;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public IRValue Value { get; private set; }

        private SetValueAtIndex(IRValue array, IRValue index, IRValue value, IAstElement errorReportedElement)
        {
            Array = array;
            ErrorReportedElement = errorReportedElement;

            Value = array.Type is ArrayType arrayType
                ? ArithmeticCast.CastTo(value, arrayType.ElementType)
                : throw new UnexpectedTypeException(array.Type, array.ErrorReportedElement);

            Index = ArithmeticCast.CastTo(index, Primitive.Integer);
        }
    }

    public sealed partial class GetPropertyValue : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public Property Property { get; private set; }

        public GetPropertyValue(IRValue record, string propertyName, IAstElement errorReportedElement)
        {
            Record = record;
            Property = record.Type is IPropertyContainer propertyContainer
				? (propertyContainer.HasProperty(propertyName) ? propertyContainer.FindProperty(propertyName) : throw new UnexpectedTypeException(record.Type, errorReportedElement))
                : record.Type is TypeParameterReference typeParameter
                ? typeParameter.TypeParameter.RequiredImplementedInterface == null 
                ? throw new UnexpectedTypeException(record.Type, errorReportedElement)
                : (typeParameter.TypeParameter.RequiredImplementedInterface.HasProperty(propertyName) ?typeParameter.TypeParameter.RequiredImplementedInterface.FindProperty(propertyName) : throw new UnexpectedTypeException(record.Type, errorReportedElement))
                : throw new UnexpectedTypeException(record.Type, errorReportedElement);
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class SetPropertyValue : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Property.Type;

        public IRValue Record { get; private set; }
        public RecordDeclaration.RecordProperty Property { get; private set; }

        public IRValue Value { get; private set; }

        public SetPropertyValue(IRValue record, string propertyName, IRValue value, IAstElement errorReportedElement)
        {
            Record = record;
            ErrorReportedElement = errorReportedElement;
            IsInitializingProperty = false;

            if (record.Type is RecordType propertyContainer)
            {
				if(!propertyContainer.HasProperty(propertyName))
                	throw new UnexpectedTypeException(record.Type, errorReportedElement);
				
                Property = (RecordDeclaration.RecordProperty)propertyContainer.FindProperty(propertyName);
                Value = ArithmeticCast.CastTo(value, Property.Type);
            }
            else
                throw new UnexpectedTypeException(record.Type, errorReportedElement);
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
                ? ArithmeticOperator.ComposeArithmeticOperation(ArithmeticTokens[Operator], Left.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Right.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), this)
                : ComparativeTokens.ContainsKey(Operator)
                ? new ComparativeOperator(ComparativeTokens[Operator], Left.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Right.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), this)
                : LogicalTokens.ContainsKey(Operator)
                ? (IRValue)new LogicalOperator(LogicalTokens[Operator], Left.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Right.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), this)
                : throw new InvalidOperationException();
        }
    }

    partial class GetValueAtIndex
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => IntermediateRepresentation.Values.GetValueAtIndex.ComposeGetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irProgramBuilder, expectedType == null ? null : new ArrayType(expectedType)), Index.GenerateIntermediateRepresentationForValue(irProgramBuilder, Primitive.Integer), this);
    }

    partial class SetValueAtIndex
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => IntermediateRepresentation.Values.SetValueAtIndex.ComposeSetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irProgramBuilder, expectedType == null ? null : new ArrayType(expectedType)), Index.GenerateIntermediateRepresentationForValue(irProgramBuilder, Primitive.Integer), Value.GenerateIntermediateRepresentationForValue(irProgramBuilder, expectedType), this);
    }

    partial class GetPropertyValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => new IntermediateRepresentation.Values.GetPropertyValue(Record.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Property, this);
    }

    partial class SetPropertyValue
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => new IntermediateRepresentation.Values.SetPropertyValue(Record.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), Property, Value.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), this);
    }
}