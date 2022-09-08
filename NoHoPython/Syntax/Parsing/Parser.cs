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
            _ = scanner.ScanToken();
        }

        private string parseIdentifier()
        {
            StringBuilder idBuilder = new();

            MatchToken(TokenType.Identifier);
            _ = idBuilder.Append(scanner.LastToken.Identifier);
            _ = scanner.ScanToken();

            while (scanner.LastToken.Type == TokenType.Colon)
            {
                _ = scanner.ScanToken();
                MatchToken(TokenType.Identifier);
                _ = idBuilder.Append(scanner.LastToken.Identifier);
                _ = scanner.ScanToken();
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
                    _ = scanner.ScanToken();
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
                    _ = scanner.ScanToken();
                return lastCountedIndents = count;
            }

            currentExpectedIndents++;
            List<TLineType> statements = new();
            while (true)
            {
                if (skipIndentCounting)
                    skipIndentCounting = false;
                else
                    _ = countIndent();

                if (lastCountedIndents == currentExpectedIndents)
                {
                    if (scanner.LastToken.Type == TokenType.Newline)
                    {
                        _ = scanner.ScanToken();
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
            _ = scanner.ScanToken();

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
                                _ = scanner.ScanToken();
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
                                    _ = scanner.ScanToken();
                                    return new SetVariable(identifier, parseExpression(), location);
                                }
                                else
                                    return new VariableReference(identifier, location);
                            }
                        case TokenType.IntegerLiteral:
                            {
                                IntegerLiteral integerLiteral = new(long.Parse(scanner.LastToken.Identifier), location);
                                _ = scanner.ScanToken();
                                return integerLiteral;
                            }
                        case TokenType.DecimalLiteral:
                            {
                                DecimalLiteral decimalLiteral = new(decimal.Parse(scanner.LastToken.Identifier), location);
                                _ = scanner.ScanToken();
                                return decimalLiteral;
                            }
                        case TokenType.CharacterLiteral:
                            {
                                CharacterLiteral characterLiteral = new(scanner.LastToken.Identifier[0], location);
                                _ = scanner.ScanToken();
                                return characterLiteral;
                            }
                        case TokenType.StringLiteral:
                            {
                                ArrayLiteral stringLiteral = new(scanner.LastToken.Identifier, location);
                                _ = scanner.ScanToken();
                                return stringLiteral;
                            }
                        case TokenType.True:
                            _ = scanner.ScanToken();
                            return new TrueLiteral(location);
                        case TokenType.False:
                            _ = scanner.ScanToken();
                            return new FalseLiteral(location);
                        case TokenType.New:
                            _ = scanner.ScanToken();
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
                        _ = scanner.ScanToken();
                        IAstValue index = parseExpression();
                        MatchAndScanToken(TokenType.CloseBracket);

                        if (scanner.LastToken.Type == TokenType.Set)
                        {
                            _ = scanner.ScanToken();
                            value = new SetValueAtIndex(value, index, parseExpression(), value.SourceLocation);
                        }
                        else
                            value = new GetValueAtIndex(value, index, value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.Period)
                    {
                        _ = scanner.ScanToken();
                        MatchToken(TokenType.Identifier);
                        string propertyIdentifier = scanner.LastToken.Identifier;

                        _ = scanner.ScanToken();
                        if (scanner.LastToken.Type == TokenType.Set)
                        {
                            _ = scanner.ScanToken();
                            value = new SetPropertyValue(value, parseExpression(), propertyIdentifier, value.SourceLocation);
                        }
                        else
                            value = new GetPropertyValue(value, propertyIdentifier, value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.OpenParen)
                        value = new AnonymousFunctionCall(value, parseArguments(), value.SourceLocation);
                    else if (scanner.LastToken.Type == TokenType.If)
                    {
                        _ = scanner.ScanToken();
                        IAstValue ifTrue = parseExpression();
                        MatchAndScanToken(TokenType.Else);

                        value = new IfElseValue(value, ifTrue, parseExpression(), value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.As)
                    {
                        _ = scanner.ScanToken();
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
                _ = scanner.ScanToken();
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
                        _ = scanner.ScanToken();
                        continue;
                    case TokenType.Include:
                        {
                            _ = scanner.ScanToken();
                            MatchToken(TokenType.StringLiteral);
                            string file = scanner.LastToken.Identifier;
                            _ = scanner.ScanToken();
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
