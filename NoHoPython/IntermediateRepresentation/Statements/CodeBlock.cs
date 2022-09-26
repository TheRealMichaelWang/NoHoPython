using NoHoPython.Scoping;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public partial class CodeBlock : VariableContainer
    {
        public List<IRStatement>? Statements { get; private set; }
        public List<Variable> DeclaredVariables { get; private set; }

        public CodeBlock(List<IRStatement> statements, SymbolContainer? parent) : base(parent)
        {
            Statements = statements;
            DeclaredVariables = new List<Variable>();
        }

        public CodeBlock(SymbolContainer? parent) : base(parent)
        {
            Statements = null;
            DeclaredVariables = new List<Variable>();
        }

        public virtual void DelayedLinkSetStatements(List<IRStatement> statements)
        {
            if (Statements != null)
                throw new InvalidOperationException();
            Statements = statements;
        }

        public List<Variable> GetCurrentLocals()
        {
            if (base.parentContainer == null)
                return DeclaredVariables;
            else if(base.parentContainer is CodeBlock parentBlock)
            {
                List<Variable> combined = new List<Variable>();
                combined.AddRange(parentBlock.GetCurrentLocals());
                combined.AddRange(DeclaredVariables);
                return combined;
            }
            else
                return DeclaredVariables;
        }
    }
}