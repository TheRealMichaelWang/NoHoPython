using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;

namespace NoHoPython.Typing
{
    partial class TypeParameterReference
    {
        partial class TypeParameterProperty
        {
            private Property? UnderlyingTypeargumentProperty = null;

            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs)
            {
                IPropertyContainer typeargPropertyContainer = (IPropertyContainer)typeargs[TypeParameter];
                return typeargPropertyContainer.FindProperty(Name).RequiresDisposal(typeargs);
            }

            public override void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
            {
                IPropertyContainer typeargPropertyContainer = (IPropertyContainer)typeargs[TypeParameter];
                
                UnderlyingTypeargumentProperty = typeargPropertyContainer.FindProperty(Name);
                UnderlyingTypeargumentProperty.ScopeForUse(optimizedMessageRecieverCall, typeargs, irBuilder);
            }

            public override bool EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource, string responsibleDestroyer)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return UnderlyingTypeargumentProperty.EmitGet(irProgram, emitter, typeargs, propertyContainer, valueCSource, responsibleDestroyer);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        public bool IsNativeCType => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool RequiresDisposal => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool MustSetResponsibleDestroyer => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool IsTypeDependency => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => true;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public string GetCName(IRProgram irProgram) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public string GetStandardIdentifier(IRProgram irProgram) => $"type_param_{TypeParameter.Name}";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string recordCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) => throw new UnexpectedTypeParameterError(TypeParameter, null);
    }
}