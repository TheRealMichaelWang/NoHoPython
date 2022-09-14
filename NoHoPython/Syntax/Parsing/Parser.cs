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

        private string ParseIdentifier()
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

        IAstStatement ParseStatement()
        {
            SourceLocation location = scanner.CurrentLocation;
            switch (scanner.LastToken.Type)
            {
                case TokenType.If:
                    return ParseIfElseBlock();
                case TokenType.While:
                    return ParseWhileBlock();
                case TokenType.Identifier:
                    {
                        IAstValue value = ParseExpression();
                        return value is IAstStatement statement ? statement : throw new UnexpectedTokenException(scanner.LastToken, location);
                    }
                case TokenType.Return:
                    scanner.ScanToken();
                    return new ReturnStatement(ParseExpression(), location);
                case TokenType.Define:
                    return ParseProcedureDeclaration();
                default:
                    throw new UnexpectedTokenException(scanner.LastToken, location);
            }
        }

        private bool skipIndentCounting = false;
        private int lastCountedIndents = 0;
        private List<TLineType> ParseBlock<TLineType>(Func<TLineType?> lineParser)
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

        private List<IAstStatement> ParseCodeBlock() => ParseBlock(ParseStatement);

        private List<IAstValue> ParseArguments()
        {
            MatchAndScanToken(TokenType.OpenParen);

            List<IAstValue> arguments = new();
            while (scanner.LastToken.Type != TokenType.CloseParen)
            {
                arguments.Add(ParseExpression());
                if (scanner.LastToken.Type != TokenType.CloseParen)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();

            return arguments;
        }

        private IAstValue ParseExpression(int minPrec = 0)
        {
            IAstValue ParseValue()
            {
                IAstValue ParseFirst()
                {
                    SourceLocation location = scanner.CurrentLocation;
                    switch (scanner.LastToken.Type)
                    {
                        case TokenType.OpenParen:
                            {
                                scanner.ScanToken();
                                IAstValue expression = ParseExpression();
                                MatchAndScanToken(TokenType.CloseParen);
                                return expression;
                            }
                        case TokenType.Identifier:
                            {
                                string identifier = ParseIdentifier();
                                if (scanner.LastToken.Type == TokenType.OpenBrace)
                                    return new NamedFunctionCall(identifier, ParseArguments(), location);
                                else if (scanner.LastToken.Type == TokenType.OpenParen) //is function call
                                    return new NamedFunctionCall(identifier, ParseArguments(), location);
                                else if (scanner.LastToken.Type == TokenType.Set)
                                {
                                    scanner.ScanToken();
                                    return new SetVariable(identifier, ParseExpression(), location);
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
                            return ParseArrayLiteral();
                        case TokenType.True:
                            scanner.ScanToken();
                            return new TrueLiteral(location);
                        case TokenType.False:
                            scanner.ScanToken();
                            return new FalseLiteral(location);
                        case TokenType.New:
                            scanner.ScanToken();
                            return new InstantiateNewRecord(ParseType(), ParseArguments(), location);
                        case TokenType.Nothing:
                            return new NothingLiteral(location);
                        default:
                            throw new UnexpectedTokenException(scanner.LastToken, location);
                    }
                }
                IAstValue value = ParseFirst();
                while (true)
                {
                    if (scanner.LastToken.Type == TokenType.OpenBracket)
                    {
                        scanner.ScanToken();
                        IAstValue index = ParseExpression();
                        MatchAndScanToken(TokenType.CloseBracket);

                        if (scanner.LastToken.Type == TokenType.Set)
                        {
                            scanner.ScanToken();
                            value = new SetValueAtIndex(value, index, ParseExpression(), value.SourceLocation);
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
                            value = new SetPropertyValue(value, ParseExpression(), propertyIdentifier, value.SourceLocation);
                        }
                        else
                            value = new GetPropertyValue(value, propertyIdentifier, value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.OpenParen)
                        value = new AnonymousFunctionCall(value, ParseArguments(), value.SourceLocation);
                    else if (scanner.LastToken.Type == TokenType.If)
                    {
                        scanner.ScanToken();
                        IAstValue ifTrue = ParseExpression();
                        MatchAndScanToken(TokenType.Else);

                        value = new IfElseValue(value, ifTrue, ParseExpression(), value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.As)
                    {
                        scanner.ScanToken();
                        value = new ExplicitCast(value, ParseType(), value.SourceLocation);
                    }
                    else
                        break;
                }
                return value;
            }

            IAstValue value = ParseValue();

            Token opTok;
            while (BinaryOperator.OperatorPrecedence.ContainsKey((opTok = scanner.LastToken).Type) && BinaryOperator.OperatorPrecedence[opTok.Type] > minPrec)
            {
                SourceLocation opLocation = scanner.CurrentLocation;
                scanner.ScanToken();
                value = new BinaryOperator(opTok, value, ParseExpression(BinaryOperator.OperatorPrecedence[opTok.Type]), value.SourceLocation, opLocation);
            }
            return value;
        }

        private IAstStatement ParseTopLevel()
        {
            switch (scanner.LastToken.Type)
            {
                case TokenType.Define:
                    return ParseProcedureDeclaration();
                case TokenType.Enum:
                    return ParseEnumDeclaration();
                case TokenType.Interface:
                    return ParseInterfaceDeclaration();
                case TokenType.Record:
                    return ParseRecordDeclaration();
                case TokenType.Module:
                    return ParseModule();
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
                        topLevelStatements.Add(ParseTopLevel());
                        break;
                }
            }
            return topLevelStatements;
        }
    }
}
