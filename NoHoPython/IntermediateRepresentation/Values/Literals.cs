using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IntegerLiteral : IRValue
    {
        public IType Type { get => new IntegerType(); }

        public long Number { get; private set; }

        public IntegerLiteral(long number)
        {
            this.Number = number;
        }
    }

    public sealed partial class DecimalLiteral : IRValue
    {
        public IType Type { get => new DecimalType(); }

        public decimal Number { get;private set; }

        public DecimalLiteral(decimal number)
        {
            this.Number = number;
        }
    }

    public sealed partial class CharacterLiteral : IRValue
    {
        public IType Type { get => new CharacterType(); }

        public char Character { get; private set; }

        public CharacterLiteral(char character)
        {
            this.Character = character;
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
        }
    }
}
