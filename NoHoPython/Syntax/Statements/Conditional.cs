using NoHoPython.Syntax.Statements;

namespace NoHoPython.Syntax.Statements
{
    public sealed partial class IfBlock : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Condition { get; private set; }

        public readonly List<IAstStatement> IfTrueBlock;
        public IfBlock? NextIf { get; private set; }
        public ElseBlock? NextElse { get; private set; }

#pragma warning disable CS8618 // null feilds initialized during IR generation
        public IfBlock(IAstValue condition, List<IAstStatement> ifTrueBlock, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            Condition = condition;
            IfTrueBlock = ifTrueBlock;
            NextIf = null;
            NextElse = null;

            SourceLocation = sourceLocation;
        }

        public void SetNextIf(IfBlock ifBlock)
        {
            if (NextIf != null || NextElse != null)
                throw new InvalidOperationException();

            NextIf = ifBlock;
        }

        public void SetNextElse(ElseBlock elseBlock)
        {
            if (NextIf != null || NextElse != null)
                throw new InvalidOperationException();

            NextElse = elseBlock;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}if {Condition}:\n{IAstStatement.BlockToString(indent, IfTrueBlock)}{(NextIf == null ? NextElse == null ? string.Empty : "\n" + NextElse.ToString(indent) : "\n" + NextIf.ToElifString(indent))}";

        private string ToElifString(int indent) => $"{IAstStatement.Indent(indent)}elif {Condition}:\n{IAstStatement.BlockToString(indent, IfTrueBlock)}{(NextIf == null ? NextElse == null ? string.Empty : "\n" + NextElse.ToString(indent) : "\n" + NextIf.ToElifString(indent))}";
    }

    public sealed partial class ElseBlock : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }
        public readonly List<IAstStatement> ToExecute;

#pragma warning disable CS8618 // null feilds initialized during IR generation
        public ElseBlock(List<IAstStatement> toExecute, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            ToExecute = toExecute;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}else:\n{IAstStatement.BlockToString(indent, ToExecute)}";
    }

    public sealed partial class WhileBlock : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Condition { get; private set; }
        public readonly List<IAstStatement> ToExecute;

#pragma warning disable CS8618 // null feilds initialized during IR generation
        public WhileBlock(IAstValue condition, List<IAstStatement> toExecute, SourceLocation sourceLocation)
#pragma warning restore CS8618
        {
            SourceLocation = sourceLocation;

            Condition = condition;
            ToExecute = toExecute;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}while {Condition}:\n{IAstStatement.BlockToString(indent, ToExecute)}";
    }

    public sealed partial class MatchStatement : IAstStatement
    {
        public sealed partial class MatchHandler
        {
            public AstType MatchType { get; private set; }
            public string? MatchIdentifier { get; private set; }

            public readonly List<IAstStatement> Statements;

            public MatchHandler(AstType matchType, string? matchIdentifier, List<IAstStatement> statements)
            {
                MatchType = matchType;
                MatchIdentifier = matchIdentifier;
                Statements = statements;
            }

            public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{MatchType}{(MatchIdentifier == null ? string.Empty : $" {MatchIdentifier}")}:\n{IAstStatement.BlockToString(indent, Statements)}";
        }

        public SourceLocation SourceLocation { get; private set; }

        public IAstValue MatchedValue { get; private set; }
        public readonly List<MatchHandler> MatchHandlers;

#pragma warning disable CS8618 // null feilds initialized during IR generation
        public MatchStatement(IAstValue matchedValue, List<MatchHandler> matchHandlers, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            MatchedValue = matchedValue;
            MatchHandlers = matchHandlers;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}match {MatchedValue}:{string.Join("",MatchHandlers.ConvertAll((statement) => $"\n{statement.ToString(indent + 1)}"))}";
    }

    public sealed partial class AssertStatement : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Condition { get; private set; }

        public AssertStatement(IAstValue condition, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Condition = condition;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}assert {Condition}";
    }
}

namespace NoHoPython.Syntax.Values
{
    public sealed partial class IfElseValue : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Condition { get; private set; }
        public IAstValue IfTrueValue { get; private set; }
        public IAstValue IfFalseValue { get; private set; }

        public IfElseValue(IAstValue condition, IAstValue ifTrueValue, IAstValue ifFalseValue, SourceLocation sourceLocation)
        {
            Condition = condition;
            IfTrueValue = ifTrueValue;
            IfFalseValue = ifFalseValue;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"{Condition} if {IfTrueValue} else {IfFalseValue}";
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private IfBlock ParseIfBlock()
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchAndScanToken(TokenType.If);

            IAstValue condititon = ParseExpression();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            return new IfBlock(condititon, ParseCodeBlock(), location);
        }

        private IfBlock ParseElifBlock(IfBlock parentBlock)
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchAndScanToken(TokenType.Elif);

            IAstValue condititon = ParseExpression();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            skipIndentCounting = true;
            IfBlock elifBlock = new(condititon, ParseCodeBlock(), location);
            parentBlock.SetNextIf(elifBlock);
            return elifBlock;
        }

        private ElseBlock ParseElseBlock(IfBlock parentBlock)
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchAndScanToken(TokenType.Else);
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            skipIndentCounting = true;
            ElseBlock elseBlock = new(ParseCodeBlock(), location);
            parentBlock.SetNextElse(elseBlock);
            return elseBlock;
        }

        private IfBlock ParseIfElseBlock()
        {
            int expectedIndents = currentExpectedIndents;
            IfBlock head = ParseIfBlock();
            IfBlock currentBlock = head;
            while (lastCountedIndents == expectedIndents)
            {
                if (scanner.LastToken.Type == TokenType.Elif)
                {
                    currentBlock = ParseElifBlock(currentBlock);
                    continue;
                }
                else if (scanner.LastToken.Type == TokenType.Else)
                    ParseElseBlock(currentBlock);
                else
                    break;
            }
            return head;
        }

        private WhileBlock ParseWhileBlock()
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchAndScanToken(TokenType.While);

            IAstValue condition = ParseExpression();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            return new WhileBlock(condition, ParseCodeBlock(), location);
        }

        private MatchStatement ParseMatchStatement()
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchAndScanToken(TokenType.Match);

            IAstValue toMatch = ParseExpression();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<MatchStatement.MatchHandler> handlers = ParseBlock(() =>
            {
                AstType type = ParseType();

                string? identifier = null;
                if (scanner.LastToken.Type == TokenType.Identifier)
                {
                    identifier = scanner.LastToken.Identifier;
                    scanner.ScanToken();
                }

                MatchAndScanToken(TokenType.Colon);
                MatchAndScanToken(TokenType.Newline);
                return new MatchStatement.MatchHandler(type, identifier, ParseCodeBlock());
            });

            return new MatchStatement(toMatch, handlers, location);
        }
    }
}
