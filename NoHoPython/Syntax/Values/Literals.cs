using System.Text;

namespace NoHoPython.Syntax.Values
{
    public sealed partial class IntegerLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public long Number { get; private set; }

        public IntegerLiteral(long number, SourceLocation sourceLocation)
        {
            Number = number;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => Number.ToString();
    }

    public sealed partial class DecimalLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public decimal Number { get; private set; }

        public DecimalLiteral(decimal number, SourceLocation sourceLocation)
        {
            Number = number;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => Number.ToString();
    }

    public sealed partial class CharacterLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public char Character { get; private set; }

        public CharacterLiteral(char character, SourceLocation sourceLocation)
        {
            Character = character;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"\'{Character}\'";
    }

    public sealed partial class ArrayLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public AstType ElementType { get; private set; }
        public readonly List<IAstValue> Elements;
        private bool IsStringLiteral;

        public ArrayLiteral(List<IAstValue> elements, AstType elementType, SourceLocation sourceLocation)
        {
            Elements = elements;
            SourceLocation = sourceLocation;
            IsStringLiteral = false;
            ElementType = elementType;
        }

        public ArrayLiteral(string stringLiteral, SourceLocation sourceLocation) : this(stringLiteral.ToList().ConvertAll((char c) => (IAstValue)new CharacterLiteral(c, sourceLocation)), new AstType("char", new List<AstType>()), sourceLocation)
        {
            IsStringLiteral = true;
        }

        public override string ToString()
        {
            if (IsStringLiteral)
            {
                StringBuilder builder = new();
                _ = builder.Append("\"");
                foreach (IAstValue value in Elements)
                    _ = builder.Append(((CharacterLiteral)value).Character);
                _ = builder.Append("\"");
                return builder.ToString();
            }
            else
                return $"[{string.Join(", ", Elements)}]";
        }
    }

    public sealed partial class TrueLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public TrueLiteral(SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public override string ToString() => "True";
    }

    public sealed partial class FalseLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public FalseLiteral(SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public override string ToString() => "False";
    }

    public sealed partial class InstantiateNewRecord : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }
        public AstType RecordType { get; private set; }
        public readonly List<IAstValue> Arguments;

        public InstantiateNewRecord(AstType recordType, List<IAstValue> arguments, SourceLocation sourceLocation)
        {
            RecordType = recordType;
            Arguments = arguments;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"new {RecordType}({string.Join(", ", Arguments)})";
    }
}