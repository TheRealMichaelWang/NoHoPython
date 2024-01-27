using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private int loopBreakLabelCount = 0;

        public int GetBreakLabelId() => loopBreakLabelCount++;
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public partial class CodeBlock : SymbolContainer
    {
        public override bool IsGloballyNavigable => false;

        public List<IRStatement>? Statements { get; private set; }
        public List<Variable> LocalVariables { get; private set; }
        private List<VariableDeclaration> DeclaredVariables;

        public bool IsLoop { get; private set; }
        public int? BreakLabelId { get; private set; }

        public SymbolContainer parentContainer { get; private set; }
        public SourceLocation BlockBeginLocation { get; private set; }

        public CodeBlock(SymbolContainer parentContainer, bool isLoop, SourceLocation blockBeginLocation)
        {
            IsLoop = isLoop;
            this.parentContainer = parentContainer;
            BlockBeginLocation = blockBeginLocation;
            Statements = null;
            LocalVariables = new();
            DeclaredVariables = new();
        }

        public void AddVariableDeclaration(VariableDeclaration variableDeclaration)
        {
            DeclaredVariables.Add(variableDeclaration);
            LocalVariables.Add(variableDeclaration.Variable);
        }

        public virtual void DelayedLinkSetStatements(List<IRStatement> statements, AstIRProgramBuilder irBuilder)
        {
            if (Statements != null)
                throw new InvalidOperationException();
            Statements = statements;
        }

        public int GetLoopBreakLabelId(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder)
        {
            if (this.IsLoop)
            {
                if (this.BreakLabelId == null)
                    this.BreakLabelId = irBuilder.GetBreakLabelId();
                return this.BreakLabelId.Value;
            }
            if (parentContainer == null || parentContainer is not CodeBlock)
                throw new UnexpectedLoopStatementException(errorReportedElement);
            else
                return ((CodeBlock)parentContainer).GetLoopBreakLabelId(errorReportedElement, irBuilder);
        }

        public override IScopeSymbol? FindSymbol(string identifier)
        {
            IScopeSymbol? result = base.FindSymbol(identifier);
            return result ?? (parentContainer?.FindSymbol(identifier));
        }
    }
}