using NoHoPython.Scoping;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public partial class CodeBlock : VariableContainer
    {
        public List<IRStatement>? Statements { get; private set; }

        public CodeBlock(List<IRStatement> statements, SymbolContainer? parent) : base(parent)
        {
            Statements = statements;
        }

        public CodeBlock(SymbolContainer? parent) : base(parent)
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