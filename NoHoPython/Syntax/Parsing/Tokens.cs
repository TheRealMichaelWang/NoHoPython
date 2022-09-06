﻿namespace NoHoPython.Syntax.Parsing
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
        While,
        For,
        If,
        Else,
        Elif,
        Break,
        Continue,
        Return,
        Include,
        In,
        And,
        Or,
        New,
        As,
        Readonly,

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

        //operators
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Exponentiate,

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
