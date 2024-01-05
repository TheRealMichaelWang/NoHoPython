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

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                return UnderlyingTypeargumentProperty.EmitGet(irProgram, emitter, typeargs, propertyContainer, value, responsibleDestroyer, errorReportedElement);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        public bool IsNativeCType => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool RequiresDisposal => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool MustSetResponsibleDestroyer => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool IsTypeDependency => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool HasMutableChildren => false;
        public bool IsReferenceType => false;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => true;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public string GetCName(IRProgram irProgram) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public string GetStandardIdentifier(IRProgram irProgram) => $"type_param_{TypeParameter.Name}";

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)=> throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCStruct(IRProgram irProgram, Emitter emitter) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) => throw new UnexpectedTypeParameterError(TypeParameter, null);
    }
}