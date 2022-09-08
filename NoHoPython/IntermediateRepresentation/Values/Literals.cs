using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IntegerLiteral : IRValue
    {
        public IType Type { get => new IntegerType(); }

        public long Number { get; private set; }

        public IntegerLiteral(long number)
        {
            Number = number;
        }
    }

    public sealed partial class DecimalLiteral : IRValue
    {
        public IType Type { get => new DecimalType(); }

        public decimal Number { get; private set; }

        public DecimalLiteral(decimal number)
        {
            Number = number;
        }
    }

    public sealed partial class CharacterLiteral : IRValue
    {
        public IType Type { get => new CharacterType(); }

        public char Character { get; private set; }

        public CharacterLiteral(char character)
        {
            Character = character;
        }
    }

    public sealed partial class TrueLiteral : IRValue
    {
        public IType Type => new BooleanType();

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new TrueLiteral();
    }

    public sealed partial class FalseLiteral : IRValue
    {
        public IType Type => new BooleanType();

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new FalseLiteral();
    }

    public sealed partial class NothingLiteral : IRValue
    {
        public IType Type => new NothingType();

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new NothingLiteral();
    }

    public sealed partial class ArrayLiteral : IRValue
    {
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
    }

    public sealed partial class AllocArray : IRValue
    {
        public IType Type { get => new ArrayType(ElementType); }

        public IType ElementType { get; private set; }
        public IRValue Size { get; private set; }

        public AllocArray(IType elementType, IRValue size)
        {
            ElementType = elementType;
            Size = ArithmeticCast.CastTo(size, Primitive.Integer);
        }
    }

    public sealed partial class AllocRecord : IRValue
    {
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
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => new IntermediateRepresentation.Values.IntegerLiteral(Number);
    }

    partial class DecimalLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => new IntermediateRepresentation.Values.DecimalLiteral(Number);
    }

    partial class TrueLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => new IntermediateRepresentation.Values.TrueLiteral();
    }

    partial class FalseLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => new IntermediateRepresentation.Values.FalseLiteral();
    }

    partial class CharacterLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => new IntermediateRepresentation.Values.CharacterLiteral(Character);
    }

    partial class ArrayLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => new IntermediateRepresentation.Values.ArrayLiteral(ElementType.ToIRType(irBuilder), Elements.ConvertAll((IAstValue element) => element.GenerateIntermediateRepresentationForValue(irBuilder)));
    }

    partial class InstantiateNewRecord
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder)
        {
            IType prototype = RecordType.ToIRType(irBuilder);
            return prototype is Typing.RecordType record
                ? (IRValue)new IntermediateRepresentation.Values.AllocRecord(record, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder)))
                : throw new UnexpectedTypeException(prototype);
        }
    }
}