using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class RefinementContext
    {
        public delegate void RefinementEmitter(IRProgram irProgram, Emitter emitter, Emitter.Promise value, Dictionary<TypeParameter, IType> typeargs);

        public sealed class RefinementEntry
        {
            public (IType, RefinementEmitter?)? Refinement { get; private set; }
            private Dictionary<string, RefinementEntry> propertyRefinements;
            private RefinementEntry? parentEntry;

            public RefinementEntry((IType, RefinementEmitter?)? refinement, Dictionary<string, RefinementEntry> propertyRefinements, RefinementEntry? parentEntry)
            {
                Refinement = refinement;
                this.propertyRefinements = propertyRefinements;
                this.parentEntry = parentEntry;
            }

            public RefinementEntry? GetSubentry(string propertyName) => propertyRefinements.ContainsKey(propertyName) ? propertyRefinements[propertyName] : null;

            public RefinementEntry? NewSubentry(string propertyName)
            {
                if (propertyRefinements.ContainsKey(propertyName))
                    return propertyRefinements[propertyName];

                RefinementEntry newEntry = new(null, new(), null);
                propertyRefinements.Add(propertyName, newEntry);
                return newEntry;
            }

            public void Clear()
            {
                Refinement = null;
                ClearSubRefinments();
                parentEntry?.Clear();
            }

            public void ClearSubRefinments() => propertyRefinements.Clear();

            public void SetRefinement((IType, RefinementEmitter?) refinement)
            {
                if (Refinement.HasValue && Refinement.Value.Item2 != null)
                {
                    if (refinement.Item2 == null)
                        Refinement = (refinement.Item1, Refinement.Value.Item2);
                    else
                        Refinement = (refinement.Item1, (IRProgram irProgram, Emitter emitter, Emitter.Promise value, Dictionary<TypeParameter, IType> typeargs) => refinement.Item2(irProgram, emitter, e => Refinement.Value.Item2(irProgram, emitter, value, typeargs), typeargs));
                }
                else
                    Refinement = refinement;
            }

            public RefinementEntry Clone()
            {
                Dictionary<string, RefinementEntry> newPropertyRefinements = new(propertyRefinements.Count);
                foreach (KeyValuePair<string, RefinementEntry> pair in propertyRefinements)
                    newPropertyRefinements.Add(pair.Key, pair.Value.Clone());
                if (Refinement.HasValue)
                    return new RefinementEntry((Refinement.Value.Item1, Refinement.Value.Item2), newPropertyRefinements, parentEntry ?? this);
                else
                    return new RefinementEntry(null, newPropertyRefinements, parentEntry ?? this);
            }
        }

        private Dictionary<Variable, RefinementEntry> VariableRefinements;

        public RefinementContext(Dictionary<Variable, RefinementEntry> variableRefinements)
        {
            VariableRefinements = variableRefinements;
        }

        public RefinementEntry? GetRefinementEntry(Variable variable)
        {
            if (VariableRefinements.ContainsKey(variable))
                return VariableRefinements[variable];
            return null;
        }

        public void NewRefinementEntry(Variable variable, RefinementEntry entry) => VariableRefinements.Add(variable, entry);

        public RefinementContext Clone()
        {
            Dictionary<Variable, RefinementEntry> newVariableRefinements = new(VariableRefinements.Count);
            foreach (KeyValuePair<Variable, RefinementEntry> pair in VariableRefinements)
                newVariableRefinements.Add(pair.Key, pair.Value.Clone());
            return new(newVariableRefinements);
        }
    }
}

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        public void NewRefinmentContext() => Refinements.Push(Refinements.Count > 0 ? Refinements.Peek().Clone() : new(new()));
    }
}