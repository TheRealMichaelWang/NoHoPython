using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Syntax;

namespace NoHoPython.Typing
{
    public sealed partial class ReferenceType : IType, IPropertyContainer
    {
        public sealed partial class ElementProperty : Property
        {
            public ReferenceType ReferenceType { get; private set; }
            public override bool IsReadOnly => false;

            public ElementProperty(ReferenceType referenceType) : base("elem", referenceType.ElementType)
            {
                ReferenceType = referenceType;
            }
        }

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

        public bool HasProperty(string property) => property == "elem";

        public Property FindProperty(string property)
        {
            if (property != "elem")
                throw new InvalidOperationException();
            return new ElementProperty(this);
        }

        public List<Property> GetProperties() => new List<Property>() { new ElementProperty(this) };
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    //public sealed partial class SetReferenceElement : IRStatement
    //{
    //    public IAstElement ErrorReportedElement { get; private set; }

    //    public IRValue ReferenceValue { get; private set; }
    //    public IRValue NewElementValue { get; private set; }

    //    public SetReferenceElement(IRValue referenceValue, IRValue newElementValue, IAstElement errorReportedElement)
    //    {
    //        ReferenceValue = referenceValue;
    //        NewElementValue = newElementValue;
    //        ErrorReportedElement = errorReportedElement;
    //    }
    //}
}