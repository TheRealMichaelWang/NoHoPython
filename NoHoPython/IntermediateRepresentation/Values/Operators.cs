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

        public BinaryOperator(IRValue left, IRValue right, IAstElement errorReportedElement)
        {
            Left = left;
            Right = right;
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

        public ComparativeOperator(CompareOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
        {
            Operation = operation;

            if (left.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("compare"))
            {
                Left = ArithmeticCast.CastTo(AnonymousProcedureCall.ComposeCall(GetPropertyValue.ComposeGetProperty(left, "compare", irBuilder, errorReportedElement), new List<IRValue>()
                {
                    right
                }, irBuilder, errorReportedElement), Primitive.Integer, irBuilder);
                Right = new IntegerLiteral(0, errorReportedElement);
            }
            else
            {
                if (left.Type is not Primitive && !(left.Type is ForeignCType foreignCType && foreignCType.Declaration.PointerPropertyAccess))
                    throw new UnexpectedTypeException(left.Type, left.ErrorReportedElement);
                if (right.Type is not Primitive && !(right.Type is ForeignCType rightForeignCType && rightForeignCType.Declaration.PointerPropertyAccess))
                    throw new UnexpectedTypeException(right.Type, right.ErrorReportedElement);
            }
        }

        private ComparativeOperator(CompareOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
        {
            Operation = operation;
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

        public LogicalOperator(LogicalOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(ArithmeticCast.CastTo(left, Primitive.Boolean, irBuilder), ArithmeticCast.CastTo(right, Primitive.Boolean, irBuilder), errorReportedElement)
        {
            Operation = operation;
        }

        private LogicalOperator(LogicalOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
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

        public BitwiseOperator(BitwiseOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(ArithmeticCast.CastTo(left, Primitive.Integer, irBuilder), ArithmeticCast.CastTo(right, Primitive.Integer, irBuilder), errorReportedElement)
        {
            Operation = operation;
        }

        private BitwiseOperator(BitwiseOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
        {
            Operation = operation;
        }
    }

    public sealed partial class GetValueAtIndex : IRValue
    {
        public static IRValue ComposeGetValueAtIndex(IRValue array, IRValue index, IType? expectedType, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("getAtIndex")
                ? AnonymousProcedureCall.ComposeCall(GetPropertyValue.ComposeGetProperty(array, "getAtIndex", irBuilder, errorReportedElement), new List<IRValue>()
                {
                    index
                }, irBuilder, array.ErrorReportedElement)
                : array.Type is HandleType handleType
                ? new MemoryGet(handleType.ValueType is not NothingType ? handleType.ValueType : expectedType ?? throw new UnexpectedTypeException(Primitive.Nothing, errorReportedElement), array, index, irBuilder, errorReportedElement)
                : new GetValueAtIndex(array, index, irBuilder, errorReportedElement);
        }

        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        private GetValueAtIndex(IRValue array, IRValue index, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (array.Type is ArrayType arrayType)
                Type = arrayType.ElementType;
            else if (array.Type is MemorySpan memorySpan)
                Type = memorySpan.ElementType;
            else
                throw new UnexpectedTypeException(array.Type, errorReportedElement);

            Array = array;
            Index = ArithmeticCast.CastTo(index, Primitive.Integer, irBuilder);
            ErrorReportedElement = errorReportedElement;
        }

        public GetValueAtIndex(IType type, IRValue array, IRValue index, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Type = type;
            Array = array;
            Index = index;
        }
    }
    
    public sealed partial class SetValueAtIndex : IRValue, IRStatement
    {
        public static IRValue ComposeSetValueAtIndex(IRValue array, IRValue index, IRValue value, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            return array.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty("setAtIndex")
                ? AnonymousProcedureCall.ComposeCall(GetPropertyValue.ComposeGetProperty(array, "setAtIndex", irBuilder, errorReportedElement), new List<IRValue>()
                {
                    index,
                    value
                }, irBuilder, array.ErrorReportedElement)
                : array.Type is HandleType handleType
                ? new MemorySet(handleType.ValueType is NothingType ? value.Type : handleType.ValueType, array, index, handleType.ValueType is NothingType ? value : ArithmeticCast.CastTo(value, handleType.ValueType, irBuilder), irBuilder, errorReportedElement)
                : new SetValueAtIndex(array, index, value, irBuilder, errorReportedElement);
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => Value.Type;
        public bool IsTruey => Value.IsTruey;
        public bool IsFalsey => Value.IsFalsey;

        public IRValue Array { get; private set; }
        public IRValue Index { get; private set; }

        public IRValue Value { get; private set; }

        private SetValueAtIndex(IRValue array, IRValue index, IRValue value, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Array = array;
            ErrorReportedElement = errorReportedElement;

            Value = array.Type is ArrayType arrayType
                ? ArithmeticCast.CastTo(value, arrayType.ElementType, irBuilder)
                : throw new UnexpectedTypeException(array.Type, array.ErrorReportedElement);

            Index = ArithmeticCast.CastTo(index, Primitive.Integer, irBuilder);
        }

        private SetValueAtIndex(IRValue array, IRValue index, IRValue value, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Array = array;
            Index = index;
            Value = value;
        }
    }

    public sealed partial class GetPropertyValue : IRValue
    {
        public static bool HasMessageReceiver(IRValue value, string name, AstIRProgramBuilder irBuilder)
        {
            if(value.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty(name))
                return true;
            
            IScopeSymbol? procedure = irBuilder.SymbolMarshaller.FindSymbol($"{value.Type.Identifier}_{name}");
            if(procedure == null)
                procedure = irBuilder.SymbolMarshaller.FindSymbol($"{value.Type.PrototypeIdentifier}_{name}");
            
            return procedure != null && procedure is ProcedureDeclaration;
        }

        public static GetPropertyValue ComposeGetProperty(IRValue record, string propertyName, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) => new GetPropertyValue(record, propertyName, record.GetRefinementEntry(irBuilder)?.GetSubentry(propertyName)?.Refinement, errorReportedElement);

        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Refinements.HasValue ? Refinements.Value.Item1 : Property.Type;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue Record { get; private set; }
        public Property Property { get; private set; }

        public (IType, CodeBlock.RefinementEmitter?)? Refinements { get; private set; }

        public GetPropertyValue(IRValue record, string propertyName, (IType, CodeBlock.RefinementEmitter?)? refinements, IAstElement errorReportedElement)
        {
            Record = record;
            Refinements = refinements;
            Property = record.Type is IPropertyContainer propertyContainer
				? (propertyContainer.HasProperty(propertyName) ? propertyContainer.FindProperty(propertyName) : throw new UnexpectedTypeException(record.Type, $"Type doesn't contain property {propertyName}.", errorReportedElement))
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

        public SetPropertyValue(IRValue record, string propertyName, IRValue value, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Record = record;
            ErrorReportedElement = errorReportedElement;
            IsInitializingProperty = false;

            if (record.Type is RecordType propertyContainer)
            {
                if (!propertyContainer.HasProperty(propertyName))
                    throw new UnexpectedTypeException(record.Type, errorReportedElement);

                Property = (RecordDeclaration.RecordProperty)propertyContainer.FindProperty(propertyName);
                Value = ArithmeticCast.CastTo(value, Property.Type, irBuilder);
            }
            else
                throw new UnexpectedTypeException(record.Type, errorReportedElement);
        }

        private SetPropertyValue(IRValue record, RecordDeclaration.RecordProperty property, IRValue value, IAstElement errorReportedElement)
        {
            Record = record;
            Property = property;
            Value = value;
            ErrorReportedElement = errorReportedElement;
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
                ? ArithmeticOperator.ComposeArithmeticOperation(ArithmeticTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, lhs.Type, willRevaluate), irBuilder, this)
                : ComparativeTokens.ContainsKey(Operator)
                ? new ComparativeOperator(ComparativeTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, lhs.Type, willRevaluate), irBuilder, this)
                : LogicalTokens.ContainsKey(Operator)
                ? new LogicalOperator(LogicalTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, willRevaluate), irBuilder, this)
                : BitwiseTokens.ContainsKey(Operator)
                ? new BitwiseOperator(BitwiseTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), irBuilder, this)
                : throw new InvalidOperationException();
        }
    }

    partial class GetValueAtIndex
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => IntermediateRepresentation.Values.GetValueAtIndex.ComposeGetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irBuilder, expectedType == null ? null : new ArrayType(expectedType), willRevaluate), Index.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), expectedType, irBuilder, this);
    }

    partial class SetValueAtIndex
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => IntermediateRepresentation.Values.SetValueAtIndex.ComposeSetValueAtIndex(Array.GenerateIntermediateRepresentationForValue(irBuilder, expectedType == null ? null : new ArrayType(expectedType), willRevaluate), Index.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), Value.GenerateIntermediateRepresentationForValue(irBuilder, expectedType, willRevaluate), irBuilder, this);
    }

    partial class GetPropertyValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IRValue record = Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);

            if (record.Type is IPropertyContainer property)
            {
                if (property.HasProperty($"get_{Property}"))
                    return AnonymousProcedureCall.ComposeCall(IntermediateRepresentation.Values.GetPropertyValue.ComposeGetProperty(record, $"get_{Property}", irBuilder, this), new(), irBuilder, this);
                else
                    return IntermediateRepresentation.Values.GetPropertyValue.ComposeGetProperty(record, Property, irBuilder, this);
            }
            else
                throw new UnexpectedTypeException(record.Type, "Expected a property container type.", record.ErrorReportedElement);
        }
    }

    partial class SetPropertyValue
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IRValue record = Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);

            IType? hintType = expectedType;
            if (record is IPropertyContainer propertyContainer && !propertyContainer.HasProperty(Property) && propertyContainer.HasProperty($"set_{Property}"))
            {
                IRValue getter = IntermediateRepresentation.Values.GetPropertyValue.ComposeGetProperty(record, $"set_property", irBuilder, this);
                if (getter.Type is ProcedureType procedureType && procedureType.ParameterTypes.Count == 1)
                    hintType = procedureType.ParameterTypes[0];
                return AnonymousProcedureCall.ComposeCall(getter, new List<IRValue>()
                {
                    Value.GenerateIntermediateRepresentationForValue(irBuilder, hintType, willRevaluate)
                }, irBuilder, this);
            }
            else
            {
                if (record.Type is RecordType recordType && recordType.HasProperty(Property))
                    hintType = recordType.FindProperty(Property).Type;
                IRValue value = Value.GenerateIntermediateRepresentationForValue(irBuilder, hintType, willRevaluate);

                CodeBlock.RefinementEntry? recordRefinement = record.GetRefinementEntry(irBuilder)?.GetSubentry(Property);
                recordRefinement?.Clear();
                if (recordRefinement != null)
                    value.RefineSet(irBuilder, recordRefinement);
                return new IntermediateRepresentation.Values.SetPropertyValue(record, Property, value, irBuilder, this);
            }
        }
    }
}