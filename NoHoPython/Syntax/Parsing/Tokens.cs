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
        Proc,
        Def,
        While,
        For,
        If,
        Else,
        Elif,
        Break,
        Continue,
        Return,

        //symbols
        OpenBrackets,
        CloseBrackets,
        OpenParens,
        CloseParens,
        OpenBraces,
        CloseBraces,
        Comma,

        //operators
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Exponentiate,

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
