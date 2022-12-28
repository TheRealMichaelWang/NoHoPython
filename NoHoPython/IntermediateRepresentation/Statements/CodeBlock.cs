using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;

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
    public partial class CodeBlock : VariableContainer
    {
        public List<IRStatement>? Statements { get; private set; }
        public List<VariableDeclaration> DeclaredVariables { get; private set; }
        public bool IsLoop { get; private set; }

        public int? BreakLabelId { get; private set; }

        public CodeBlock(SymbolContainer parent, bool isLoop) : base(parent)
        {
            Statements = null;
            DeclaredVariables = new List<VariableDeclaration>();
            IsLoop = isLoop;
        }

        public virtual void DelayedLinkSetStatements(List<IRStatement> statements, AstIRProgramBuilder irBuilder)
        {
            if (Statements != null)
                throw new InvalidOperationException();
            Statements = statements;
        }

        public List<Variable> GetCurrentLocals(ProcedureDeclaration currentProcedure)
        {
            if (parentContainer == null || parentContainer is not CodeBlock || this == currentProcedure)
                return DeclaredVariables.ConvertAll((declaration) => declaration.Variable);
            else
            {
                List<Variable> combined = new();
                combined.AddRange(((CodeBlock)parentContainer).GetCurrentLocals(currentProcedure));
                combined.AddRange(DeclaredVariables.ConvertAll((declaration) => declaration.Variable));
                return combined;
            }
        }

        public List<Variable> GetLoopLocals(IAstElement errorReportedElement)
        {
            if (this.IsLoop)
                return DeclaredVariables.ConvertAll((declaration) => declaration.Variable);
            if (parentContainer == null || parentContainer is not CodeBlock)
                throw new UnexpectedLoopStatementException(errorReportedElement);
            else
            {
                List<Variable> combined = new();
                combined.AddRange(((CodeBlock)parentContainer).GetLoopLocals(errorReportedElement));
                combined.AddRange(DeclaredVariables.ConvertAll((declaration) => declaration.Variable));
                return combined;
            }
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
    }
}