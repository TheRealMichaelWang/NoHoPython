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
            public (IType, RefinementEmitter?)? Refinement { get; private set; }
            private Dictionary<string, RefinementEntry> propertyRefinements;
            private RefinementEntry? parentEntry;
            private RefinementContext context;

            public RefinementEntry((IType, RefinementEmitter?)? refinement, Dictionary<string, RefinementEntry> propertyRefinements, RefinementEntry? parentEntry, RefinementContext context)
            {
                Refinement = refinement;
                this.propertyRefinements = propertyRefinements;
                this.parentEntry = parentEntry;
                this.context = context;
            }

            public RefinementEntry? GetSubentry(string propertyName) => propertyRefinements.ContainsKey(propertyName) ? propertyRefinements[propertyName] : null;

            public RefinementEntry? NewSubentry(string propertyName)
            {
                if (propertyRefinements.ContainsKey(propertyName))
                    return null;

                RefinementEntry newEntry = new(null, new(), null, context);
                propertyRefinements.Add(propertyName, newEntry);
                return newEntry;
            }

            public void Clear()
            {
                Refinement = null;
                ClearSubRefinements();
                
                if(parentEntry != null)
                    parentEntry.context.QueueRefinement(parentEntry, null);
            }

            public void ClearSubRefinements()
            {
                foreach(RefinementEntry entry in propertyRefinements.Values)
                    entry.Clear();
                propertyRefinements.Clear();
            }

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

            public void SetRefinement((IType, RefinementEmitter?)? refinement)
            {
                if (refinement.HasValue)
                    SetRefinement(refinement.Value);
                else
                    Clear();
            }

            public RefinementEntry Clone(RefinementContext newContext)
            {
                Dictionary<string, RefinementEntry> newPropertyRefinements = new(propertyRefinements.Count);
                foreach (KeyValuePair<string, RefinementEntry> pair in propertyRefinements)
                    newPropertyRefinements.Add(pair.Key, pair.Value.Clone(newContext));
                if (Refinement.HasValue)
                    return new RefinementEntry(Refinement.Value, newPropertyRefinements, this, newContext);
                else
                    return new RefinementEntry(null, newPropertyRefinements, this, newContext);
            }
        }

        private Dictionary<Variable, RefinementEntry> VariableRefinements;
        private Queue<(RefinementEntry, (IType, RefinementEmitter?)?)> refinementsToApply;

        public RefinementContext(Dictionary<Variable, RefinementEntry> variableRefinements)
        {
            VariableRefinements = variableRefinements;
            refinementsToApply = new();
        }

        public RefinementEntry? GetRefinementEntry(Variable variable)
        {
            if (VariableRefinements.ContainsKey(variable))
                return VariableRefinements[variable];
            return null;
        }

        public RefinementEntry NewRefinementEntry(Variable variable, (IType, RefinementEmitter?)? refinement = null)
        {
            RefinementEntry entry = new(refinement, new(), null, this);
            VariableRefinements.Add(variable, entry);
            return entry;
        }

        public RefinementContext Clone()
        {
            RefinementContext context = new(new(VariableRefinements.Count));
            foreach (KeyValuePair<Variable, RefinementEntry> pair in VariableRefinements)
                context.VariableRefinements.Add(pair.Key, pair.Value.Clone(context));
            return context;
        }

        public void QueueRefinement(RefinementEntry entry, (IType, RefinementEmitter?)? refinement) => refinementsToApply.Enqueue((entry, refinement));

        public void ApplyQueuedRefinements()
        {
            while(refinementsToApply.Count > 0)
            {
                var toapply = refinementsToApply.Dequeue();
                toapply.Item1.SetRefinement(toapply.Item2);
            }
        }

        public void DiscardQueuedRefinements() => refinementsToApply.Clear();
    }
}

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        public RefinementContext? NewRefinmentContext(bool noParent = false)
        {
            if(Refinements.Count > 0 && !noParent)
            {
                RefinementContext oldContext = Refinements.Peek();
                Refinements.Push(oldContext.Clone());
                return oldContext;
            }
            else
            {
                Refinements.Push(new(new()));
                return null;
            }
        }

        public void PopAndApplyRefinementContext()
        {
            Refinements.Pop();
            if(Refinements.Count > 0)
                Refinements.Peek().ApplyQueuedRefinements();
        }
    }
}