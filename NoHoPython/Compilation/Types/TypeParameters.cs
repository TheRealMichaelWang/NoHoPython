using NoHoPython.IntermediateRepresentation;
using System.Text;

namespace NoHoPython.Typing
{
    partial class TypeParameterReference
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public bool HasResponsibleDestroyer => false;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public string GetCName(IRProgram irProgram) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public string GetStandardIdentifier(IRProgram irProgram) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string childAgent) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) => throw new UnexpectedTypeParameterError(TypeParameter, null);
    }
}