using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class RefinementContext
    {
        public delegate void RefinementEmitter(IRProgram irProgram, Emitter emitter, Emitter.Promise value, Dictionary<TypeParameter, IType> typeargs);

        public sealed class RefinementEntry
        {
            public (IType, RefinementEmitter?)? Refinement { get; set; }
            private Dictionary<string, RefinementEntry> propertyRefinements;

            public RefinementEntry((IType, RefinementEmitter?)? refinement, Dictionary<string, RefinementEntry> propertyRefinements)
            {
                Refinement = refinement;
                this.propertyRefinements = propertyRefinements;
            }

            public RefinementEntry? GetSubentry(string propertyName) => propertyRefinements.ContainsKey(propertyName) ? propertyRefinements[propertyName] : null;

            public RefinementEntry? NewSubentry(string propertyName)
            {
                if (propertyRefinements.ContainsKey(propertyName))
                    return propertyRefinements[propertyName];

                RefinementEntry newEntry = new(null, new());
                propertyRefinements.Add(propertyName, newEntry);
                return newEntry;
            }

            public void Clear()
            {
                Refinement = null;
                propertyRefinements.Clear();
            }
        }

        private Dictionary<Variable, RefinementEntry> VariableRefinements;
        private RefinementContext? PreviousContext;

        public RefinementContext(RefinementContext? previousContext)
        {
            VariableRefinements = new();
            PreviousContext = previousContext;
        }

        public RefinementEntry? GetRefinementEntry(Variable variable, bool currentContextOnly = false)
        {
            if (VariableRefinements.ContainsKey(variable))
                return VariableRefinements[variable];

            if (PreviousContext == null || currentContextOnly)
                return null;
            return PreviousContext.GetRefinementEntry(variable);
        }

        public void NewRefinementEntry(Variable variable, RefinementEntry entry) => VariableRefinements.Add(variable, entry);
    }
}

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        public void NewRefinmentContext() => Refinements.Push(new RefinementContext(Refinements.Count > 0 ? Refinements.Peek() : null));
    }
}