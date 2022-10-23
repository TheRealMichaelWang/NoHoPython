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
            public SourceLocation CurrentLocation => new(Row, Column, FileName);

            public readonly string FileName;
            public readonly string WorkingDirectory;

            public int Row { get; private set; }
            public int Column { get; private set; }
            public char LastChar { get; private set; }

            private readonly string source;
            private int position;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public FileVisitor(string fileName, Scanner scanner)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            {
                if (!File.Exists(fileName))
                {
                    if (File.Exists(scanner.standardLibraryDirectory + "\\" + fileName))
                    {
                        fileName = scanner.standardLibraryDirectory + "\\" + fileName;
                        goto file_found;
                    }
                    else if (scanner.visitorStack.Count > 0)
                    {
                        FileVisitor parent = scanner.visitorStack.Peek();
                        if (File.Exists(parent.WorkingDirectory + "\\" + fileName))
                        {
                            fileName = parent.WorkingDirectory + "\\" + fileName;
                            goto file_found;
                        }
                    }
                    throw new FileNotFoundException(fileName);
                }

            file_found:
                fileName = Path.GetFullPath(fileName);

                FileName = fileName;
                source = File.ReadAllText(fileName);
#pragma warning disable CS8601 // Possible null reference assignment.
                WorkingDirectory = Path.GetDirectoryName(fileName);
#pragma warning restore CS8601

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
            this.standardLibraryDirectory = standardLibraryDirectory;
            visitorStack = new Stack<FileVisitor>();
            visitedFiles = new SortedSet<string>();

            IncludeFile(firstFileToVisit);
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
                    throw new UnrecognizedEscapeCharacterException('\0');
                else if (lastChar == '\\') //control characters
                {
                    char c = ScanChar();
                    switch (c)
                    {
                        case '\"':
                            return '\"';
                        case '\'':
                            return '\'';
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
                    throw new UnexpectedCharacterException(symChar);
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
                    "assert" => TokenType.Assert,
                    "in" => TokenType.In,
                    "and" => TokenType.And,
                    "or" => TokenType.Or,
                    "new" => TokenType.New,
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
                return LastToken = new Token(numStr.Contains('.') ? TokenType.DecimalLiteral : TokenType.IntegerLiteral, numStr);
            }
            else if (lastChar == '\'')
            {
                ScanChar();
                LastToken = new Token(TokenType.CharacterLiteral, ScanCharLiteral().ToString());
                if (lastChar != '\'')
                    throw new UnexpectedCharacterException('\'', lastChar);
                ScanChar();
                return LastToken;
            }
            else if (lastChar == '\"')
            {
                string buffer = string.Empty;
                ScanChar();
                while (lastChar != '\"')
                    buffer += ScanCharLiteral();
                ScanChar();
                return LastToken = new Token(TokenType.StringLiteral, buffer);
            }
            else if(lastChar == '#')
            {
                do
                {
                    ScanChar();
                } while (lastChar != '\0' && lastChar != '\n');
                return ScanToken();
            }    
            else
                return LastToken = new Token(ScanSymbol(), string.Empty);
        }
    }
}
