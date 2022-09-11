using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IntegerLiteral : IRValue
    {
        public bool IsConstant => true;
        public IType Type { get => new IntegerType(); }

        public long Number { get; private set; }

        public IntegerLiteral(long number)
        {
            Number = number;
        }
    }

    public sealed partial class DecimalLiteral : IRValue
    {
        public bool IsConstant => true;
        public IType Type { get => new DecimalType(); }

        public decimal Number { get; private set; }

        public DecimalLiteral(decimal number)
        {
            Number = number;
        }
    }

    public sealed partial class CharacterLiteral : IRValue
    {
        public bool IsConstant => true;
        public IType Type { get => new CharacterType(); }

        public char Character { get; private set; }

        public CharacterLiteral(char character)
        {
            Character = character;
        }
    }

    public sealed partial class TrueLiteral : IRValue
    {
        public bool IsConstant => true;
        public IType Type => new BooleanType();

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new TrueLiteral();
    }

    public sealed partial class FalseLiteral : IRValue
    {
        public bool IsConstant => true;
        public IType Type => new BooleanType();

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new FalseLiteral();
    }

    public sealed partial class NothingLiteral : IRValue
    {
        public bool IsConstant => true;
        public IType Type => new NothingType();

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new NothingLiteral();
    }

    public sealed partial class ArrayLiteral : IRValue
    {
        public bool IsConstant => Elements.TrueForAll(element => element.IsConstant);

        public IType Type { get => new ArrayType(ElementType); }

        public IType ElementType { get; private set; }

        public readonly List<IRValue> Elements;

        public ArrayLiteral(IType elementType, List<IRValue> elements)
        {
            ElementType = elementType;
            Elements = elements;

            for (int i = 0; i < elements.Count; i++)
                elements[i] = ArithmeticCast.CastTo(elements[i], ElementType);
        }

        public ArrayLiteral(List<IRValue> elements)
        {
            Elements = new List<IRValue>(elements.Count);

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

            throw new UnexpectedTypeException(Primitive.Nothing);
        }
    }

    public sealed partial class AllocRecord : IRValue
    {
        public bool IsConstant => false;
        public IType Type { get => RecordPrototype; }

        public RecordType RecordPrototype { get; private set; }
        public readonly List<IRValue> ConstructorArguments;

        public AllocRecord(RecordType recordPrototype, List<IRValue> constructorArguments)
        {
            RecordPrototype = recordPrototype;
            ConstructorArguments = constructorArguments;
            if (RecordPrototype.HasProperty("__init__") && RecordPrototype.FindProperty("__init__").Type is ProcedureType)
            {

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
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.IntegerLiteral(Number);
    }

    partial class DecimalLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.DecimalLiteral(Number);
    }

    partial class TrueLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.TrueLiteral();
    }

    partial class FalseLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.FalseLiteral();
    }

    partial class CharacterLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType) => new IntermediateRepresentation.Values.CharacterLiteral(Character);
    }

    partial class ArrayLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            List<IRValue> elements = Elements.ConvertAll((IAstValue element) => element.GenerateIntermediateRepresentationForValue(irBuilder, null));
            if (IsStringLiteral)
                return new IntermediateRepresentation.Values.ArrayLiteral(Primitive.Character, elements);
            else if (AnnotatedElementType != null)
                return new IntermediateRepresentation.Values.ArrayLiteral(AnnotatedElementType.ToIRType(irBuilder, this), elements);
            else
                return new IntermediateRepresentation.Values.ArrayLiteral(elements);
        }
    }

    partial class InstantiateNewRecord
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            IType prototype = RecordType.ToIRType(irBuilder, this);
            return prototype is Typing.RecordType record
                ? (IRValue)new IntermediateRepresentation.Values.AllocRecord(record, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)))
                : throw new UnexpectedTypeException(prototype);
        }
    }
}