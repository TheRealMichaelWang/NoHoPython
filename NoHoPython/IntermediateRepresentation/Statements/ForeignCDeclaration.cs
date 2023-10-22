using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class ForeignCDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed partial class ForeignCProperty : Property
        {
            public string? AccessSource { get; private set; }

            public ForeignCProperty(string name, IType type, string? accessSource) : base(name, type)
            {
                AccessSource = accessSource;
            }

            public ForeignCProperty SubstituteWithTypeargs(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs), AccessSource);
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
        public string? Destructor { get; private set; }
        public string? ResponsibleDestroyerSetter { get; private set; }

        public string? FormatSpecifier { get; private set; }
        public string? Formatter { get; private set; }

        public List<ForeignCProperty>? Properties = null;

        public ForeignCDeclaration(string name, List<TypeParameter> typeParameters, bool pointerPropertyAccess, string? forwardDeclaration, string? cStructDeclaration, string? marshallerHeaders, string? marshallerDeclarations, string cReferenceSource, string? copier, string? destructor, string? responsibleDestroyerSetter, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement)
        {
            Name = name;
            TypeParameters = typeParameters;
            PointerPropertyAccess = pointerPropertyAccess;

            ForwardDeclaration = forwardDeclaration;
            CStructDeclaration = cStructDeclaration;
            MarshallerHeaders = marshallerHeaders;
            MarshallerDeclarations = marshallerDeclarations;
            CReferenceSource = cReferenceSource;
            Copier = copier;
            Destructor = destructor;
            ResponsibleDestroyerSetter = responsibleDestroyerSetter;

            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }

        public void DelayedLinkSetProperties(List<ForeignCProperty> properties)
        {
            Debug.Assert(Properties == null);
            Properties = properties;
        }
    }
}

namespace NoHoPython.Typing
{
    public sealed partial class ForeignCType : IType, IPropertyContainer
    {
        public string TypeName => $"{Declaration.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => IType.GetIdentifier(IScopeSymbol.GetAbsolouteName(Declaration), TypeArguments.ToArray());
        public string PrototypeIdentifier => IType.GetPrototypeIdentifier(IScopeSymbol.GetAbsolouteName(Declaration), Declaration.TypeParameters);
        public bool IsEmpty => false;

        public ForeignCDeclaration Declaration { get; private set; }
        public readonly List<IType> TypeArguments;

        private Lazy<List<ForeignCDeclaration.ForeignCProperty>> properties;
        private Lazy<Dictionary<string, ForeignCDeclaration.ForeignCProperty>> identifierPropertyMap;
        private Lazy<Dictionary<TypeParameter, IType>> typeargMap;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

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

namespace NoHoPython.Syntax.Statements
{
    partial class ForeignCDeclaration
    {
        private IntermediateRepresentation.Statements.ForeignCDeclaration IRDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            string? GetOption(string attribute)
            {
                if (Attributes.ContainsKey(attribute))
                    return Attributes[attribute];
                return null;
            }

            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            IRDeclaration = new IntermediateRepresentation.Statements.ForeignCDeclaration(Identifier, typeParameters, Attributes.ContainsKey("ptr") || CSource.EndsWith('*'), GetOption("ForwardDeclaration"), GetOption("CStruct"), GetOption("MarshallerHeaders"), GetOption("Marshallers"), CSource, GetOption("Copy"), GetOption("Destroy"), GetOption("ActorSetter"), irBuilder.SymbolMarshaller.CurrentModule, this);
            irBuilder.SymbolMarshaller.DeclareSymbol(IRDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            irBuilder.SymbolMarshaller.GoBack();
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRDeclaration);
            IRDeclaration.DelayedLinkSetProperties(Properties.ConvertAll((property) => new IntermediateRepresentation.Statements.ForeignCDeclaration.ForeignCProperty(property.Item2, property.Item1.ToIRType(irBuilder, this), property.Item3)));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IRDeclaration;
    }
}