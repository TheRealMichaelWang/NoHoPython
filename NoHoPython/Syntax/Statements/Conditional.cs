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

        public IfBlock(IAstValue condition, List<IAstStatement> ifTrueBlock, SourceLocation sourceLocation)
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
    }

    public sealed partial class ElseBlock : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }
        public readonly List<IAstStatement> ToExecute;

        public ElseBlock(List<IAstStatement> toExecute, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            ToExecute = toExecute;
        }
    }

    public sealed partial class WhileBlock : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue Condition { get; private set; }
        public readonly List<IAstStatement> ToExecute;

        public WhileBlock(IAstValue condition, List<IAstStatement> toExecute, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;

            Condition = condition;
            ToExecute = toExecute;
        }
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
    }
}

namespace NoHoPython.Syntax.Parsing
{
    public partial class AstParser
    {
        private IfBlock parseIfBlock()
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchToken(TokenType.If);
            scanner.ScanToken();

            IAstValue condititon = parseExpression();
            MatchToken(TokenType.Colon);
            scanner.ScanToken();
            MatchToken(TokenType.Newline);
            scanner.ScanToken();

            return new IfBlock(condititon, parseCodeBlock(), location);
        }

        private IfBlock parseElifBlock(IfBlock parentBlock)
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchToken(TokenType.Elif);
            scanner.ScanToken();

            IAstValue condititon = parseExpression();
            MatchToken(TokenType.Colon);
            scanner.ScanToken();
            MatchToken(TokenType.Newline);
            scanner.ScanToken();

            IfBlock elifBlock = new IfBlock(condititon, parseCodeBlock(), location);
            parentBlock.SetNextIf(elifBlock);
            return elifBlock;
        }

        private ElseBlock parseElseBlock(IfBlock parentBlock)
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchToken(TokenType.Else);
            scanner.ScanToken();
            MatchToken(TokenType.Colon);
            scanner.ScanToken();
            MatchToken(TokenType.Newline);
            scanner.ScanToken();

            return new ElseBlock(parseCodeBlock(), location);
        }

        private IfBlock parseIfElseBlock()
        {
            IfBlock currentBlock = parseIfBlock();
            while (true)
            {
                if (scanner.LastToken.Type == TokenType.Elif)
                {
                    skipIndentCounting = false;
                    currentBlock = parseElifBlock(currentBlock);
                }
                else if (scanner.LastToken.Type == TokenType.Else)
                {
                    skipIndentCounting = false;
                    parseElseBlock(currentBlock);
                    break;
                }
                else
                    break;
            }
            return currentBlock;
        }

        private WhileBlock parseWhileBlock()
        {
            SourceLocation location = scanner.CurrentLocation;
            MatchToken(TokenType.While);
            scanner.ScanToken();

            IAstValue condition = parseExpression();
            MatchToken(TokenType.Colon);
            scanner.ScanToken();
            MatchToken(TokenType.Newline);
            scanner.ScanToken();

            return new WhileBlock(condition, parseCodeBlock(), location);
        }
    }
}
