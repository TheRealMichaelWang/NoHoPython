using NoHoPython.Syntax.Statements;
using NoHoPython.Syntax.Values;
using System.Text;

namespace NoHoPython.Syntax.Parsing
{
    public sealed class UnexpectedTokenException : Exception
    {
        public TokenType? ExpectedTokenType { get; private set; }
        public Token RecievedToken { get; private set; }

        public UnexpectedTokenException(TokenType expectedTokenType, Token recievedToken) : base($"Expected {expectedTokenType} but got {recievedToken.Type} instead.")
        {
            ExpectedTokenType = expectedTokenType;  
            RecievedToken = recievedToken;
        }

        public UnexpectedTokenException(Token recievedToken) : base($"Unexpected token {recievedToken.Type}")
        {
            ExpectedTokenType = null;
            RecievedToken = recievedToken;
        }
    }

    public sealed class IndentationLevelException : Exception
    {
        public int ExpectedIndentationLevel { get; private set; }
        public int ReceivedIndentationLevel { get; private set; }

        public IndentationLevelException(int expectedIndentationLevel, int recievedIndentationLevel) : base($"Expected {expectedIndentationLevel} tabs/indents, but got {recievedIndentationLevel} instead.")
        {
            ExpectedIndentationLevel = expectedIndentationLevel;
            ReceivedIndentationLevel = recievedIndentationLevel;
        }
    }

    public sealed partial class AstParser
    {
        private Scanner scanner;
        private int currentExpectedIndents;

        public AstParser(Scanner scanner)
        {
            this.scanner = scanner;
            currentExpectedIndents = 0;
        }

        private void MatchToken(TokenType expectedLastTokenType)
        {
            if (scanner.LastToken.Type != expectedLastTokenType)
                throw new UnexpectedTokenException(expectedLastTokenType, scanner.LastToken);
        }

        private void MatchAndScanToken(TokenType expectedLastTokenType)
        {
            MatchToken(expectedLastTokenType);
            scanner.ScanToken();
        }

        private string parseIdentifier()
        {
            StringBuilder idBuilder = new StringBuilder();

            MatchToken(TokenType.Identifier);
            idBuilder.Append(scanner.LastToken.Identifier);
            scanner.ScanToken();

            while(scanner.LastToken.Type == TokenType.Colon)
            {
                scanner.ScanToken();
                MatchToken(TokenType.Identifier);
                idBuilder.Append(scanner.LastToken.Identifier);
                scanner.ScanToken();
            }

            return idBuilder.ToString();
        }

        IAstStatement parseStatement()
        {
            SourceLocation location = scanner.CurrentLocation;
            switch (scanner.LastToken.Type)
            {
                case TokenType.If:
                    return parseIfElseBlock();
                case TokenType.While:
                    return parseWhileBlock();
                case TokenType.Identifier:
                    {
                        IAstValue value = parseExpression();
                        if (value is IAstStatement statement)
                            return statement;
                        throw new UnexpectedTokenException(scanner.LastToken);
                    }
                case TokenType.Return:
                    scanner.ScanToken();
                    return new ReturnStatement(parseExpression(), location);
                default:
                    throw new UnexpectedTokenException(scanner.LastToken);
            }
        }

        private bool skipIndentCounting = false;
        private int lastCountedIndents = 0;
        private List<TLineType> parseBlock<TLineType>(Func<TLineType?> lineParser)
        {
            int countIndent()
            {
                int count;
                for (count = 0; scanner.LastToken.Type == TokenType.Tab; count++)
                    scanner.ScanToken();
                return (lastCountedIndents = count);
            }

            currentExpectedIndents++;
            List<TLineType> statements = new();
            while (true)
            {
                if (skipIndentCounting)
                    skipIndentCounting = false;
                else
                    countIndent();

                if (lastCountedIndents == currentExpectedIndents)
                {
                    if (scanner.LastToken.Type == TokenType.Newline)
                    {
                        scanner.ScanToken();
                        continue;
                    }

                    TLineType? result = lineParser();
                    if (result != null)
                        statements.Add(result);

                    if (scanner.LastToken.Type == TokenType.EndOfFile)
                        break;
                    if (!skipIndentCounting)
                        MatchAndScanToken(TokenType.Newline);
                }
                else if (lastCountedIndents < currentExpectedIndents)
                    break;
                else
                    throw new IndentationLevelException(currentExpectedIndents, lastCountedIndents);
            }
            currentExpectedIndents--;
            skipIndentCounting = true;

            return statements;
        }

        private List<IAstStatement> parseCodeBlock() => parseBlock(parseStatement);

        private IAstValue parseExpression(int minPrec = 0)
        {
            IAstValue parseValue()
            {
                List<IAstValue> parseArguments()
                {
                    MatchAndScanToken(TokenType.OpenParen);

                    List<IAstValue> arguments = new List<IAstValue>();
                    while (scanner.LastToken.Type != TokenType.CloseParen)
                    {
                        arguments.Add(parseExpression());
                        if (scanner.LastToken.Type != TokenType.CloseParen)
                            MatchAndScanToken(TokenType.Comma);
                    }
                    scanner.ScanToken();

                    return arguments;
                }

                IAstValue parseFirst()
                {
                    SourceLocation location = scanner.CurrentLocation;
                    switch (scanner.LastToken.Type)
                    {
                        case TokenType.OpenParen:
                            {
                                scanner.ScanToken();
                                IAstValue expression = parseExpression();
                                MatchAndScanToken(TokenType.CloseParen);
                                return expression;
                            }
                        case TokenType.Identifier:
                            {
                                string identifier = parseIdentifier();
                                if (scanner.LastToken.Type == TokenType.OpenBrace)
                                    return new NamedFunctionCall(identifier, parseTypeArguments(), parseArguments(), location);
                                else if (scanner.LastToken.Type == TokenType.OpenParen) //is function call
                                    return new NamedFunctionCall(identifier, new List<AstType>(), parseArguments(), location);
                                else if (scanner.LastToken.Type == TokenType.Set)
                                {
                                    scanner.ScanToken();
                                    return new SetVariable(identifier, parseExpression(), location);
                                }
                                else
                                    return new VariableReference(identifier, location);
                            }
                        case TokenType.IntegerLiteral:
                            {
                                IntegerLiteral integerLiteral = new IntegerLiteral(long.Parse(scanner.LastToken.Identifier), location);
                                scanner.ScanToken();
                                return integerLiteral;
                            }
                        case TokenType.DecimalLiteral:
                            {
                                DecimalLiteral decimalLiteral = new DecimalLiteral(decimal.Parse(scanner.LastToken.Identifier), location);
                                scanner.ScanToken();
                                return decimalLiteral;
                            }
                        case TokenType.CharacterLiteral:
                            {
                                CharacterLiteral characterLiteral = new CharacterLiteral(scanner.LastToken.Identifier[0], location);
                                scanner.ScanToken();
                                return characterLiteral;
                            }
                        case TokenType.StringLiteral:
                            {
                                ArrayLiteral stringLiteral = new ArrayLiteral(scanner.LastToken.Identifier, location);
                                scanner.ScanToken();
                                return stringLiteral;
                            }
                        default:
                            throw new UnexpectedTokenException(scanner.LastToken);
                    }
                }
                IAstValue value = parseFirst();
                while (true)
                {
                    if (scanner.LastToken.Type == TokenType.OpenBracket)
                    {
                        scanner.ScanToken();
                        IAstValue index = parseExpression();
                        MatchAndScanToken(TokenType.CloseBracket);

                        if (scanner.LastToken.Type == TokenType.Set)
                        {
                            scanner.ScanToken();
                            value = new SetValueAtIndex(value, index, parseExpression(), value.SourceLocation);
                        }
                        else
                            value = new GetValueAtIndex(value, index, value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.Period)
                    {
                        scanner.ScanToken();
                        MatchToken(TokenType.Identifier);
                        string propertyIdentifier = scanner.LastToken.Identifier;

                        scanner.ScanToken();
                        if (scanner.LastToken.Type == TokenType.Set)
                        {
                            scanner.ScanToken();
                            value = new SetPropertyValue(value, parseExpression(), propertyIdentifier, value.SourceLocation);
                        }
                        else
                            value = new GetPropertyValue(value, propertyIdentifier, value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.OpenParen)
                        value = new AnonymousFunctionCall(value, parseArguments(), value.SourceLocation);
                    else if(scanner.LastToken.Type == TokenType.If)
                    {
                        scanner.ScanToken();
                        IAstValue ifTrue = parseExpression();
                        MatchAndScanToken(TokenType.Else);

                        value = new IfElseValue(value, ifTrue, parseExpression(), value.SourceLocation);
                    }
                    else
                        break;
                }
                return value;
            }

            IAstValue value = parseValue();

            Token opTok;
            while (BinaryOperator.OperatorPrecedence.ContainsKey((opTok = scanner.LastToken).Type) && BinaryOperator.OperatorPrecedence[opTok.Type] > minPrec)
            {
                scanner.ScanToken();
                value = new BinaryOperator(opTok, value, parseExpression(BinaryOperator.OperatorPrecedence[opTok.Type]), value.SourceLocation);
            }
            return value;
        }

        public List<IAstStatement> ParseAll()
        {
            List<IAstStatement> topLevelStatements = new();
            while(scanner.LastToken.Type != TokenType.EndOfFile)
            {
                skipIndentCounting = false;
                switch (scanner.LastToken.Type)
                {
                    case TokenType.Newline:
                        scanner.ScanToken();
                        continue;
                    case TokenType.Include:
                        {
                            scanner.ScanToken();
                            MatchToken(TokenType.StringLiteral);
                            string file = scanner.LastToken.Identifier;
                            scanner.ScanToken();
                            MatchAndScanToken(TokenType.Newline);
                            scanner.IncludeFile(file);
                            continue;
                        }
                    case TokenType.Define:
                        topLevelStatements.Add(parseProcedureDeclaration());
                        break;
                    case TokenType.Enum:
                        topLevelStatements.Add(parseEnumDeclaration());
                        break;
                    case TokenType.Interface:
                        topLevelStatements.Add(parseInterfaceDeclaration());
                        break;
                    case TokenType.Record:
                        topLevelStatements.Add(parseRecordDeclaration());
                        break;
                    default:
                        throw new UnexpectedTokenException(scanner.LastToken);
                }
            }
            return topLevelStatements;
        }
    }
}
