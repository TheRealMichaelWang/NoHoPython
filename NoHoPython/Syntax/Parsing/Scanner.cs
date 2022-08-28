﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.Syntax.Parsing
{
    public sealed class UnrecognizedEscapeCharacterException : Exception
    {
        public char EscapeCharacter { get; private set; }

        public UnrecognizedEscapeCharacterException(char escapeCharacter) : base($"Unrecognized escape character \"{escapeCharacter}\".")
        {
            EscapeCharacter = escapeCharacter;
        }
    }

    public sealed class UnexpectedCharacterException : Exception
    {
        public char? ExpectedCharacter { get; private set; }
        public char RecievedCharacter { get; private set; }

        public UnexpectedCharacterException(char expectedCharacter, char recievedCharacter) : base($"Expected {expectedCharacter} but got {recievedCharacter} instead.")
        {
            ExpectedCharacter = expectedCharacter;
            RecievedCharacter = recievedCharacter;
        }

        public UnexpectedCharacterException(char recievedCharacter) : base($"Unexpected character {recievedCharacter}.")
        {
            RecievedCharacter = recievedCharacter;
            ExpectedCharacter = null;
        }
    }

    public sealed class Scanner
    {
        private sealed class FileVisitor
        {
            public readonly string FileName;
            public int Row { get; private set; }
            public int Column { get; private set; }

            private readonly string source;
            private int position;

            public FileVisitor(string fileName)
            {
                FileName = fileName;

                if(!File.Exists(fileName))
                    throw new FileNotFoundException(fileName);
                source = File.ReadAllText(fileName);

                Row = 1;
                Column = 1;
                position = 0;
            }

            public char ScanChar()
            {
                if(position < source.Length)
                    return source[position++];
                return '\0';
            }
        }

        private Stack<FileVisitor> visitorStack;
        private SortedSet<string> visitedFiles;

        private Token LastToken;
        private char lastChar;

        public Scanner(string firstFileToVisit)
        {
            visitorStack = new Stack<FileVisitor>();
            visitedFiles = new SortedSet<string>();

            IncludeFile(firstFileToVisit);
            ScanChar();
        }

        public void IncludeFile(string fileName)
        {
            if (visitedFiles.Contains(fileName))
                return;

            visitorStack.Push(new FileVisitor(fileName));
            visitedFiles.Add(fileName);
        }

        private char ScanChar()
        {
            char c = visitorStack.Peek().ScanChar();
            if (c == '\0')
            {
                if (visitorStack.Count > 1)
                {
                    visitorStack.Pop();
                    return ScanChar();
                }
                return '\0';
            }
            else
                return lastChar = c;
        }

        private char ScanCharLiteral()
        {
            char internalScanChar() 
            {
                if (lastChar == '\0')
                    throw new UnrecognizedEscapeCharacterException('\0');
                else if (lastChar == '\\') //control characters
                {
                    char c = ScanChar();
                    switch (c)
                    {
                        case '\"':
                            return '\"';
                        case 'a':
                            return '\a';
                        case 'b':
                            return '\b';
                        case 'f':
                            return '\f';
                        case 't':
                            return '\t';
                        case 'r':
                            return '\r';
                        case 'n':
                            return '\n';
                        case '0':
                            return '\0';
                        default:
                            throw new UnrecognizedEscapeCharacterException(c);
                    }
                }
                else
                    return lastChar; 
            }
            char scanned = internalScanChar();
            ScanChar();
            return scanned;
        }

        private TokenType ScanSymbol()
        {
            char symChar = lastChar;
            ScanChar();
            switch (symChar)
            {
                case '[':
                    return TokenType.OpenBracket;
                case ']':
                    return TokenType.CloseBracket;
                case '(':
                    return TokenType.OpenParen;
                case ')':
                    return TokenType.CloseParen;
                case '{':
                    return TokenType.OpenBracket;
                case '}':
                    return TokenType.CloseBracket;
                case ',':
                    return TokenType.Comma;
                case ';':
                    return TokenType.Semicolon;
                case ':':
                    return TokenType.Comma;
                case '+':
                    return TokenType.Add;
                case '-':
                    return TokenType.Subtract;
                case '*':
                    return TokenType.Multiply;
                case '/':
                    return TokenType.Divide;
                case '%':
                    return TokenType.Modulo;
                case '^':
                    return TokenType.Exponentiate;
                case '=':
                    if (lastChar == '=')
                    {
                        ScanChar();
                        return TokenType.Equals;
                    }
                    else
                        return TokenType.Set;
                case '>':
                    if (lastChar == '=')
                    {
                        ScanChar();
                        return TokenType.MoreEqual;
                    }
                    else
                        return TokenType.More;
                case '<':
                    if (lastChar == '=')
                    {
                        ScanChar();
                        return TokenType.LessEqual;
                    }
                    else
                        return TokenType.Less;
                case '!':
                    if (lastChar == '=')
                    {
                        ScanChar();
                        return TokenType.NotEquals;
                    }
                    else
                        return TokenType.Not;
                case '\n':
                    return TokenType.Newline;
                case '\t':
                    return TokenType.Tab;
                case '\0':
                    return TokenType.EndOfFile;
                default:
                    throw new UnexpectedCharacterException(symChar);
            }
        }

        public Token ScanToken()
        {
            while (lastChar == '\r' || lastChar == ' ')
                ScanChar();

            if(char.IsLetter(lastChar) || lastChar == '_' || lastChar == '@')
            {
                string keyword = string.Empty;
                do
                {
                    keyword += lastChar;
                    ScanChar();
                }
                while (char.IsLetter(lastChar) || char.IsDigit(lastChar) || lastChar == '_');

                return LastToken = new Token(keyword switch
                {
                    "True" => TokenType.True,
                    "true" => TokenType.True,
                    "False" => TokenType.False,
                    "false" => TokenType.False,
                    "module" => TokenType.Module,
                    "mod" => TokenType.Module,
                    "record" => TokenType.Record,
                    "proc" => TokenType.Proc,
                    "def" => TokenType.Proc,
                    "while" => TokenType.While,
                    "for" => TokenType.For,
                    "if" => TokenType.If,
                    "elif" => TokenType.Elif,
                    "else" => TokenType.Else,
                    "break" => TokenType.Break,
                    "continue" => TokenType.Continue,
                    "return" => TokenType.Return,
                    "in" => TokenType.In,
                    "and" => TokenType.And,
                    "or" => TokenType.Or,
                    _ => TokenType.Identifier
                }, keyword);
            }
            else if (char.IsDigit(lastChar))
            {
                string numStr = string.Empty;
                do
                {
                    numStr += lastChar;
                    ScanChar();
                } while (char.IsDigit(lastChar) || lastChar == '.');
                return LastToken = new Token(numStr.Contains('.') ? TokenType.DecimalLiteral : TokenType.IntegerLiteral, numStr);
            }
            else if(lastChar == '\'')
            {
                ScanChar();
                Token tok = new Token(TokenType.CharacterLiteral, ScanCharLiteral().ToString());
                if (lastChar != '\'')
                    throw new UnexpectedCharacterException('\'', lastChar);
                ScanChar();
                return tok;
            }
            else if(lastChar == '\"')
            {
                string buffer = string.Empty;
                ScanChar();
                while (lastChar != '\"')
                    buffer += ScanCharLiteral();
                ScanChar();
                return new Token(TokenType.StringLiteral, buffer);
            }
            else
                return new Token(ScanSymbol(), string.Empty);
        }
    }
}
