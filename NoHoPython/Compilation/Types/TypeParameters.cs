using NoHoPython.IntermediateRepresentation;
using System.Text;

namespace NoHoPython.Typing
{
    partial class TypeParameterReference
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public string GetCName(IRProgram irProgram) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public string GetStandardIdentifier(IRProgram irProgram) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) => throw new UnexpectedTypeParameterError(TypeParameter, null);
    }
}