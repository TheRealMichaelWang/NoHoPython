﻿using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class InterfaceDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed partial class InterfaceProperty : Property
        {
            public override bool IsReadOnly => true;

            public InterfaceProperty(string name, IType type) : base(name, type) { }

            public bool SatisfiesRequirement(Property property) => property.Name == Name && Type.IsCompatibleWith(property.Type);

            public bool SatisfiesRequirement(List<RecordDeclaration.RecordProperty> recordProperties) => recordProperties.Any((property) => SatisfiesRequirement(property));

            public InterfaceProperty SubstituteWithTypeargs(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs));
        }

        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }

        public override bool IsGloballyNavigable => false;
        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        private List<InterfaceProperty>? requiredImplementedProperties;

        public InterfaceDeclaration(string name, List<TypeParameter> typeParameters, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement) : base()
        {
            Name = name;
            TypeParameters = typeParameters;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }

        public List<InterfaceProperty> GetRequiredProperties(InterfaceType interfaceType)
        {
            if (interfaceType.InterfaceDeclaration != this)
                throw new InvalidOperationException();
            if (requiredImplementedProperties == null)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new(requiredImplementedProperties.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], interfaceType.TypeArguments[i]);

            List<InterfaceProperty> interfaceRequiredProperties = new(requiredImplementedProperties.Count);
            for (int i = 0; i < requiredImplementedProperties.Count; i++)
                interfaceRequiredProperties.Add(requiredImplementedProperties[i].SubstituteWithTypeargs(typeargs));
            return interfaceRequiredProperties;
        }

        public void DelayedLinkSetProperties(List<InterfaceProperty> requiredImplementedProperties)
        {
            if (this.requiredImplementedProperties != null)
                throw new InvalidOperationException();
            if (requiredImplementedProperties.Count < 1)
                throw new InterfaceMustRequireProperties(this, ErrorReportedElement);
            this.requiredImplementedProperties = requiredImplementedProperties;
            IPropertyContainer.SanitizePropertyNames(requiredImplementedProperties.ConvertAll((prop) => (Property)prop), ErrorReportedElement);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class MarshalIntoInterface : IRValue
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TargetType;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public InterfaceType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoInterface(InterfaceType targetType, IRValue value, Syntax.IAstElement errorReportedElement)
        {
            TargetType = targetType;
            Value = value;
            ErrorReportedElement = errorReportedElement;

            if (value.Type is IPropertyContainer propertyContainer)
            {
                if (!TargetType.SupportsProperties(propertyContainer.GetProperties()))
                    throw new UnexpectedTypeException(value.Type, errorReportedElement);
            }
            else
                throw new UnexpectedTypeException(value.Type, errorReportedElement);
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MarshalIntoInterface((InterfaceType)TargetType.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }
}

namespace NoHoPython.Typing
{
    public sealed partial class InterfaceType : IType, IPropertyContainer
    {
        public string TypeName => $"{InterfaceDeclaration.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => IType.GetIdentifier(IScopeSymbol.GetAbsolouteName(InterfaceDeclaration), TypeArguments.ToArray());
        public string PrototypeIdentifier => IType.GetPrototypeIdentifier(IScopeSymbol.GetAbsolouteName(InterfaceDeclaration), InterfaceDeclaration.TypeParameters);
        public bool IsEmpty => false;
        public bool HasMutableChildren => false;
        public bool IsReferenceType => GetProperties().Any(property => property.Type.IsReferenceType);

        public InterfaceDeclaration InterfaceDeclaration { get; private set; }
        public readonly List<IType> TypeArguments;

        private Lazy<List<InterfaceDeclaration.InterfaceProperty>> requiredImplementedProperties;
        private Lazy<Dictionary<string, InterfaceDeclaration.InterfaceProperty>> identifierPropertyMap;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public InterfaceType(InterfaceDeclaration interfaceDeclaration, List<IType> typeArguments, Syntax.IAstElement errorReportedElement) : this(interfaceDeclaration, TypeParameter.ValidateTypeArguments(interfaceDeclaration.TypeParameters, typeArguments, errorReportedElement))
        {

        }

        private InterfaceType(InterfaceDeclaration interfaceDeclaration, List<IType> typeArguments)
        {
            InterfaceDeclaration = interfaceDeclaration;
            TypeArguments = typeArguments;
            
            requiredImplementedProperties = new Lazy<List<InterfaceDeclaration.InterfaceProperty>>(() => interfaceDeclaration.GetRequiredProperties(this));

            identifierPropertyMap = new Lazy<Dictionary<string, InterfaceDeclaration.InterfaceProperty>>(() =>
            {
                var toret = new Dictionary<string, InterfaceDeclaration.InterfaceProperty>(requiredImplementedProperties.Value.Count);
                foreach (InterfaceDeclaration.InterfaceProperty property in requiredImplementedProperties.Value)
                    toret.Add(property.Name, property);
                return toret;
            });
        }

        public bool HasProperty(string identifier) => identifierPropertyMap.Value.ContainsKey(identifier);

        public Property FindProperty(string identifier) => identifierPropertyMap.Value[identifier];

        public List<Property> GetProperties() => requiredImplementedProperties.Value.ConvertAll((InterfaceDeclaration.InterfaceProperty property) => (Property)property);

        public bool IsCompatibleWith(IType type)
        {
            if (type is InterfaceType interfaceType)
            {
                if (InterfaceDeclaration != interfaceType.InterfaceDeclaration)
                    return false;

                for (int i = 0; i < TypeArguments.Count; i++)
                    if (!TypeArguments[i].IsCompatibleWith(interfaceType.TypeArguments[i]))
                        return false;
                return true;
            }
            return false;
        }

        public bool SupportsProperties(List<Property> properties)
        {
            bool SupportsProperty(InterfaceDeclaration.InterfaceProperty requiredProperty)
            {
                foreach (Property property in properties)
                    if (requiredProperty.SatisfiesRequirement(property))
                        return true;
                return false;
            }

            foreach (InterfaceDeclaration.InterfaceProperty requiredProperty in requiredImplementedProperties.Value)
            {
                if (!SupportsProperty(requiredProperty))
                    return false;
            }
            return true;
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class InterfaceDeclaration
    {
        private IntermediateRepresentation.Statements.InterfaceDeclaration IRInterfaceDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            IRInterfaceDeclaration = new IntermediateRepresentation.Statements.InterfaceDeclaration(Identifier, typeParameters, irBuilder.SymbolMarshaller.CurrentModule, this);
            irBuilder.SymbolMarshaller.DeclareSymbol(IRInterfaceDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRInterfaceDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            irBuilder.SymbolMarshaller.GoBack();

            irBuilder.AddInterfaceDeclaration(IRInterfaceDeclaration);
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRInterfaceDeclaration);
            IRInterfaceDeclaration.DelayedLinkSetProperties(Properties.ConvertAll((property) => new IntermediateRepresentation.Statements.InterfaceDeclaration.InterfaceProperty(property.Item2, property.Item1.ToIRType(irBuilder, this))));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IRInterfaceDeclaration;
    }
}