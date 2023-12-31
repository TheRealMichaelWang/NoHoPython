using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;

namespace NoHoPython.Typing
{
    public sealed partial class ReferenceType : IType
    {
        public enum ReferenceMode
        {
            UnreleasedCanRelease = 0,
            UnreleasedCannotRelease = 1,
            Released = 2
        }

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public string TypeName => $"{(Mode == ReferenceMode.UnreleasedCanRelease ? "ref" : "noReleaseRef")}<{ElementType.TypeName}>";
        public string Identifier => $"ref_{ElementType.Identifier}";
        public string PrototypeIdentifier => $"ref_T";
        public bool IsEmpty => Mode >= ReferenceMode.Released;
        public bool HasMutableChildren => true;
        public bool IsReferenceType => true;

        public IType ElementType { get; private set; }
        public ReferenceMode Mode { get; private set; }

        public ReferenceType(IType elementType, ReferenceMode mode)
        {
            ElementType = elementType;
            Mode = mode;
        }

        public bool IsCompatibleWith(IType type) => type is ReferenceType referenceType && referenceType.ElementType.IsCompatibleWith(ElementType) && Mode == referenceType.Mode && Mode < ReferenceMode.Released;
    }
}
