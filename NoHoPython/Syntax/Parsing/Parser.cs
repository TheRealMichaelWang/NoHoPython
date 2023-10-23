using NoHoPython.Syntax.Statements;
using NoHoPython.Syntax.Values;
using System.Text;

namespace NoHoPython.Syntax.Parsing
{
    public sealed partial class AstParser
    {
        private Scanner scanner;
        private int currentExpectedIndents;
        private List<(string, string[]?)> includedCFiles;

        public AstParser(Scanner scanner)
        {
            this.scanner = scanner;
            currentExpectedIndents = 0;
            includedCFiles = new();
        }

        public void IncludeCFiles(IntermediateRepresentation.IRProgram irProgram) => includedCFiles.ForEach((file) => irProgram.IncludeCFile(file));

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

            while (scanner.LastToken.Type == TokenType.ModuleAccess)
            {
                idBuilder.Append(':');
                scanner.ScanToken();
                MatchToken(TokenType.Identifier);
                idBuilder.Append(scanner.LastToken.Identifier);
                scanner.ScanToken();
            }

            return idBuilder.ToString();
        }

        IAstStatement? ParseStatement()
        {
            SourceLocation location = scanner.CurrentLocation;
            Token actionTok = scanner.LastToken;
            switch (actionTok.Type)
            {
                case TokenType.If:
                    return ParseIfElseBlock();
                case TokenType.While:
                    return ParseWhileBlock();
                case TokenType.For:
                    return ParseForLoop();
                case TokenType.Identifier:
                    {
                        IAstValue value = ParseExpression();
                        return value is IAstStatement statement ? statement : throw new UnexpectedTokenException(scanner.LastToken, location);
                    }
                case TokenType.Return:
                    scanner.ScanToken();
                    if (scanner.LastToken.Type == TokenType.Newline)
                        return new ReturnStatement(new NothingLiteral(location), location);
                    else
                        return new ReturnStatement(ParseExpression(), location);
                case TokenType.Abort:
                    scanner.ScanToken();
                    return new AbortStatement(scanner.LastToken.Type == TokenType.Newline ? null : ParseExpression(), location);
                case TokenType.Assert:
                    scanner.ScanToken();
                    return new AssertStatement(ParseExpression(), location);
                case TokenType.Define:
                case TokenType.Pure:
                    return ParseProcedureDeclaration();
                case TokenType.CDefine:
                    return ParseCDefine();
                case TokenType.Match:
                    return ParseMatchStatement();
                case TokenType.Pass:
                    scanner.ScanToken();
                    return null;
                case TokenType.Break:
                case TokenType.Continue:
                    scanner.ScanToken();
                    return new LoopStatement(actionTok, location);
                case TokenType.Destroy:
                    scanner.ScanToken();
                    return new DestroyStatement(ParseExpression(), location);
                default:
                    throw new UnexpectedTokenException(scanner.LastToken, location);
            }
        }

        private bool skipIndentCounting = false;
        private int lastCountedIndents = 0;
        
        private int CountIndent()
        {
            int count;
            for (count = 0; scanner.LastToken.Type == TokenType.Tab; count++)
                scanner.ScanToken();
            return count;
        }
        
        private List<TLineType> ParseBlock<TLineType>(Func<TLineType?> lineParser)
        {
            currentExpectedIndents++;
            List<TLineType> statements = new();
            while (true)
            {
                if (scanner.LastToken.Type == TokenType.EndOfFile)
                    return statements;

                if (skipIndentCounting)
                    skipIndentCounting = false;
                else
                {
                    int indents = CountIndent();
                    if (scanner.LastToken.Type == TokenType.Newline)
                    {
                        scanner.ScanToken();
                        continue;
                    }
                    else if (scanner.LastToken.Type == TokenType.EndOfFile)
                        return statements;
                    lastCountedIndents = indents;
                }

                if (lastCountedIndents == currentExpectedIndents)
                {
                    if (scanner.LastToken.Type == TokenType.Newline) //empty line
                    {
                        scanner.ScanToken();
                        continue;
                    }
                    else if(scanner.LastToken.Type == TokenType.EndOfFile) //handle end of file
                        return statements;

                    TLineType? result = lineParser();
                    if (result != null)
                        statements.Add(result);

                    if (scanner.LastToken.Type == TokenType.EndOfFile)
                        return statements;

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

        private bool NextLine(TokenType? matchTok = null)
        {
            if (skipIndentCounting)
            {
                skipIndentCounting = false;
                if (matchTok == null)
                    return true;
                return scanner.LastToken.Type == matchTok;
            }
            else
            {
                if (matchTok == null)
                {
                    MatchAndScanToken(TokenType.Newline);
                    int countedIndents = CountIndent();
                    if (scanner.LastToken.Type == TokenType.Newline)
                        return false;

                    lastCountedIndents = countedIndents;
                    if (lastCountedIndents != currentExpectedIndents)
                        throw new IndentationLevelException(currentExpectedIndents, lastCountedIndents, scanner.CurrentLocation);
                    return true;
                }
                else
                {
                    if (scanner.LastToken.Type == TokenType.EndOfFile)
                        return false;
                    MatchAndScanToken(TokenType.Newline);
                    int countedIndents = CountIndent();
                    if (scanner.LastToken.Type == TokenType.Newline)
                        return false;

                    lastCountedIndents = countedIndents;

                    if (lastCountedIndents < currentExpectedIndents || scanner.LastToken.Type != matchTok)
                    {
                        skipIndentCounting = true;
                        return false;
                    }
                    else if (lastCountedIndents > currentExpectedIndents)
                        throw new IndentationLevelException(currentExpectedIndents, lastCountedIndents, scanner.CurrentLocation);
                    return true;
                }
            }
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
                        case TokenType.Lambda:
                        case TokenType.Pure:
                            return ParseLambdaDeclaration(location);
                        case TokenType.OpenParen:
                            {
                                scanner.ScanToken();
                                IAstValue expression = ParseExpression();

                                if (scanner.LastToken.Type == TokenType.CloseParen)
                                {
                                    scanner.ScanToken();
                                    return expression;
                                }
                                else //parse tuple literal
                                {
                                    List<IAstValue> elements = new();
                                    elements.Add(expression);
                                    do
                                    {
                                        MatchAndScanToken(TokenType.Comma);
                                        elements.Add(ParseExpression());
                                    }
                                    while (scanner.LastToken.Type != TokenType.CloseParen);
                                    scanner.ScanToken();
                                    return new TupleLiteral(elements, location);
                                }
                            }
                        case TokenType.Identifier:
                            {
                                string identifier = ParseIdentifier();
                                if (scanner.LastToken.Type == TokenType.OpenParen) //is function call
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
                                StringLiteral stringLiteral = new(scanner.LastToken.Identifier, location);
                                scanner.ScanToken();
                                return stringLiteral;
                            }
                        case TokenType.InterpolatedStart:
                            return ParseInterpolated(location);
                        case TokenType.OpenBracket:
                            return ParseArrayLiteral();
                        case TokenType.True:
                            scanner.ScanToken();
                            return new TrueLiteral(location);
                        case TokenType.False:
                            scanner.ScanToken();
                            return new FalseLiteral(location);
                        case TokenType.New:
                            {
                                scanner.ScanToken();
                                if (scanner.LastToken.Type == TokenType.OpenParen)
                                    return new InstantiateNewRecord(null, ParseArguments(), location);
                                else if (scanner.LastToken.Type == TokenType.OpenBracket)
                                    return ParseAllocArray(null, location);

                                AstType type = ParseType();

                                if (scanner.LastToken.Type == TokenType.OpenParen)
                                    return new InstantiateNewRecord(type, ParseArguments(), location);
                                else
                                    return ParseAllocArray(type, location);
                            }
                        case TokenType.Marshal:
                            return ParseMarshalArray(location);
                        case TokenType.Flag:
                            {
                                scanner.ScanToken();
                                MatchToken(TokenType.StringLiteral);
                                string flag = scanner.LastToken.Identifier;
                                scanner.ScanToken();
                                return new FlagLiteral(location, flag);
                            }
                        case TokenType.Nothing:
                            scanner.ScanToken();
                            return new NothingLiteral(location);
                        case TokenType.Null:
                            scanner.ScanToken();
                            return new NullPointerLiteral(location);
                        case TokenType.Sizeof:
                            {
                                scanner.ScanToken();
                                MatchAndScanToken(TokenType.OpenParen);
                                AstType measureType = ParseType();
                                MatchAndScanToken(TokenType.CloseParen);
                                return new SizeofOperator(measureType, location);
                            }
                        case TokenType.Subtract: //interpret as negate operator
                            {
                                Token sub = scanner.LastToken;
                                scanner.ScanToken();
                                IAstValue toNegate = ParseValue();
                                return new BinaryOperator(sub, new IntegerLiteral(0, location), toNegate, location, toNegate.SourceLocation);
                            }
                        default:
                            throw new UnexpectedTokenException(scanner.LastToken, location);
                    }
                }

                IAstValue value = ParseFirst();
                while (true)
                {
                    if (scanner.LastToken.Type == TokenType.OpenBracket || scanner.LastToken.Type == TokenType.OpenBrace)
                    {
                        MatchAndScanToken(TokenType.OpenBracket);
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
                        IAstValue condition = ParseExpression();
                        MatchAndScanToken(TokenType.Else);

                        value = new IfElseValue(condition, value, ParseExpression(), value.SourceLocation);
                    }
                    else if (scanner.LastToken.Type == TokenType.As)
                    {
                        scanner.ScanToken();
                        value = new ExplicitCast(value, ParseType(), value.SourceLocation);
                    }
                    else if(scanner.LastToken.Type == TokenType.Is)
                    {
                        scanner.ScanToken();
                        value = new CheckEnumOption(value, ParseType(), value.SourceLocation);
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
                case TokenType.Pure:
                case TokenType.AffectsArgs:
                case TokenType.AffectsCaptured:
                case TokenType.Impure:
                    return ParseProcedureDeclaration();
                case TokenType.CDefine:
                    return ParseCDefine();
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

            while (true)
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
                            MatchToken(TokenType.Newline);
                            scanner.IncludeFile(file);
                            continue;
                        }
                    case TokenType.CInclude:
                        {
                            scanner.ScanToken();
                            MatchToken(TokenType.StringLiteral);
                            string includeSource = scanner.LastToken.Identifier;
                            scanner.ScanToken();

                            if (scanner.LastToken.Type == TokenType.OpenParen)
                            {
                                scanner.ScanToken();
                                List<string> preincludeDefinitions = new();
                                while (scanner.LastToken.Type != TokenType.CloseParen)
                                {
                                    MatchToken(TokenType.Identifier);
                                    preincludeDefinitions.Add(scanner.LastToken.Identifier);
                                    scanner.ScanToken();
                                    if (scanner.LastToken.Type != TokenType.CloseParen)
                                        MatchAndScanToken(TokenType.Comma);
                                }
                                scanner.ScanToken();
                                includedCFiles.Add((includeSource, preincludeDefinitions.ToArray()));
                            }
                            else
                                includedCFiles.Add((includeSource, null));

                            MatchToken(TokenType.Newline);
                            continue;
                        }
                    case TokenType.EndOfFile:
                        lastCountedIndents = 0;
                        currentExpectedIndents = 0;
                        scanner.ScanToken();
                        if (scanner.LastToken.Type == TokenType.EndOfFile)
                            return topLevelStatements;
                        break;
                    default:
                        topLevelStatements.Add(ParseTopLevel());
                        break;
                }
            }
        }
    }
}
