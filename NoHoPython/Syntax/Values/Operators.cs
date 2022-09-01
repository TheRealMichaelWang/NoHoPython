using NoHoPython.Syntax.Parsing;

namespace NoHoPython.Syntax.Values
{
    public sealed partial class BinaryOperator : IAstValue
    {
        public static readonly Dictionary<TokenType, int> OperatorPrecedence = new Dictionary<TokenType, int>()
        {
            {TokenType.Equals, 2},
            {TokenType.NotEquals, 2},
            {TokenType.More, 2},
            {TokenType.Less, 2},
            {TokenType.MoreEqual, 2},
            {TokenType.LessEqual, 2},
            {TokenType.Add, 3},
            {TokenType.Subtract, 3},
            {TokenType.Multiply, 4},
            {TokenType.Divide, 4},
            {TokenType.Modulo, 4},
            {TokenType.Exponentiate, 5},
            {TokenType.Add, 1},
            {TokenType.Or, 1}
        };

        public SourceLocation SourceLocation { get; private set; }

        public TokenType Operator { get; private set; }
        public IAstValue Left { get; private set; }
        public IAstValue Right { get; private set; }

        public BinaryOperator(Token @operator, IAstValue left, IAstValue right, SourceLocation sourceLocation)
        {
            if (!OperatorPrecedence.ContainsKey(@operator.Type))
                throw new UnexpectedTokenException(@operator);

            SourceLocation = sourceLocation;
            Operator = @operator.Type;
            Left = left;
            Right = right;
        }
    }

    public sealed partial class GetValueAtIndex : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Array { get; private set; }
        public IAstValue Index { get; private set; }

        public GetValueAtIndex(IAstValue array, IAstValue index, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Array = array;
            Index = index;
        }
    }

    public sealed partial class SetValueAtIndex : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Array { get; private set; }
        public IAstValue Index { get; private set; }
        public IAstValue Value { get; private set; }

        public SetValueAtIndex(IAstValue array, IAstValue index, IAstValue value, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Array = array;
            Value = value;
            Index = index;
        }
    }

    public sealed partial class GetPropertyValue : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Record { get; private set; }
        public readonly string Property;

        public GetPropertyValue(IAstValue record, string property, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Record = record;
            Property = property;
        }
    }

    public sealed partial class SetPropertyValue : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Record { get; private set; }
        public IAstValue Value { get; private set; }
        public readonly string Property;

        public SetPropertyValue(IAstValue record, IAstValue value, string property, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Record = record;
            Value = value;
            Property = property;
        }
    }
}
