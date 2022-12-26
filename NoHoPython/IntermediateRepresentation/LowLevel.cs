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
            if (TypeToMeasure is NothingType)
                throw new UnexpectedTypeException(TypeToMeasure, errorReportedElement);
        }
    }

    public sealed partial class MemoryGet : BinaryOperator
    {
        public override IType Type { get; }

        public MemoryGet(IType type, IRValue address, IRValue index, IAstElement errorReportedElement) : base(ArithmeticCast.CastTo(address, Primitive.Handle), ArithmeticCast.CastTo(index, Primitive.Integer), errorReportedElement)
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
        public IRValue? ResponsibleDestroyer { get; private set; }

        public MemorySet(IType type, IRValue address, IRValue index, IRValue value, IRValue? responsibleDestroyer, IAstElement errorReportedElement)
        {
            Type = type;
            Address = ArithmeticCast.CastTo(address, Primitive.Handle);
            Index = ArithmeticCast.CastTo(index, Primitive.Integer);
            Value = ArithmeticCast.CastTo(value, type);
            if (responsibleDestroyer != null && responsibleDestroyer.Type is not ArrayType && responsibleDestroyer.Type is not RecordType)
                throw new UnexpectedTypeException(responsibleDestroyer.Type, errorReportedElement);
            ResponsibleDestroyer = responsibleDestroyer;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public partial class MarshalIntoArray : IRValue
    {
        public IType Type => new ArrayType(ElementType);
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IAstElement ErrorReportedElement { get; private set; }

        public IType ElementType { get; private set; }
        
        public IRValue Length { get; private set; }
        public IRValue Address { get; private set; }

        public MarshalIntoArray(IType elementType, IRValue length, IRValue address, IAstElement errorReportedElement)
        {
            ElementType = elementType;
            Length = length;
            Address = address;
            ErrorReportedElement = errorReportedElement;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class MemoryDestroy : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get; private set; }
        public IRValue Address { get; private set; }
        public IRValue? Index { get; private set; }

        public MemoryDestroy(IType type, IRValue address, IRValue? index, IAstElement errorReportedElement)
        {
            Type = type;
            Address = ArithmeticCast.CastTo(address, Primitive.Handle);
            Index = index == null ? null : ArithmeticCast.CastTo(index, Primitive.Integer);
            ErrorReportedElement = errorReportedElement;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class MemorySet
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) 
        {
            IRValue value = Value.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);
            return new IntermediateRepresentation.Values.MemorySet(value.Type, Address.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Handle, willRevaluate), Index.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), value, ResponsibleDestroyer.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), this);
        }
    }

    partial class MarshalIntoArray
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            return new IntermediateRepresentation.Values.MarshalIntoArray(ElementType.ToIRType(irBuilder, this), Length.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), Address.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Handle, willRevaluate), this);
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class DestroyStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => new IntermediateRepresentation.Statements.MemoryDestroy(Type.ToIRType(irBuilder, this), Address.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Handle, false), Index == null ? null : Index.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, false), this);
    }
}