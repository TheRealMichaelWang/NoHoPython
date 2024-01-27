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
    public sealed class CannotCopyForeignResourceType : CodegenError
    {
        public CannotCopyForeignResourceType(IRElement? errorReportedElement, ForeignCType foreignCType) : base(errorReportedElement, $"Cannot copy foreign type {foreignCType.TypeName} because it is a resource.")
        {

        }
    }

    partial class IRProgram
    {
        public readonly Dictionary<ForeignCDeclaration, List<ForeignCType>> ForeignTypeOverloads;

        public void ForwardDeclareForeignTypes(Emitter emitter)
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
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class ForeignCDeclaration
    {
        partial class ForeignCProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
            {
                ForeignCType foreignCType = (ForeignCType)propertyContainer;

                if (AccessSource == null)
                {
                    value(emitter);
                    emitter.Append(foreignCType.Declaration.PointerPropertyAccess ? "->" : ".");
                    emitter.Append(Name);
                }
                else
                    emitter.Append(foreignCType.GetSource(AccessSource, irProgram, value, responsibleDestroyer));
                
                return false;
            }
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.ForeignTypeOverloads.ContainsKey(this))
                return;
            if (CStructDeclaration == null)
                return;

            foreach (ForeignCType foreignCType in irProgram.ForeignTypeOverloads[this])
                emitter.AppendLine(foreignCType.GetSource(CStructDeclaration, irProgram));
        }

        public void ForwardDeclare(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.ForeignTypeOverloads.ContainsKey(this))
                return;
            if (MarshallerHeaders == null)
                return;

            foreach (ForeignCType foreignCType in irProgram.ForeignTypeOverloads[this])
                emitter.AppendLine(foreignCType.GetSource(MarshallerHeaders, irProgram));
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (!irProgram.ForeignTypeOverloads.ContainsKey(this))
                return;
            if (MarshallerDeclarations == null)
                return;

            foreach (ForeignCType foreignCType in irProgram.ForeignTypeOverloads[this])
                primaryEmitter.AppendLine(foreignCType.GetSource(MarshallerDeclarations, irProgram));
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
        public bool IsCapturedByReference => Declaration.IsReferenceType;
        public bool IsThreadSafe => Declaration.IsThreadSafe;
        public bool HasCopier => !Declaration.IsResource;

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

        public string? GetInvalidState(IRProgram irProgram) => Declaration.InvalidState;
        public Emitter.SetPromise? IsInvalid(Emitter emitter) => null;

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            if (Declaration.Destructor != null)
                emitter.Append(GetSource(Declaration.Destructor, irProgram, valuePromise, childAgent));
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
        {
            if (!HasCopier)
                throw new CannotCopyForeignResourceType(errorReportedElement, this);

            if (Declaration.Copier != null)
                emitter.Append(GetSource(Declaration.Copier, irProgram, valueCSource, responsibleDestroyer));
            else
                valueCSource(emitter);
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer, null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord, null);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedForeignCType(this))
            {
                foreach (var property in properties.Value)
                    property.Type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
        {
            if (Declaration.CStructDeclaration != null)
                emitter.AppendLine(GetSource(Declaration.CStructDeclaration, irProgram));
        }
    }
}
