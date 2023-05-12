using System.Text;

namespace NoHoPython.Syntax.Parsing
{
    public sealed class UnrecognizedEscapeCharacterException : SyntaxError
    {
        public char EscapeCharacter { get; private set; }

        public UnrecognizedEscapeCharacterException(char escapeCharacter, SourceLocation sourceLocation) : base(sourceLocation, $"Unrecognized escape character \"{escapeCharacter}\".")
        {
            EscapeCharacter = escapeCharacter;
        }
    }

    public sealed class UnexpectedCharacterException : SyntaxError
    {
        public char? ExpectedCharacter { get; private set; }
        public char RecievedCharacter { get; private set; }

        public UnexpectedCharacterException(char expectedCharacter, char recievedCharacter, SourceLocation sourceLocation) : base(sourceLocation, $"Expected {expectedCharacter} but got {recievedCharacter} instead.")
        {
            ExpectedCharacter = expectedCharacter;
            RecievedCharacter = recievedCharacter;
        }

        public UnexpectedCharacterException(char recievedCharacter, SourceLocation sourceLocation) : base(sourceLocation, $"Unexpected character {recievedCharacter}.")
        {
            RecievedCharacter = recievedCharacter;
            ExpectedCharacter = null;
        }
    }

    public sealed partial class Scanner
    {
        private sealed class FileVisitor
        {
            public SourceLocation CurrentLocation => new(Row, Column, FileName);

            public readonly string FileName;
            public readonly string WorkingDirectory;

            public int Row { get; private set; }
            public int Column { get; private set; }
            public char LastChar { get; private set; }

            private readonly string source;
            private int position;

            public FileVisitor(string fileName, Scanner scanner)
            {
                if (!File.Exists(fileName))
                {
                    if (File.Exists(Path.Combine(scanner.standardLibraryDirectory, fileName)))
                    {
                        fileName = Path.Combine(scanner.standardLibraryDirectory, fileName);
                        goto file_found;
                    }
                    else if (scanner.visitorStack.Count > 0)
                    {
                        FileVisitor parent = scanner.visitorStack.Peek();
                        if (File.Exists(Path.Combine(parent.WorkingDirectory, fileName)))
                        {
                            fileName = Path.Combine(parent.WorkingDirectory, fileName);
                            goto file_found;
                        }
                    }
                    throw new FileNotFoundException(fileName);
                }

            file_found:
                fileName = Path.GetFullPath(fileName);

                FileName = fileName;
                source = File.ReadAllText(fileName);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                WorkingDirectory = Path.GetDirectoryName(fileName).Replace('\\','/');
#pragma warning restore CS8602 // Dereference of a possibly null reference.

                Row = 1;
                Column = 1;
                position = 0;
            }

            public char ScanChar()
            {
                if (position < source.Length)
                {
                    if (source[position] == '\n')
                    {
                        Row++;
                        Column = 1;
                    }
                    else
                        Column++;
                    return LastChar = source[position++];
                }
                return LastChar = '\0';
            }
        }

        public SourceLocation CurrentLocation => visitorStack.Peek().CurrentLocation;

        private Stack<FileVisitor> visitorStack;
        private SortedSet<string> visitedFiles;

        public Token LastToken { get; private set; }
        private char lastChar => visitorStack.Peek().LastChar;

        private readonly string standardLibraryDirectory;

        public Scanner(string firstFileToVisit, string standardLibraryDirectory)
        {
            this.standardLibraryDirectory = standardLibraryDirectory.Replace('\\', '/');
            visitorStack = new Stack<FileVisitor>();
            visitedFiles = new SortedSet<string>();

            IncludeFile(firstFileToVisit);
            IncludeFile("std.nhp");
            IncludeFile("string.nhp");
            IncludeFile("list.nhp");
            ScanToken();
        }

        public void IncludeFile(string fileName)
        {
            FileVisitor visitor = new(fileName, this);
            if (visitedFiles.Contains(visitor.FileName))
                return;

            visitorStack.Push(visitor);
            visitedFiles.Add(visitor.FileName);
            ScanChar();
        }

        private char ScanChar() => visitorStack.Peek().ScanChar();

        private char ScanCharLiteral()
        {
            char internalScanChar()
            {
                if (lastChar == '\0')
                    throw new UnrecognizedEscapeCharacterException('\0', CurrentLocation);
                else if (lastChar == '\\') //control characters
                {
                    return ScanChar() switch
                    {
                        '\"' => '\"',
                        '\'' => '\'',
                        'a' => '\a',
                        'b' => '\b',
                        'f' => '\f',
                        't' => '\t',
                        'r' => '\r',
                        'n' => '\n',
                        '0' => '\0',
                        _ => throw new UnrecognizedEscapeCharacterException(lastChar, CurrentLocation)
                    };
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
                    return TokenType.OpenBrace;
                case '}':
                    return TokenType.CloseBrace;
                case ',':
                    return TokenType.Comma;
                case ';':
                    return TokenType.Semicolon;
                case ':':
                    {
                        if (lastChar == '=') //walrus operator
                        {
                            ScanChar();
                            return TokenType.Set;
                        }
                        else if(lastChar == ':')
                        {
                            ScanChar();
                            return TokenType.ModuleAccess;
                        }
                        return TokenType.Colon;
                    }
                case '.':
                    return TokenType.Period;
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
                    return TokenType.Caret;
                case '&':
                    return TokenType.BitAnd;
                case '|':
                    return TokenType.BitOr;
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
                    if(visitorStack.Count > 1)
                        visitorStack.Pop();
                    return TokenType.EndOfFile;
                default:
                    throw new UnexpectedCharacterException(symChar, CurrentLocation);
            }
        }

        public Token ScanToken()
        {
            while (lastChar == '\r' || lastChar == ' ')
                ScanChar();

            if (char.IsLetter(lastChar) || lastChar == '_' || lastChar == '@')
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
                    "None" => TokenType.Nothing,
                    "Nothing" => TokenType.Nothing,
                    "nothing" => TokenType.Nothing,
                    "module" => TokenType.Module,
                    "mod" => TokenType.Module,
                    "interface" => TokenType.Interface,
                    "enum" => TokenType.Enum,
                    "class" => TokenType.Record,
                    "record" => TokenType.Record,
                    "lambda" => TokenType.Lambda,
                    "readonly" => TokenType.Readonly,
                    "def" => TokenType.Define,
                    "cdef" => TokenType.CDefine,
                    "match" => TokenType.Match,
                    "while" => TokenType.While,
                    "for" => TokenType.For,
                    "if" => TokenType.If,
                    "elif" => TokenType.Elif,
                    "else" => TokenType.Else,
                    "return" => TokenType.Return,
                    "abort" => TokenType.Abort,
                    "break" => TokenType.Break,
                    "continue" => TokenType.Continue,
                    "pass" => TokenType.Pass,
                    "default" => TokenType.Default,
                    "sizeof" => TokenType.Sizeof,
                    "assert" => TokenType.Assert,
                    "del" => TokenType.Destroy,
                    "destroy" => TokenType.Destroy,
                    "is" => TokenType.Is,
                    "from" => TokenType.From,
                    "to" => TokenType.To,
                    "within" => TokenType.Within,
                    "and" => TokenType.And,
                    "or" => TokenType.Or,
                    "xor" => TokenType.Or,
                    "lshift" => TokenType.ShiftLeft,
                    "rshift" => TokenType.ShiftRight,
                    "new" => TokenType.New,
                    "marshal" => TokenType.Marshal,
                    "flag" => TokenType.Flag,
                    "as" => TokenType.As,
                    "include" => TokenType.Include,
                    "cinclude" => TokenType.CInclude,
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
                if (lastChar == 'd' || lastChar == 'f')
                {
                    ScanChar();
                    return LastToken = new Token(TokenType.DecimalLiteral, numStr);
                }
                return LastToken = new Token(numStr.Contains('.') ? TokenType.DecimalLiteral : TokenType.IntegerLiteral, numStr);
            }
            else if (lastChar == '\'')
            {
                ScanChar();
                LastToken = new Token(TokenType.CharacterLiteral, ScanCharLiteral().ToString());
                if (lastChar != '\'')
                    throw new UnexpectedCharacterException('\'', lastChar, CurrentLocation);
                ScanChar();
                return LastToken;
            }
            else if (lastChar == '\"')
            {
                ScanChar();
                StringBuilder buffer = new();
                while (lastChar != '\"')
                {
                    if (lastChar == '\0')
                        throw new UnexpectedCharacterException('\0', lastChar, CurrentLocation);
                    else
                        buffer.Append(ScanCharLiteral());
                }
                ScanChar();
                return LastToken = new Token(TokenType.StringLiteral, buffer.ToString());
            }
            else if (lastChar == '$')
                return ParseInterpolatedStart();
            else if (lastChar == '#')
            {
                do
                {
                    ScanChar();
                } while (lastChar != '\0' && lastChar != '\n');
                return ScanToken();
            }
            else
            {
                Token? interpolatedTok = ContinueParseInterpolated();
                if (interpolatedTok == null)
                    return LastToken = new Token(ScanSymbol(), string.Empty);
                else
                    return LastToken = interpolatedTok.Value;
            }
        }
    }
}
