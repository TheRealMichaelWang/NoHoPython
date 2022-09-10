using NoHoPython.Scoping;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public partial class CodeBlock : VariableContainer
    {
        public List<IRStatement>? Statements { get; private set; }

        public CodeBlock(List<IRStatement> statements, SymbolContainer? parent, List<Variable> variables) : base(parent, variables)
        {
            Statements = statements;
        }

        public CodeBlock(SymbolContainer? parent, List<Variable> variables) : base(parent, variables)
        {

        }

        public void DelayedLinkSetStatements(List<IRStatement> statements)
        {
            if (Statements != null)
                throw new InvalidOperationException();
            Statements = statements;
        }
    }
}