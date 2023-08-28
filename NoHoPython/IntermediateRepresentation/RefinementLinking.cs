using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
        public delegate void RefinementEmitter(IRProgram irProgram, IEmitter emitter, string variableIdentifier, Dictionary<TypeParameter, IType> typeargs);

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

        public RefinementEntry? GetRefinementEntry(Variable variable, bool currentContextOnly = false)
        {
            if (VariableRefinements.ContainsKey(variable))
                return VariableRefinements[variable];

            if (parentContainer == null || parentContainer is not CodeBlock || this == variable.ParentProcedure || currentContextOnly)
                return null;
            else
                return ((CodeBlock)parentContainer).GetRefinementEntry(variable);
        }

        public void NewRefinementEntry(Variable variable, RefinementEntry entry) => VariableRefinements.Add(variable, entry);
    }
}
