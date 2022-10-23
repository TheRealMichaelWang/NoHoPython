using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public partial class CodeBlock : VariableContainer
    {
        public List<IRStatement>? Statements { get; private set; }
        public List<VariableDeclaration> DeclaredVariables { get; private set; }
        public bool IsLoop { get; private set; }

        public CodeBlock(List<IRStatement> statements, bool isLoop, SymbolContainer? parent) : base(parent)
        {
            Statements = statements;
            DeclaredVariables = new List<VariableDeclaration>();
            IsLoop = isLoop;
        }

        public CodeBlock(SymbolContainer? parent, bool isLoop) : base(parent)
        {
            Statements = null;
            DeclaredVariables = new List<VariableDeclaration>();
            IsLoop = isLoop;
        }

        public virtual void DelayedLinkSetStatements(List<IRStatement> statements)
        {
            if (Statements != null)
                throw new InvalidOperationException();
            Statements = statements;
        }

        public List<Variable> GetCurrentLocals()
        {
            if (parentContainer == null || parentContainer is not CodeBlock)
                return DeclaredVariables.ConvertAll((declaration) => declaration.Variable);
            else
            {
                List<Variable> combined = new();
                combined.AddRange(((CodeBlock)parentContainer).GetCurrentLocals());
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
    }
}