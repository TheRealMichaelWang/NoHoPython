using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class InterfaceDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed class InterfaceProperty : Property
        {
            public override bool IsReadOnly => true;

            public InterfaceProperty(string name, IType type) : base(name, type)
            {

            }

            public bool SatisfiesRequirement(Property property)
            {
                return property.Name == Name && Type.IsCompatibleWith(property.Type);
            }

            public bool SatisfiesRequirement(List<RecordDeclaration.RecordProperty> recordProperties)
            {
                foreach (RecordDeclaration.RecordProperty property in recordProperties)
                    if (SatisfiesRequirement(property))
                        return true;
                return false;
            }

            public InterfaceProperty SubstituteWithTypeargs(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs));
        }


        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        private List<InterfaceProperty>? requiredImplementedProperties;

        public InterfaceDeclaration(string name, List<TypeParameter> typeParameters) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            TypeParameters = typeParameters;
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
            for (int i = 0; i < interfaceRequiredProperties.Count; i++)
                interfaceRequiredProperties.Add(requiredImplementedProperties[i].SubstituteWithTypeargs(typeargs));
            return interfaceRequiredProperties;
        }

        public void DelayedLinkSetProperties(List<InterfaceProperty> requiredImplementedProperties)
        {
            if (this.requiredImplementedProperties != null)
                throw new InvalidOperationException();
            this.requiredImplementedProperties = requiredImplementedProperties;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class MarshalIntoInterface : IRValue
    {
        public bool IsConstant => false;
        public IType Type => TargetType;

        public InterfaceType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoInterface(InterfaceType targetType, IRValue value)
        {
            TargetType = targetType;
            Value = value;

            if (value.Type is TypeParameterReference typeParameterReference)
            {
                if (typeParameterReference.TypeParameter.RequiredImplementedInterface is not null && typeParameterReference.TypeParameter.RequiredImplementedInterface is IPropertyContainer requiredContainer)
                {
                    if (!TargetType.SupportsProperties(requiredContainer.GetProperties()))
                        throw new UnexpectedTypeException(typeParameterReference);
                }
                else
                    throw new UnexpectedTypeException(typeParameterReference);
            }
            if (value.Type is IPropertyContainer propertyContainer)
            {
                if (!TargetType.SupportsProperties(propertyContainer.GetProperties()))
                    throw new UnexpectedTypeException(value.Type);
            }
            else
                throw new UnexpectedTypeException(value.Type);
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => ArithmeticCast.CastTo(Value.SubstituteWithTypearg(typeargs), TargetType.SubstituteWithTypearg(typeargs));
    }
}

namespace NoHoPython.Typing
{
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class InterfaceType : IType, IPropertyContainer
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => InterfaceDeclaration.Name; }

        public InterfaceDeclaration InterfaceDeclaration { get; private set; }
        public readonly List<IType> TypeArguments;

        private Lazy<List<InterfaceDeclaration.InterfaceProperty>> requiredImplementedProperties;
        private Lazy<Dictionary<string, InterfaceDeclaration.InterfaceProperty>> identifierPropertyMap;

        public InterfaceType(InterfaceDeclaration interfaceDeclaration, List<IType> typeArguments)
        {
            InterfaceDeclaration = interfaceDeclaration;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(interfaceDeclaration.TypeParameters, typeArguments);
            
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

            IRInterfaceDeclaration = new IntermediateRepresentation.Statements.InterfaceDeclaration(Identifier, typeParameters);
            irBuilder.SymbolMarshaller.DeclareSymbol(IRInterfaceDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRInterfaceDeclaration);

            irBuilder.SymbolMarshaller.GoBack();

            irBuilder.AddInterfaceDeclaration(IRInterfaceDeclaration);
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            IRInterfaceDeclaration.DelayedLinkSetProperties(Properties.ConvertAll((InterfaceProperty property) => new IntermediateRepresentation.Statements.InterfaceDeclaration.InterfaceProperty(property.Identifier, property.Type.ToIRType(irBuilder, this))));
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IRInterfaceDeclaration;
    }
}