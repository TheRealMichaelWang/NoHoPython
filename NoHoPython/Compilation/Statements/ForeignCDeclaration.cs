using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<ForeignCType> usedForeignCTypes = new(new ITypeComparer());
        private Dictionary<ForeignCDeclaration, List<ForeignCType>> foreignTypeOverloads = new();

        public bool DeclareUsedForeignCType(ForeignCType foreignCType)
        {
            if (usedForeignCTypes.Contains(foreignCType))
                return false;

            usedForeignCTypes.Add(foreignCType);
            if (!foreignTypeOverloads.ContainsKey(foreignCType.Declaration))
                foreignTypeOverloads.Add(foreignCType.Declaration, new());
            foreignTypeOverloads[foreignCType.Declaration].Add(foreignCType);

            DeclareTypeDependencies(foreignCType, foreignCType.GetProperties().ConvertAll((prop) => prop.Type).ToArray());
            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public readonly Dictionary<ForeignCDeclaration, List<ForeignCType>> ForeignTypeOverloads;

        public void ForwardDeclareForeignTypes(StatementEmitter emitter)
        {
            foreach (ForeignCDeclaration foreignDeclaration in ForeignTypeOverloads.Keys)
            {
                if (foreignDeclaration.ForwardDeclaration == null)
                    continue;

                foreach (ForeignCType foreign in ForeignTypeOverloads[foreignDeclaration])
                        emitter.AppendLine(foreign.GetSource(foreignDeclaration.ForwardDeclaration, this));
            }
        }
    }

    public sealed class ForeignInlineCError : CodegenError
    {
        public string InlineCSource { get; private set; }
        public int Index { get; private set; }
        public string InlineMessage { get; private set; }

        public ForeignInlineCError(string inlineCSource, int index, string inlineMessage, ForeignCType foreignCType):base(foreignCType.Declaration, "An error occured with your inline C code.")
        {
            InlineCSource = inlineCSource;
            Index = index;
            InlineMessage = inlineMessage;
        }

        public override void Print()
        {
            base.Print();

        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class ForeignCDeclaration
    {
        partial class ForeignCProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

            public override bool EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource, string responsibleDestroyer)
            {
                ForeignCType foreignCType = (ForeignCType)propertyContainer;

                emitter.Append(valueCSource);
                emitter.Append(foreignCType.Declaration.PointerPropertyAccess ? "->" : ".");
                emitter.Append(Name);
                
                return false;
            }
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.ForeignTypeOverloads.ContainsKey(this))
                return;
            if (CStructDeclaration == null)
                return;

            foreach (ForeignCType foreignCType in irProgram.ForeignTypeOverloads[this])
                emitter.AppendLine(foreignCType.GetSource(CStructDeclaration, irProgram));
        }

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.ForeignTypeOverloads.ContainsKey(this))
                return;
            if (MarshallerHeaders == null)
                return;

            foreach (ForeignCType foreignCType in irProgram.ForeignTypeOverloads[this])
                emitter.AppendLine(foreignCType.GetSource(MarshallerHeaders, irProgram));
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.ForeignTypeOverloads.ContainsKey(this))
                return;
            if (MarshallerDeclarations == null)
                return;

            foreach (ForeignCType foreignCType in irProgram.ForeignTypeOverloads[this])
                emitter.AppendLine(foreignCType.GetSource(MarshallerDeclarations, irProgram));
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ForeignCType
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => Declaration.Destructor != null;
        public bool MustSetResponsibleDestroyer => Declaration.ResponsibleDestroyerSetter != null;
        
        public bool IsTypeDependency
        {
            get
            {
                if (Declaration.PointerPropertyAccess)
                    return false;

                return Declaration.CStructDeclaration != null;
            }
        }

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => Declaration.TypeParameters.Count > 0;

        public string GetStandardIdentifier(IRProgram irProgram) => $"nhp_foreign_{IScopeSymbol.GetAbsolouteName(Declaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}";

        public string GetCName(IRProgram irProgram) => GetSource(Declaration.CReferenceSource, irProgram);

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent)
        {
            if (Declaration.Destructor != null)
                emitter.Append(GetSource(Declaration.Destructor, irProgram, valueCSource, childAgent));
        }

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (Declaration.Copier != null)
                emitter.Append(GetSource(Declaration.Copier, irProgram, valueCSource, responsibleDestroyer));
            else
                emitter.Append(valueCSource);
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (Declaration.Mover == null)
            {
                if (Declaration.Destructor == null)
                    emitter.Append($"{destC} = {valueCSource}");
                else
                    IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
            }
            else
                emitter.Append(GetSource(Declaration.Mover, irProgram, valueCSource).Replace("##AGENT", childAgent));
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedForeignCType(this))
            {
                foreach (var property in properties.Value)
                    property.Type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (Declaration.CStructDeclaration != null)
                emitter.AppendLine(GetSource(Declaration.CStructDeclaration, irProgram));
        }
    }
}
