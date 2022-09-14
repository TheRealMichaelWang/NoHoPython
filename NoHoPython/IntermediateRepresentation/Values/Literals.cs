using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IntegerLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => new IntegerType(); }

        public long Number { get; private set; }

        public IntegerLiteral(long number, IAstElement errorReportedElement)
        {
            Number = number;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class DecimalLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => new DecimalType(); }

        public decimal Number { get; private set; }

        public DecimalLiteral(decimal number, IAstElement errorReportedElement)
        {
            Number = number;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class CharacterLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => new CharacterType(); }

        public char Character { get; private set; }

        public CharacterLiteral(char character, IAstElement errorReportedElement)
        {
            Character = character;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class TrueLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public TrueLiteral(IAstElement errorReportedElement) => ErrorReportedElement = errorReportedElement;

        public IType Type => new BooleanType();
    }

    public sealed partial class FalseLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public FalseLiteral(IAstElement errorReportedElement) => ErrorReportedElement = errorReportedElement;

        public IType Type => new BooleanType();
    }

    public sealed partial class NothingLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public NothingLiteral(IAstElement errorReportedElement) => ErrorReportedElement = errorReportedElement;

        public IType Type => new NothingType();
    }

    public sealed partial class ArrayLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => new ArrayType(ElementType); }

        public IType ElementType { get; private set; }

        public readonly List<IRValue> Elements;

        public ArrayLiteral(IType elementType, List<IRValue> elements, IAstElement errorReportedElement)
        {
            ElementType = elementType;
            Elements = elements;
            ErrorReportedElement = errorReportedElement;

            for (int i = 0; i < elements.Count; i++)
                elements[i] = ArithmeticCast.CastTo(elements[i], ElementType);
        }

        public ArrayLiteral(List<IRValue> elements, IAstElement errorReportedElement)
        {
            Elements = new List<IRValue>(elements.Count);
            ErrorReportedElement = errorReportedElement;

            bool CanBeElementType(IType type)
            {
                try
                {
                    for (int i = 0; i < elements.Count; i++)
                        Elements[i] = ArithmeticCast.CastTo(elements[i], type);
                    return true;
                }
                catch (UnexpectedTypeException)
                {
                    return false;
                }
            }

            foreach(IRValue element in elements)
                if (CanBeElementType(element.Type))
                {
                    ElementType = element.Type;
                    return;
                }

            throw new UnexpectedTypeException(Primitive.Nothing, errorReportedElement);
        }
    }

    public sealed partial class AllocRecord : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => RecordPrototype; }

        public RecordType RecordPrototype { get; private set; }
        public readonly List<IRValue> ConstructorArguments;

        public AllocRecord(RecordType recordPrototype, List<IRValue> constructorArguments, IAstElement errorReportedElement)
        {
            RecordPrototype = recordPrototype;
            ConstructorArguments = constructorArguments;
            ErrorReportedElement = errorReportedElement;

            if (RecordPrototype.HasProperty("__init__") && RecordPrototype.FindProperty("__init__").Type is ProcedureType constructurType)
            {
                for (int i = 0; i < ConstructorArguments.Count; i++)
                    ConstructorArguments[i] = ArithmeticCast.CastTo(ConstructorArguments[i], constructurType.ParameterTypes[i]);
            }
            else
            {

            }
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class IntegerLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.IntegerLiteral(Number, this);
    }

    partial class DecimalLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.DecimalLiteral(Number, this);
    }

    partial class TrueLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.TrueLiteral(this);
    }

    partial class FalseLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.FalseLiteral(this);
    }

    partial class NothingLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.NothingLiteral(this);
    }

    partial class CharacterLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.CharacterLiteral(Character, this);
    }

    partial class ArrayLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            List<IRValue> elements = Elements.ConvertAll((IAstValue element) => element.GenerateIntermediateRepresentationForValue(irBuilder, null));
            if (IsStringLiteral)
                return new IntermediateRepresentation.Values.ArrayLiteral(Primitive.Character, elements, this);
            else if (AnnotatedElementType != null)
                return new IntermediateRepresentation.Values.ArrayLiteral(AnnotatedElementType.ToIRType(irBuilder, this), elements, this);
            else
                return new IntermediateRepresentation.Values.ArrayLiteral(elements, this);
        }
    }

    partial class InstantiateNewRecord
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            IType prototype = RecordType.ToIRType(irBuilder, this);
            return prototype is Typing.RecordType record
                ? (IRValue)new IntermediateRepresentation.Values.AllocRecord(record, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)), this)
                : throw new UnexpectedTypeException(prototype, this);
        }
    }
}