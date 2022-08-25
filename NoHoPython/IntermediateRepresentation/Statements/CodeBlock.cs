using NoHoPython.Scoping;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public partial class CodeBlock : VariableContainer
    {
        public readonly List<IRStatement> Statements;

        public CodeBlock(List<IRStatement> statements, SymbolContainer? parent, List<Variable> variables) : base(parent, variables)
        {
            Statements = statements;
        }
    }
}