using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
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

        private static Dictionary<CompareOperation, CompareOperation> reversedCompareOperations = new Dictionary<CompareOperation, CompareOperation>() 
        { 
            {CompareOperation.Equals, CompareOperation.Equals},
            {CompareOperation.NotEquals, CompareOperation.NotEquals},
            {CompareOperation.Less, CompareOperation.More},
            {CompareOperation.LessEqual, CompareOperation.MoreEqual},
            {CompareOperation.More, CompareOperation.Less },
            {CompareOperation.MoreEqual, CompareOperation.LessEqual}
        };

        public static IRValue ComposeComparativeOperator(CompareOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (operation == CompareOperation.Equals || operation == CompareOperation.NotEquals)
            {
                if (GetPropertyValue.HasMessageReceiver(left, "equals", irBuilder))
                    return AnonymousProcedureCall.SendMessage(left, "equals", Primitive.Boolean, new List<IRValue>()
                    {
                        right
                    }, irBuilder, errorReportedElement);
                if(GetPropertyValue.HasMessageReceiver(right, "equals", irBuilder))
                    return AnonymousProcedureCall.SendMessage(right, "equals", Primitive.Boolean, new List<IRValue>()
                    {
                        left
                    }, irBuilder, errorReportedElement);
            }
            if (GetPropertyValue.HasMessageReceiver(left, "compare", irBuilder))
                return new ComparativeOperator(operation, AnonymousProcedureCall.SendMessage(left, "compare", Primitive.Integer, new List<IRValue>()
                    {
                        right
                    }, irBuilder, errorReportedElement), new IntegerLiteral(0, errorReportedElement), errorReportedElement);
            if(GetPropertyValue.HasMessageReceiver(right, "compare", irBuilder))
                return new ComparativeOperator(reversedCompareOperations[operation], AnonymousProcedureCall.SendMessage(right, "compare", Primitive.Integer, new List<IRValue>()
                    {
                        left
                    }, irBuilder, errorReportedElement), new IntegerLiteral(0, errorReportedElement), errorReportedElement);

            return new ComparativeOperator(operation, left, right, irBuilder, errorReportedElement);
        }

        public override IType Type => new BooleanType();

        public CompareOperation Operation { get; private set; }

        private ComparativeOperator(CompareOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
        {
            Operation = operation;

            if (left.Type is not Primitive && !(left.Type is ForeignCType foreignCType && foreignCType.Declaration.PointerPropertyAccess))
                throw new UnexpectedTypeException(left.Type, left.ErrorReportedElement);
            if (right.Type is not Primitive && !(right.Type is ForeignCType rightForeignCType && rightForeignCType.Declaration.PointerPropertyAccess))
                throw new UnexpectedTypeException(right.Type, right.ErrorReportedElement);

            if (left.Type is DecimalType && right.Type is not DecimalType)
                Right = ArithmeticCast.CastTo(right, Primitive.Decimal, irBuilder);
            if (right.Type is DecimalType && left.Type is not DecimalType)
                Left = ArithmeticCast.CastTo(left, Primitive.Decimal, irBuilder);
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
            return GetPropertyValue.HasMessageReceiver(array, "getAtIndex", irBuilder)
                ? AnonymousProcedureCall.SendMessage(array, "getAtIndex", expectedType, new List<IRValue>() { index }, irBuilder, errorReportedElement)
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
            return GetPropertyValue.HasMessageReceiver(array, "setAtIndex", irBuilder)
                ? AnonymousProcedureCall.SendMessage(array, "setAtIndex", null, new List<IRValue>() { index, value}, irBuilder, errorReportedElement)
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
        public static bool HasMessageReceiver(IType type, string name, IAstElement errorReportedElement, AstIRProgramBuilder irBuilder)
        {
            if(type is IPropertyContainer propertyContainer && propertyContainer.HasProperty(name))
                return true;
            
            IScopeSymbol? messageReceiver = irBuilder.SymbolMarshaller.FindSymbol($"typeExt:{type.Identifier}_{name}");
            if(messageReceiver == null)
                messageReceiver = irBuilder.SymbolMarshaller.FindSymbol($"typeExt:{type.PrototypeIdentifier}_{name}");

            if(messageReceiver == null && type.IsCompatibleWith(Primitive.GetStringType(irBuilder, errorReportedElement)))
                messageReceiver = irBuilder.SymbolMarshaller.FindSymbol($"typeExt:string_{name}");

            if (messageReceiver == null && type is EnumType enumType)
                return enumType.GetOptions().Any(option => HasMessageReceiver(option, name, errorReportedElement, irBuilder));

            return messageReceiver is ProcedureDeclaration || messageReceiver is ForeignCProcedureDeclaration;
        }

        public static bool HasMessageReceiver(IRValue value, string name, AstIRProgramBuilder irBuilder) => HasMessageReceiver(value.Type, name, value.ErrorReportedElement, irBuilder);

        public static IRValue ComposeGetProperty(IRValue record, string name, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (record.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty(name))
                return new GetPropertyValue(record, name, record.GetRefinementEntry(irBuilder)?.GetSubentry(name)?.Refinement, errorReportedElement);

            if (HasMessageReceiver(record, $"get_{name}", irBuilder))
                return AnonymousProcedureCall.SendMessage(record, $"get_{name}", null, new(), irBuilder, errorReportedElement);

            if (record.Type is ArrayType)
            {
                if(name == "length" || name == "len")
                    return new ArrayOperator(ArrayOperator.ArrayOperation.GetArrayLength, record, record.ErrorReportedElement);
                if(name == "handle" || name == "ptr")
                    return new ArrayOperator(ArrayOperator.ArrayOperation.GetArrayHandle, record, record.ErrorReportedElement);
            }
            if(record.Type is MemorySpan memorySpan)
            {
                if (name == "length" || name == "len")
                    return new IntegerLiteral(memorySpan.Length, record.ErrorReportedElement);
                if (name == "handle" || name == "ptr")
                    return new ArrayOperator(ArrayOperator.ArrayOperation.GetSpanHandle, record, record.ErrorReportedElement);
            }

            throw new UnexpectedTypeException(record.Type, $"Type doesn't contain property {name}.", errorReportedElement);
        }

        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Refinements.HasValue ? Refinements.Value.Item1 : Property.Type;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue Record { get; private set; }
        public Property Property { get; private set; }

        public (IType, RefinementContext.RefinementEmitter?)? Refinements { get; private set; }

        public GetPropertyValue(IRValue record, string propertyName, (IType, RefinementContext.RefinementEmitter?)? refinements, IAstElement errorReportedElement)
        {
            Record = record;
            Refinements = refinements;
            Property = record.Type is IPropertyContainer propertyContainer
				? (propertyContainer.HasProperty(propertyName) ? propertyContainer.FindProperty(propertyName) : throw new UnexpectedTypeException(record.Type, $"Type doesn't contain property {propertyName}.", errorReportedElement))
                : throw new UnexpectedTypeException(record.Type, "Type isn't a property container.", errorReportedElement);
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class SetPropertyValue : IRValue, IRStatement
    {
        public static IRValue ComposeSetPropertyValue(IRValue record, string name, IRValue value, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (record.Type is RecordType recordType && recordType.HasProperty(name))
            {
                RefinementContext.RefinementEntry? recordRefinement = record.GetRefinementEntry(irBuilder)?.GetSubentry(name);
                recordRefinement?.Clear();
                if (recordRefinement != null)
                    value.RefineSet(irBuilder, recordRefinement);
                return new SetPropertyValue(record, name, value, irBuilder, errorReportedElement);
            }
            if (GetPropertyValue.HasMessageReceiver(record, $"set_{name}", irBuilder))
                AnonymousProcedureCall.SendMessage(record, $"set_{name}", null, new(), irBuilder, errorReportedElement);

            throw new UnexpectedTypeException(record.Type, $"Type doesn't contain property {name}.", errorReportedElement);
        }
        
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Property.Type;
        public bool IsTruey => Value.IsTruey;
        public bool IsFalsey => Value.IsFalsey;

        public IRValue Record { get; private set; }
        public RecordDeclaration.RecordProperty Property { get; private set; }

        public IRValue Value { get; private set; }

        private SetPropertyValue(IRValue record, string propertyName, IRValue value, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Record = record;
            ErrorReportedElement = errorReportedElement;
            IsInitializingProperty = false;

            if (record.Type is RecordType propertyContainer)
            {
                if (!propertyContainer.HasProperty(propertyName))
                    throw new UnexpectedTypeException(record.Type, $"Record doesn't contain property {propertyName}.", errorReportedElement);

                Property = (RecordDeclaration.RecordProperty)propertyContainer.FindProperty(propertyName);
                Value = ArithmeticCast.CastTo(value, Property.Type, irBuilder);
            }
            else
                throw new UnexpectedTypeException(record.Type, $"Type isn't a record/class.", errorReportedElement);
        }

        private SetPropertyValue(IRValue record, RecordDeclaration.RecordProperty property, IRValue value, IAstElement errorReportedElement)
        {
            Record = record;
            Property = property;
            Value = value;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class ReleaseReferenceElement : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => ((ReferenceType)ReferenceBox.Type).ElementType;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue ReferenceBox { get; private set; }

        public ReleaseReferenceElement(IRValue referenceBox, IAstElement errorReportedElement)
        {
            if (referenceBox.Type is not ReferenceType)
                throw new UnexpectedTypeException(referenceBox.Type, errorReportedElement);

            ReferenceBox = referenceBox;
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

            if (LogicalTokens.ContainsKey(Operator))
            {
                irBuilder.NewRefinmentContext();
                switch (LogicalTokens[Operator])
                {
                    case LogicalOperator.LogicalOperation.And:
                        lhs.RefineIfTrue(irBuilder);
                        break;
                    case LogicalOperator.LogicalOperation.Or:
                        lhs.RefineIfFalse(irBuilder);
                        break;
                }
                IRValue rhs = Right.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, willRevaluate);
                irBuilder.PopAndApplyRefinementContext();
                return new LogicalOperator(LogicalTokens[Operator], lhs, rhs, irBuilder, this);
            }

            return ArithmeticTokens.ContainsKey(Operator)
                ? ArithmeticOperator.ComposeArithmeticOperation(ArithmeticTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, lhs.Type, willRevaluate), irBuilder, this)
                : ComparativeTokens.ContainsKey(Operator)
                ? ComparativeOperator.ComposeComparativeOperator(ComparativeTokens[Operator], lhs, Right.GenerateIntermediateRepresentationForValue(irBuilder, lhs.Type, willRevaluate), irBuilder, this)
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
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => IntermediateRepresentation.Values.GetPropertyValue.ComposeGetProperty(Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), Property, irBuilder, this);
    }

    partial class SetPropertyValue
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IRValue record = Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);

            IType? hintType = record.Type is RecordType recordType && recordType.HasProperty(Property) ? recordType.FindProperty(Property).Type : expectedType;
            IRValue value = Value.GenerateIntermediateRepresentationForValue(irBuilder, hintType, willRevaluate);

            return IntermediateRepresentation.Values.SetPropertyValue.ComposeSetPropertyValue(record, Property, value, irBuilder, this);
        }
    }
}