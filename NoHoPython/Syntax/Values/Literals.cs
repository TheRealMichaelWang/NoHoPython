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

        public AstType? AnnotatedElementType { get; private set; }
        public readonly List<IAstValue> Elements;
        private bool IsStringLiteral;

        public ArrayLiteral(List<IAstValue> elements, AstType? annotatedElementType, SourceLocation sourceLocation)
        {
            Elements = elements;
            AnnotatedElementType = annotatedElementType;
            SourceLocation = sourceLocation;
            IsStringLiteral = false;
        }

        public ArrayLiteral(string stringLiteral, SourceLocation sourceLocation) : this(stringLiteral.ToList().ConvertAll((char c) => (IAstValue)new CharacterLiteral(c, sourceLocation)), null, sourceLocation)
        {
            IsStringLiteral = true;
        }

        public override string ToString()
        {
            if (IsStringLiteral)
            {
                StringBuilder builder = new();
                builder.Append("\"");
                foreach (IAstValue value in Elements)
                    IntermediateRepresentation.Values.CharacterLiteral.EmitCChar(builder, ((CharacterLiteral)value).Character);
                builder.Append("\"");
                return builder.ToString();
            }
            else
                return $"[{(AnnotatedElementType != null ? $"<{AnnotatedElementType}>" : string.Empty)}{string.Join(", ", Elements)}]";
        }
    }

    public sealed partial class AllocArray : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }
        public AstType ElementType { get; private set; }

        public IAstValue Length { get; private set; }
        public IAstValue? ProtoValue { get; private set; }

        public AllocArray(SourceLocation sourceLocation, AstType elementType, IAstValue length, IAstValue? protoValue)
        {
            SourceLocation = sourceLocation;
            ElementType = elementType;
            Length = length;
            ProtoValue = protoValue;
        }

        public override string ToString() => $"new {ElementType}[{Length}]";
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

    public sealed partial class NothingLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public NothingLiteral(SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
        }

        public override string ToString() => "Nothing";
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

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private Values.ArrayLiteral ParseArrayLiteral()
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchAndScanToken(TokenType.OpenBracket);

            AstType? annotatedType = null;
            if(scanner.LastToken.Type == TokenType.Less)
            {
                scanner.ScanToken();
                annotatedType = ParseType();
                MatchAndScanToken(TokenType.More);
            }

            List<IAstValue> elements = new List<IAstValue>();
            while (scanner.LastToken.Type != TokenType.CloseBracket)
            {
                elements.Add(ParseExpression());
                if (scanner.LastToken.Type != TokenType.CloseBracket)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();

            return new Values.ArrayLiteral(elements, annotatedType, location);
        }

        private Values.AllocArray ParseAllocArray(AstType elementType, SourceLocation location)
        {
            MatchAndScanToken(TokenType.OpenBracket);
            IAstValue length = ParseExpression();
            MatchAndScanToken(TokenType.CloseBracket);

            if (scanner.LastToken.Type == TokenType.OpenParen)
            {
                scanner.ScanToken();
                IAstValue protoValue = ParseExpression();
                MatchAndScanToken(TokenType.CloseParen);
                return new(location, elementType, length, protoValue);
            }
            else
                return new(location, elementType, length, null);
        }
    }
}