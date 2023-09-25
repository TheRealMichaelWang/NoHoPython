using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class SizeofOperator : IRValue
    {
        public IType Type => new IntegerType();
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IAstElement ErrorReportedElement { get; private set; }

        public IType TypeToMeasure { get; private set; }

        public SizeofOperator(IType typeToMeasure, IAstElement errorReportedElement)
        {
            TypeToMeasure = typeToMeasure;
            ErrorReportedElement = errorReportedElement;
            if (TypeToMeasure.IsEmpty)
                throw new UnexpectedTypeException(TypeToMeasure, errorReportedElement);
        }
    }

    public sealed partial class MemoryGet : BinaryOperator
    {
        public override IType Type { get; }

        public MemoryGet(IType type, IRValue address, IRValue index, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(address, ArithmeticCast.CastTo(index, Primitive.Integer, irBuilder), errorReportedElement)
        {
            Type = type;

            if (address.Type is not HandleType handleType || handleType.ValueType is NothingType)
                throw new UnexpectedTypeException(address.Type, errorReportedElement);
        }

        public MemoryGet(IType type, IRValue address, IRValue index, IAstElement errorReportedElement) : base(address, index, errorReportedElement)
        {
            Type = type;
        }
    }

    public sealed partial class MemorySet : IRValue, IRStatement
    {
        public IType Type { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Address { get; private set; }
        public IRValue Index { get; private set; }
        public IRValue Value { get; private set; }

        public MemorySet(IType type, IRValue address, IRValue index, IRValue value, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Type = type;
            Address = address;
            Index = ArithmeticCast.CastTo(index, Primitive.Integer, irBuilder);
            Value = ArithmeticCast.CastTo(value, type, irBuilder);

            if (address.Type is not HandleType handleType || handleType.ValueType is NothingType)
                throw new UnexpectedTypeException(address.Type, errorReportedElement);

            ErrorReportedElement = errorReportedElement;
        }

        private MemorySet(IType type, IRValue address, IRValue index, IRValue value, IAstElement errorReportedElement)
        {
            Type = type;
            Address = address;
            Index = index;
            Value = value;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class MarshalHandleIntoArray : IRValue
    {
        public IType Type => new ArrayType(ElementType);
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IAstElement ErrorReportedElement { get; private set; }

        public IType ElementType { get; private set; }
        
        public IRValue Length { get; private set; }
        public IRValue Address { get; private set; }

        public MarshalHandleIntoArray(IType elementType, IRValue length, IRValue address, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ElementType = elementType;
            Address = ArithmeticCast.CastTo(address, new HandleType(elementType), irBuilder);
            Length = ArithmeticCast.CastTo(length, Primitive.Integer, irBuilder);
            ErrorReportedElement = errorReportedElement;
        }

        private MarshalHandleIntoArray(IType elementType, IRValue length, IRValue address, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            ElementType = elementType;
            Length = length;
            Address = address;
        }
    }

    public sealed partial class MarshalMemorySpanIntoArray : IRValue
    {
        public IType Type => new ArrayType(ElementType);
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Span { get; private set; }
        public IType ElementType { get; private set; }

        public MarshalMemorySpanIntoArray(IRValue span, IAstElement errorReportedElement)
        {
            Span = span;
            ErrorReportedElement = errorReportedElement;

            if (Span.Type is MemorySpan memorySpan)
                ElementType = memorySpan.ElementType;
            else
                throw new UnexpectedTypeException(Span.Type, errorReportedElement);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class MemoryDestroy : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Address { get; private set; }

        private HandleType AddressType;

        public MemoryDestroy(IRValue address, IAstElement errorReportedElement)
        {
            if (address.Type is HandleType handleType && handleType.ValueType is not NothingType)
            {
                AddressType = handleType;
                Address = address;
            }
            else
                throw new UnexpectedTypeException(address.Type, errorReportedElement);

            ErrorReportedElement = errorReportedElement;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class MarshalIntoArray
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new MarshalHandleIntoArray(ElementType.ToIRType(irBuilder, this), Length.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), Address.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Handle, willRevaluate), irBuilder, this);
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class DestroyStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue address = Address.GenerateIntermediateRepresentationForValue(irBuilder, null, false);
            return new IntermediateRepresentation.Statements.MemoryDestroy(address, this);
        }
    }
}