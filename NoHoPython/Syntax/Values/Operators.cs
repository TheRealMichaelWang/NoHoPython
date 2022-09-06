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
            {TokenType.And, 1},
            {TokenType.Or, 1}
        };

        private static readonly Dictionary<TokenType, string> OperatorSymbol = new Dictionary<TokenType, string>()
        {
            {TokenType.Equals, "=="},
            {TokenType.NotEquals, "!="},
            {TokenType.More, ">"},
            {TokenType.Less, "<"},
            {TokenType.MoreEqual, ">="},
            {TokenType.LessEqual, "<="},
            {TokenType.Add, "+"},
            {TokenType.Subtract, "-"},
            {TokenType.Multiply, "*"},
            {TokenType.Divide, "/"},
            {TokenType.Modulo, "%"},
            {TokenType.Exponentiate, "^"},
            {TokenType.And, "and"},
            {TokenType.Or, "or"}
        };

        public SourceLocation SourceLocation { get; private set; }

        public TokenType Operator { get; private set; }
        public IAstValue Left { get; private set; }
        public IAstValue Right { get; private set; }

        public BinaryOperator(Token @operator, IAstValue left, IAstValue right, SourceLocation sourceLocation, SourceLocation operatorLocation)
        {
            if (!OperatorPrecedence.ContainsKey(@operator.Type))
                throw new UnexpectedTokenException(@operator, operatorLocation);

            SourceLocation = sourceLocation;
            Operator = @operator.Type;
            Left = left;
            Right = right;
        }

        public override string ToString() => $"{Left} {OperatorSymbol[Operator]} {Right}";
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

        public override string ToString() => $"{Array}[{Index}]";
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

        public override string ToString() => $"{Array}[{Index}] = {Value}";
        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{this}";
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

        public override string ToString() => $"{Record}.{Property}";
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

        public override string ToString() => $"{Record}.{Property} = {Value}";
        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{this}";
    }
}
