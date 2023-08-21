namespace NoHoPython.Syntax.Parsing
{
    public enum TokenType
    {
        //literals
        Identifier,
        IntegerLiteral,
        DecimalLiteral,
        CharacterLiteral,
        StringLiteral,
        InterpolatedStart,
        InterpolatedMiddle,
        InterpolatedEnd,

        //keywords
        True,
        False,
        Module,
        Record,
        Interface,
        Enum,
        Lambda,
        Define,
        CDefine,
        Match,
        While,
        For,
        If,
        Else,
        Elif,
        Default,
        Sizeof,
        Return,
        Abort,
        Break,
        Continue,
        Pass,
        Include,
        CInclude,
        Assert,
        Destroy,
        Is,
        From,
        Within,
        To,
        And,
        Or,
        New,
        Marshal,
        Flag,
        As,
        Readonly,
        Nothing,
        Attributes,

        //symbols
        OpenBracket,
        CloseBracket,
        OpenParen,
        CloseParen,
        OpenBrace,
        CloseBrace,
        Comma,
        Semicolon,
        Colon,
        Period,
        Set,
        ModuleAccess,

        //operators
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Caret,

        //comparison operators
        Equals,
        NotEquals,
        More,
        Less,
        MoreEqual,
        LessEqual,
        Not,

        //bitwise operators
        BitAnd,
        BitOr,
        BitXor,
        ShiftLeft,
        ShiftRight,

        Tab,
        Newline,
        EndOfFile
    }

    public struct Token
    {
        public TokenType Type { get; private set; }
        public string Identifier { get; private set; }

        public Token(TokenType type, string identifier)
        {
            Type = type;
            Identifier = identifier;
        }
    }
}
