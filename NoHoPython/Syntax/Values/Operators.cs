using NoHoPython.Syntax.Parsing;

namespace NoHoPython.Syntax.Values
{
    public sealed partial class SizeofOperator : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public AstType TypeToMeasure { get; private set; }

        public SizeofOperator(AstType typeToMeasure, SourceLocation sourceLocation)
        {
            TypeToMeasure = typeToMeasure;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"sizeof({TypeToMeasure})";
    }

    public sealed partial class BinaryOperator : IAstValue
    {
        public static readonly Dictionary<TokenType, int> OperatorPrecedence = new()
        {
            {TokenType.Equals, 3},
            {TokenType.NotEquals, 3},
            {TokenType.More, 3},
            {TokenType.Less, 3},
            {TokenType.MoreEqual, 3},
            {TokenType.LessEqual, 3},
            {TokenType.Add, 5},
            {TokenType.Subtract, 5},
            {TokenType.Multiply, 6},
            {TokenType.Divide, 6},
            {TokenType.Modulo, 6},
            {TokenType.Caret, 7},
            {TokenType.And, 1},
            {TokenType.Or, 1},
            {TokenType.BitAnd, 2},
            {TokenType.BitOr, 2},
            {TokenType.BitXor, 2},
            {TokenType.ShiftLeft, 4},
            {TokenType.ShiftRight, 4}
        };

        private static readonly Dictionary<TokenType, string> OperatorSymbol = new()
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
            {TokenType.Caret, "^"},
            {TokenType.And, "and"},
            {TokenType.Or, "or"},
            {TokenType.BitAnd, "&"},
            {TokenType.BitOr, "|"},
            {TokenType.BitXor, "xor"},
            {TokenType.ShiftLeft, "lshift"},
            {TokenType.ShiftRight, "rshift"}
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

    public sealed partial class MemorySet : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Address { get; private set; }
        public IAstValue Index { get; private set; }
        public IAstValue Value { get; private set; }
        public IAstValue ResponsibleDestroyer { get; private set; }

        public MemorySet(IAstValue array, IAstValue index, IAstValue value, IAstValue responsibleDestroyer, SourceLocation sourceLocation)
        {
            Address = array;
            Index = index;
            Value = value;
            ResponsibleDestroyer = responsibleDestroyer;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"{Address}{{{ResponsibleDestroyer}}}[{Index}] = {Value}";
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

    public sealed partial class CheckEnumOption : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Enum { get; private set; }
        public AstType Option { get; private set; }

        public CheckEnumOption(IAstValue @enum, AstType option, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Enum = @enum;
            Option = option;
        }

        public override string ToString() => $"{Enum} is {Option}";
    }
}
