using NoHoPython.Syntax.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public sealed class AstParser
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

        private List<IAstStatement> parseStatements()
        {
            int countIndent()
            {
                int count;
                for (count = 0; scanner.LastToken.Type == TokenType.Tab; count++)
                    scanner.ScanToken();
                return count;
            }

            bool skipIndentCounting = false;

            IAstStatement parseStatement()
            {

            }

            List<IAstStatement> statements = new List<IAstStatement>();
            while (true)
            {
                if (skipIndentCounting)
                    skipIndentCounting = false;
                else
                {
                    int indentLevel = countIndent();
                    if (indentLevel == currentExpectedIndents)
                        statements.Add(parseStatement());
                    else if (indentLevel == currentExpectedIndents - 1)
                        break;
                    else
                        throw new IndentationLevelException(currentExpectedIndents, indentLevel);
                }
            }
        }

        private IAstValue parseExpression(int minPrec = 0)
        {
            IAstValue parseValue()
            {
                List<IAstValue> parseArguments()
                {
                    MatchToken(TokenType.OpenParen);
                    scanner.ScanToken();

                    List<IAstValue> arguments = new List<IAstValue>();
                    while (scanner.LastToken.Type != TokenType.CloseParen)
                    {
                        arguments.Add(parseExpression());
                        if (scanner.LastToken.Type != TokenType.CloseParen)
                        {
                            MatchToken(TokenType.Comma);
                            scanner.ScanToken();
                        }
                    }
                    scanner.ScanToken();

                    return arguments;
                }

                IAstValue parseFirst()
                {
                    switch (scanner.LastToken.Type)
                    {
                        case TokenType.OpenParen:
                            {
                                scanner.ScanToken();
                                IAstValue expression = parseExpression();
                                MatchToken(TokenType.CloseParen);
                                scanner.ScanToken();
                                return expression;
                            }
                        case TokenType.Identifier:
                            {
                                string identifier = parseIdentifier();
                                if (scanner.LastToken.Type == TokenType.OpenParen) //is function call
                                    return new NamedFunctionCall(identifier, parseArguments(), scanner.CurrentLocation);
                                else if (scanner.LastToken.Type == TokenType.Set)
                                {
                                    SourceLocation location = scanner.CurrentLocation;
                                    scanner.ScanToken();
                                    return new SetVariable(identifier, parseExpression(), location);
                                }
                                else
                                    return new VariableReference(identifier, scanner.CurrentLocation);
                            }
                        case TokenType.IntegerLiteral:
                            {
                                IntegerLiteral integerLiteral = new IntegerLiteral(long.Parse(scanner.LastToken.Identifier), scanner.CurrentLocation);
                                scanner.ScanToken();
                                return integerLiteral;
                            }
                        case TokenType.DecimalLiteral:
                            {
                                DecimalLiteral decimalLiteral = new DecimalLiteral(decimal.Parse(scanner.LastToken.Identifier), scanner.CurrentLocation);
                                scanner.ScanToken();
                                return decimalLiteral;
                            }
                        case TokenType.CharacterLiteral:
                            {
                                CharacterLiteral characterLiteral = new CharacterLiteral(scanner.LastToken.Identifier[0], scanner.CurrentLocation);
                                scanner.ScanToken();
                                return characterLiteral;
                            }
                        case TokenType.StringLiteral:
                            {
                                ArrayLiteral stringLiteral = new ArrayLiteral(scanner.LastToken.Identifier, scanner.CurrentLocation);
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
                        MatchToken(TokenType.CloseBracket);

                        scanner.ScanToken();
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
                        MatchToken(TokenType.Else);
                        scanner.ScanToken();

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
    }
}
