using NoHoPython.IntermediateRepresentation;
using System.Text;

namespace NoHoPython.Typing
{
    partial class TypeParameterReference
    {
        public bool RequiresDisposal => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public string GetCName() => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public string GetStandardIdentifier() => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => throw new UnexpectedTypeParameterError(TypeParameter, null);

        public void ScopeForUsedTypes() => throw new UnexpectedTypeParameterError(TypeParameter, null);
    }
}