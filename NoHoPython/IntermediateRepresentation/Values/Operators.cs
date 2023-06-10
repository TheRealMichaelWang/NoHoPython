using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public abstract partial class BinaryOperator : IRValue
    {
        public abstract IType Type { get; }
        public virtual bool IsTruey => false;
        public virtual bool IsFalsey => false;

        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Left { get; protected set; }
        public IRValue Right { get; protected set; }

        public bool ShortCircuit { get; protected set; }

        public BinaryOperator(IRValue left, IRValue right, bool shortCuircuit, IAstElement errorReportedElement)
        {
            Left = left;
            Right = right;
            ShortCircuit = shortCuircuit;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class ComparativeOperator : BinaryOperator
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

        public override IType Type => new BooleanType();

        public CompareOperation Operation { get; private set; }

        public ComparativeOperator(CompareOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(left, right, false, errorReportedElement)
        {
            Operation = operation;

            if (left.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("compare"))
            {
                Left = ArithmeticCast.CastTo(AnonymousProcedureCall.ComposeCall(new GetPropertyValue(left, "compare", errorReportedElement), new List<IRValue>()
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

    public sealed partial class LogicalOperator : BinaryOperator
    {
        public enum LogicalOperation
        {
            And,
            Or
        }

        public override IType Type => Primitive.Boolean;
        public override bool IsTruey => (Operation == LogicalOperation.And ? Left.IsTruey && Right.IsTruey : Left.IsTruey || Right.IsTruey);
        public override bool IsFalsey => (Operation == LogicalOperation.And ? Left.IsFalsey || Right.IsFalsey : Left.IsFalsey && Right.IsFalsey);

        public LogicalOperation Operation { get; private set; }

        public LogicalOperator(LogicalOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(ArithmeticCast.CastTo(left, Primitive.Boolean), ArithmeticCast.CastTo(right, Primitive.Boolean), true, errorReportedElement)
        {
            Operation = operation;
        }
    }

    public sealed partial class BitwiseOperator : BinaryOperator
    {
        public enum BitwiseOperation
        {
            And,
            Or,
            Xor,
            ShiftLeft,
            ShiftRight
        }

        public override IType Type => Primitive.Boolean;

        public BitwiseOperation Operation { get; private set; }

        public BitwiseOperator(BitwiseOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(ArithmeticCast.CastTo(left, Primitive.Integer), ArithmeticCast.CastTo(right, Primitive.Integer), false, errorReportedElement)
        {
            Operation = operation;
        }
    }

    public sealed partial class GetValueAtIndex : IRValue
    {
        public static IRValue ComposeGetValueAtIndex(IRValue array, IRValue index, IType? expectedType, IAstElement errorReportedElement)
        {
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("getAtIndex")
                ? AnonymousProcedureCall.ComposeCall(new GetPropertyValue(array, "getAtIndex", errorReportedElement), new List<IRValue>()
                {
                    index
                }, array.ErrorReportedElement)
                : array.Type is HandleType handleType
                ? new MemoryGet(handleType.ValueType is not NothingType ? handleType.ValueType : expectedType ?? throw new UnexpectedTypeException(Primitive.Nothing, errorReportedElement), array, index, errorReportedElement)
                : new GetValueAtIndex(array, index, errorReportedElement);
        }

        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        private GetValueAtIndex(IRValue array, IRValue index, IAstElement errorReportedElement)
        {
            if (array.Type is ArrayType arrayType)
                Type = arrayType.ElementType;
            else if (array.Type is MemorySpan memorySpan)
                Type = memorySpan.ElementType;
            else
                throw new UnexpectedTypeException(array.Type, errorReportedElement);

            Array = array;
            Index = ArithmeticCast.CastTo(index, Primitive.Integer);
            ErrorReportedElement = errorReportedElement;
        }
    }
    
    public sealed partial class SetValueAtIndex : IRValue, IRStatement
    {
        public static IRValue ComposeSetValueAtIndex(IRValue array, IRValue index, IRValue value, IAstElement errorReportedElement)
        {
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("setAtIndex")
                ? AnonymousProcedureCall.ComposeCall(new GetPropertyValue(array, "setAtIndex", errorReportedElement), new List<IRValue>()
                {
                    index,
                    value
                }, array.ErrorReportedElement)
                : array.Type is HandleType handleType
                ? new MemorySet(handleType.ValueType is NothingType ? value.Type : handleType.ValueType, array, index, handleType.ValueType is NothingType ? value : ArithmeticCast.CastTo(value, handleType.ValueType), errorReportedElement)
                : new SetValueAtIndex(array, index, value, errorReportedElement);
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => Value.Type;
        public bool IsTruey => Value.IsTruey;
        public bool IsFalsey => Value.IsFalsey;

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
        public bool IsTruey => false;
        public bool IsFalsey => false;

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
        public bool IsTruey => Value.IsTruey;
        public bool IsFalsey => Value.IsFalsey;

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
                if (!propertyContainer.HasProperty(propertyName))
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
    partial class SizeofOperator
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.SizeofOperator(TypeToMeasure.ToIRType(irBuilder, this), this);
    }

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

        private static Dictionary<TokenType, BitwiseOperator.BitwiseOperation> BitwiseTokens = new()
        {
            {TokenType.BitAnd, BitwiseOperator.BitwiseOperation.And},
            {TokenType.BitOr, BitwiseOperator.BitwiseOperation.Or},
            {TokenType.BitXor, BitwiseOperator.BitwiseOperation.Xor},
            {TokenType.ShiftLeft, BitwiseOperator.BitwiseOperation.ShiftLeft},
            {TokenType.ShiftRight, BitwiseOperator.BitwiseOperation.ShiftRight}
        };

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IRValue lhs = Left.GenerateIntermediateRepresentationForValue(irBuilder, LogicalTokens.ContainsKey(Operator) ? Primitive.Boolean : BitwiseTokens.ContainsKey(Operator) ? Primitive.Integer : null, willRevaluate);

            return ArithmeticTokens.ContainsKey(Operator)
                ? ArithmeticOperator.ComposeArithmeticOperation(ArithmeticTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, lhs.Type, willRevaluate), this)
                : ComparativeTokens.ContainsKey(Operator)
                ? new ComparativeOperator(ComparativeTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, lhs.Type, willRevaluate), this)
                : LogicalTokens.ContainsKey(Operator)
                ? new LogicalOperator(LogicalTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, willRevaluate), this)
                : BitwiseTokens.ContainsKey(Operator)
                ? new BitwiseOperator(BitwiseTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), this)
                : throw new InvalidOperationException();
        }
    }

    partial class GetValueAtIndex
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => IntermediateRepresentation.Values.GetValueAtIndex.ComposeGetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irBuilder, expectedType == null ? null : new ArrayType(expectedType), willRevaluate), Index.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), expectedType, this);
    }

    partial class SetValueAtIndex
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => IntermediateRepresentation.Values.SetValueAtIndex.ComposeSetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irBuilder, expectedType == null ? null : new ArrayType(expectedType), willRevaluate), Index.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), Value.GenerateIntermediateRepresentationForValue(irBuilder, expectedType, willRevaluate), this);
    }

    partial class GetPropertyValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.GetPropertyValue(Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), Property, this);
    }

    partial class SetPropertyValue
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.SetPropertyValue(Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), Property, Value.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), this);
    }
}