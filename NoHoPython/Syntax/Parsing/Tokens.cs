namespace NoHoPython.Syntax.Parsing
{
    public enum TokenType
    {
        //keywords
        Identifier,
        IntegerLiteral,
        DecimalLiteral,
        CharacterLiteral,
        StringLiteral,
        True,
        False,
        Module,
        Record,
        Interface,
        Enum,
        Define,
        CDefine,
        While,
        For,
        If,
        Else,
        Elif,
        Break,
        Continue,
        Return,
        Include,
        CInclude,
        Assert,
        In,
        And,
        Or,
        New,
        As,
        Readonly,
        Nothing,

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
