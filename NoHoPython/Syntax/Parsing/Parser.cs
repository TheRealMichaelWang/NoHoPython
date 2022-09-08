using NoHoPython.Syntax.Statements;
using NoHoPython.Syntax.Values;
using System.Text;

namespace NoHoPython.Syntax.Parsing
{
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
                throw new UnexpectedTokenException(expectedLastTokenType, scanner.LastToken, scanner.CurrentLocation);
        }

        private void MatchAndScanToken(TokenType expectedLastTokenType)
        {
            MatchToken(expectedLastTokenType);
            scanner.ScanToken();
        }

        private string parseIdentifier()
        {
            StringBuilder idBuilder = new();

            MatchToken(TokenType.Identifier);
            idBuilder.Append(scanner.LastToken.Identifier);
            scanner.ScanToken();

            while (scanner.LastToken.Type == TokenType.Colon)
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
                        return value is IAstStatement statement ? statement : throw new UnexpectedTokenException(scanner.LastToken, location);
                    }
                case TokenType.Return:
                    scanner.ScanToken();
                    return new ReturnStatement(parseExpression(), location);
                case TokenType.Define:
                    return parseProcedureDeclaration();
                default:
                    throw new UnexpectedTokenException(scanner.LastToken, location);
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
                return lastCountedIndents = count;
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
                    throw new IndentationLevelException(currentExpectedIndents, lastCountedIndents, scanner.CurrentLocation);
            }
            currentExpectedIndents--;
            skipIndentCounting = true;

            return statements;
        }

        private List<IAstStatement> parseCodeBlock() => parseBlock(parseStatement);

        private List<IAstValue> parseArguments()
        {
            MatchAndScanToken(TokenType.OpenParen);

            List<IAstValue> arguments = new();
            while (scanner.LastToken.Type != TokenType.CloseParen)
            {
                arguments.Add(parseExpression());
                if (scanner.LastToken.Type != TokenType.CloseParen)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();

            return arguments;
        }

        private IAstValue parseExpression(int minPrec = 0)
        {
            IAstValue parseValue()
            {
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
                                    return new NamedFunctionCall(identifier, parseArguments(), location);
                                else if (scanner.LastToken.Type == TokenType.OpenParen) //is function call
                                    return new NamedFunctionCall(identifier, parseArguments(), location);
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
                                IntegerLiteral integerLiteral = new(long.Parse(scanner.LastToken.Identifier), location);
                                scanner.ScanToken();
                                return integerLiteral;
                            }
                        case TokenType.DecimalLiteral:
                            {
                                DecimalLiteral decimalLiteral = new(decimal.Parse(scanner.LastToken.Identifier), location);
                                scanner.ScanToken();
                                return decimalLiteral;
                            }
                        case TokenType.CharacterLiteral:
                            {
                                CharacterLiteral characterLiteral = new(scanner.LastToken.Identifier[0], location);
                                scanner.ScanToken();
                                return characterLiteral;
                            }
                        case TokenType.StringLiteral:
                            {
                                ArrayLiteral stringLiteral = new(scanner.LastToken.Identifier, location);
                                scanner.ScanToken();
                                return stringLiteral;
                            }
                        case TokenType.OpenBracket:
                            return parseArrayLiteral();
                        case TokenType.True:
                            scanner.ScanToken();
                            return new TrueLiteral(location);
                        case TokenType.False:
                            scanner.ScanToken();
                            return new FalseLiteral(location);
                        case TokenType.New:
                            scanner.ScanToken();
                            return new InstantiateNewRecord(parseType(), parseArguments(), location);
                        default:
                            throw new UnexpectedTokenException(scanner.LastToken, location);
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
                    else if (scanner.LastToken.Type == TokenType.If)
                    {
                        scanner.ScanToken();
                        IAstValue ifTrue = parseExpression();
                        MatchAndScanToken(TokenType.Else);

                        value = new IfElseValue(value, ifTrue, parseExpression(), value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.As)
                    {
                        scanner.ScanToken();
                        value = new ExplicitCast(value, parseType(), value.SourceLocation);
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
                SourceLocation opLocation = scanner.CurrentLocation;
                scanner.ScanToken();
                value = new BinaryOperator(opTok, value, parseExpression(BinaryOperator.OperatorPrecedence[opTok.Type]), value.SourceLocation, opLocation);
            }
            return value;
        }

        private IAstStatement parseTopLevel()
        {
            switch (scanner.LastToken.Type)
            {
                case TokenType.Define:
                    return parseProcedureDeclaration();
                case TokenType.Enum:
                    return parseEnumDeclaration();
                case TokenType.Interface:
                    return parseInterfaceDeclaration();
                case TokenType.Record:
                    return parseRecordDeclaration();
                case TokenType.Module:
                    return parseModule();
                default:
                    throw new UnexpectedTokenException(scanner.LastToken, scanner.CurrentLocation);
            }
        }

        public List<IAstStatement> ParseAll()
        {
            List<IAstStatement> topLevelStatements = new();
            while (scanner.LastToken.Type != TokenType.EndOfFile)
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
                    default:
                        topLevelStatements.Add(parseTopLevel());
                        break;
                }
            }
            return topLevelStatements;
        }
    }
}
