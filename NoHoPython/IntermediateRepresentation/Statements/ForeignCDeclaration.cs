using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class ForeignCDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed partial class ForeignCProperty : Property
        {
            public ForeignCProperty(string name, IType type) : base(name, type)
            {

            }

            public ForeignCProperty SubstituteWithTypeargs(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs));
        }

        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }
        public override bool IsGloballyNavigable => false;

        public string Name { get; private set; }
        
        public readonly List<TypeParameter> TypeParameters;
        public bool PointerPropertyAccess { get; private set; }

        public string? ForwardDeclaration { get; private set; }
        public string? CStructDeclaration { get; private set; }
        public string? MarshallerHeaders { get; private set; }
        public string? MarshallerDeclarations { get; private set; }
        public string CReferenceSource { get; private set; }
        public string? Copier { get; private set; }
        public string? Mover { get; private set; }
        public string? Destructor { get; private set; }
        public string? ResponsibleDestroyerSetter { get; private set; }

        public string? FormatSpecifier { get; private set; }
        public string? Formatter { get; private set; }

        public List<ForeignCProperty>? Properties;

        public ForeignCDeclaration(string name, List<TypeParameter> typeParameters, bool pointerPropertyAccess, List<ForeignCProperty>? properties, string? forwardDeclaration, string? cStructDeclaration, string? marshallerHeaders, string? marshallerDeclarations, string cReferenceSource, string? copier, string? mover, string? destructor, string? responsibleDestroyerSetter, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement)
        {
            Name = name;
            TypeParameters = typeParameters;
            PointerPropertyAccess = pointerPropertyAccess;
            Properties = properties;

            ForwardDeclaration = forwardDeclaration;
            CStructDeclaration = cStructDeclaration;
            MarshallerHeaders = marshallerHeaders;
            MarshallerDeclarations = marshallerDeclarations;
            CReferenceSource = cReferenceSource;
            Copier = copier;
            Mover = mover;
            Destructor = destructor;
            ResponsibleDestroyerSetter = responsibleDestroyerSetter;

            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }
    }
}


namespace NoHoPython.Typing
{
    public sealed partial class ForeignCType : IType, IPropertyContainer
    {
        public string TypeName => $"{Declaration.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => $"{IScopeSymbol.GetAbsolouteName(Declaration)}{(TypeArguments.Count == 0 ? string.Empty : $"_with_{string.Join("_", TypeArguments.ConvertAll((arg) => arg.TypeName))}")}";
        public bool IsEmpty => false;

        public ForeignCDeclaration Declaration { get; private set; }
        public readonly List<IType> TypeArguments;

        private Lazy<List<ForeignCDeclaration.ForeignCProperty>> properties;
        private Lazy<Dictionary<string, ForeignCDeclaration.ForeignCProperty>> identifierPropertyMap;
        private Lazy<Dictionary<TypeParameter, IType>> typeargMap;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public ForeignCType(ForeignCDeclaration declaration, List<IType> typeArguments, Syntax.IAstElement errorReportedElement) : this(declaration, TypeParameter.ValidateTypeArguments(declaration.TypeParameters, typeArguments, errorReportedElement))
        {

        }

        private ForeignCType(ForeignCDeclaration declaration, List<IType> typeArguments)
        {
            Declaration = declaration;
            TypeArguments = typeArguments;

            typeargMap = TypeParameter.GetTypeargMap(declaration.TypeParameters, typeArguments);

#pragma warning disable CS8604 // Possible null reference argument.
            properties = new Lazy<List<ForeignCDeclaration.ForeignCProperty>>(() => declaration.Properties.Select((property) => property.SubstituteWithTypeargs(typeargMap.Value)).ToList());
#pragma warning restore CS8604 // Possible null reference argument.

            identifierPropertyMap = new Lazy<Dictionary<string, ForeignCDeclaration.ForeignCProperty>>(() =>
            {
                Dictionary<string, ForeignCDeclaration.ForeignCProperty> propertyMap = new(properties.Value.Count);
                foreach (ForeignCDeclaration.ForeignCProperty property in properties.Value)
                    propertyMap.Add(property.Name, property);
                return propertyMap;
            });
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is ForeignCType foreignType)
            {
                if (Declaration != foreignType.Declaration)
                    return false;

                for (int i = 0; i < TypeArguments.Count; i++)
                    if (!TypeArguments[i].IsCompatibleWith(foreignType.TypeArguments[i]))
                        return false;
                return true;
            }
            return false;
        }

        public bool HasProperty(string identifier) => identifierPropertyMap.Value.ContainsKey(identifier);

        public Property FindProperty(string identifier) => identifierPropertyMap.Value[identifier];

        public List<Property> GetProperties() => properties.Value.ConvertAll((ForeignCDeclaration.ForeignCProperty property) => (Property)property);
    }
}