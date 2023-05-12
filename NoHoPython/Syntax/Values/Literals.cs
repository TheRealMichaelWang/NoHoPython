using NoHoPython.IntermediateRepresentation;
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

    public sealed partial class StringLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public string String { get; private set; }

        public StringLiteral(string @string, SourceLocation sourceLocation)
        {
            String = @string;
            SourceLocation = sourceLocation;
        }

        public override string ToString()
        {
            BufferedEmitter emitter = new(String.Length);
            emitter.Append('\"');
            foreach (char c in String)
                IntermediateRepresentation.Values.CharacterLiteral.EmitCChar(emitter, c, false);
            emitter.Append('\"');
            return emitter.ToString();
        }
    }

    public sealed partial class ArrayLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public AstType? AnnotatedElementType { get; private set; }
        public readonly List<IAstValue> Elements;

        public ArrayLiteral(List<IAstValue> elements, AstType? annotatedElementType, SourceLocation sourceLocation)
        {
            Elements = elements;
            AnnotatedElementType = annotatedElementType;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"[{(AnnotatedElementType != null ? $"<{AnnotatedElementType}>" : string.Empty)}{string.Join(", ", Elements)}]";
    }

    public sealed partial class TupleLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly List<IAstValue> TupleElements;

        public TupleLiteral(List<IAstValue> tupleElements, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            TupleElements = tupleElements;
        }

        public override string ToString() => $"({string.Join(", ", TupleElements)})";
    }

    public sealed partial class AllocArray : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }
        public AstType? ElementType { get; private set; }

        public IAstValue Length { get; private set; }
        public IAstValue? ProtoValue { get; private set; }

        public AllocArray(AstType? elementType, IAstValue length, IAstValue? protoValue, SourceLocation sourceLocation)
        {
            ElementType = elementType;
            Length = length;
            ProtoValue = protoValue;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"new {ElementType}[{Length}]{(ProtoValue != null ? $"({ProtoValue})" : string.Empty)}";
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
        public AstType? RecordType { get; private set; }
        public readonly List<IAstValue> Arguments;

        public InstantiateNewRecord(AstType? recordType, List<IAstValue> arguments, SourceLocation sourceLocation)
        {
            RecordType = recordType;
            Arguments = arguments;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"new{(RecordType == null ? string.Empty : $" {RecordType}")}({string.Join(", ", Arguments)})";
    }

    public sealed partial class MarshalIntoArray : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public AstType ElementType { get; private set; }
        public IAstValue Length { get; private set; }
        public IAstValue Address { get; private set; }

        public MarshalIntoArray(AstType elementType, IAstValue length, IAstValue address, SourceLocation sourceLocation)
        {
            ElementType = elementType;
            Length = length;
            Address = address;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"marshal {ElementType}[{Length}]({Address})";
    }

    public sealed partial class FlagLiteral : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Flag;

        public FlagLiteral(SourceLocation sourceLocation, string flag)
        {
            SourceLocation = sourceLocation;
            Flag = flag;
        }

        public override string ToString() => $"flag {Flag}";
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

            List<IAstValue> elements = new();
            while (scanner.LastToken.Type != TokenType.CloseBracket)
            {
                elements.Add(ParseExpression());
                if (scanner.LastToken.Type != TokenType.CloseBracket)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();

            return new Values.ArrayLiteral(elements, annotatedType, location);
        }

        private Values.AllocArray ParseAllocArray(AstType? elementType, SourceLocation location)
        {
            MatchAndScanToken(TokenType.OpenBracket);
            IAstValue length = ParseExpression();
            MatchAndScanToken(TokenType.CloseBracket);

            if (scanner.LastToken.Type == TokenType.OpenParen)
            {
                scanner.ScanToken();
                IAstValue protoValue = ParseExpression();
                MatchAndScanToken(TokenType.CloseParen);
                return new(elementType, length, protoValue, location);
            }
            else
                return new(elementType, length, null, location);
        }

        private Values.MarshalIntoArray ParseMarshalArray(SourceLocation location)
        {
            MatchAndScanToken(TokenType.Marshal);
            AstType elementType = ParseType();
            MatchAndScanToken(TokenType.OpenBracket);
            IAstValue length = ParseExpression();
            MatchAndScanToken(TokenType.CloseBracket);
            MatchAndScanToken(TokenType.OpenParen);
            IAstValue address = ParseExpression();
            MatchAndScanToken(TokenType.CloseParen);
            return new(elementType, length, address, location);
        }
    }
}