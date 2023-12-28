using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;

namespace NoHoPython.Typing
{
    public sealed partial class ReferenceType : IType
    {
        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public string TypeName => $"ref<{ElementType.TypeName}>";
        public string Identifier => $"ref_{ElementType.Identifier}";
        public string PrototypeIdentifier => $"ref_T";
        public bool IsEmpty => false;

        public IType ElementType { get; private set; }
        public bool IsReleased { get; private set; }

        public ReferenceType(IType elementType, bool isReleased)
        {
            ElementType = elementType;
            IsReleased = isReleased;
        }

        public bool IsCompatibleWith(IType type) => type is ReferenceType referenceType && referenceType.ElementType.IsCompatibleWith(ElementType) && !IsReleased;
    }
}
