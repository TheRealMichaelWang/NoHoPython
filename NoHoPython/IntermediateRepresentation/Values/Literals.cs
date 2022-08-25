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

    public sealed partial class ArrayLiteral : IRValue
    {
        public IType Type { get => new ArrayType(ElementType); }

        public IType ElementType { get; private set; }

        public readonly List<IRValue> Elements;

        public ArrayLiteral(IType elementType, List<IRValue> elements)
        {
            ElementType = elementType;
            Elements = elements;

            foreach (IRValue element in elements)
                if (!Type.IsCompatibleWith(element.Type))
                    throw new UnexpectedTypeException(Type, element.Type);
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
            Size = size;

            if (!Primitive.Integer.IsCompatibleWith(size.Type))
                throw new UnexpectedTypeException(Primitive.Integer, size.Type);
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
