using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class PropertyNotImplementedException : Exception
    {
        public InterfaceDeclaration.InterfaceProperty RequiredProperty { get; private set; }
        public RecordDeclaration Record { get; private set; }

        public PropertyNotImplementedException(InterfaceDeclaration.InterfaceProperty requiredProperty, RecordDeclaration record) : base($"{record.Name} doesn't implement required property {requiredProperty}.")
        {
            this.RequiredProperty = requiredProperty;
            this.Record = record;
        }
    }

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

            public InterfaceProperty SubstituteWithTypeargs(Dictionary<TypeParameter, IType> typeargs) => new (Name, Type.SubstituteWithTypearg(typeargs));
        }


        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        private readonly List<InterfaceProperty> requiredImplementedProperties;

        public InterfaceDeclaration(string name, List<TypeParameter> typeParameters, List<InterfaceProperty> requiredImplementedProperties) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            TypeParameters = typeParameters;
            this.requiredImplementedProperties = requiredImplementedProperties;
        }

        public List<InterfaceProperty> GetRequiredProperties(InterfaceType interfaceType)
        {
            if (interfaceType.InterfaceDeclaration != this)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new (requiredImplementedProperties.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], interfaceType.TypeArguments[i]);

            List<InterfaceProperty> interfaceRequiredProperties = new (requiredImplementedProperties.Count);
            for(int i = 0; i < interfaceRequiredProperties.Count; i++)
                interfaceRequiredProperties.Add(requiredImplementedProperties[i].SubstituteWithTypeargs(typeargs));
            return interfaceRequiredProperties;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class MarshalIntoInterface : IRValue
    {
        public IType Type => TargetType;

        public InterfaceType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoInterface(InterfaceType targetType, IRValue value)
        {
            TargetType = targetType;
            Value = value;

            if(value.Type is TypeParameterReference typeParameterReference)
            {
                if(typeParameterReference.TypeParameter.RequiredImplementedInterface is not null && typeParameterReference.TypeParameter.RequiredImplementedInterface is IPropertyContainer requiredContainer)
                {
                    if(!TargetType.SupportsProperties(requiredContainer.GetProperties()))
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
        public readonly List<InterfaceDeclaration.InterfaceProperty> RequiredImplementedProperties;

        private Dictionary<string, InterfaceDeclaration.InterfaceProperty> identifierPropertyMap;

        public InterfaceType(InterfaceDeclaration interfaceDeclaration, List<IType> typeArguments)
        {
            this.InterfaceDeclaration = interfaceDeclaration;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(interfaceDeclaration.TypeParameters, typeArguments);

            RequiredImplementedProperties = interfaceDeclaration.GetRequiredProperties(this);

            identifierPropertyMap = new(RequiredImplementedProperties.Count);
            foreach (InterfaceDeclaration.InterfaceProperty property in RequiredImplementedProperties)
                identifierPropertyMap.Add(property.Name, property);
        }

        public bool HasProperty(string identifier) => identifierPropertyMap.ContainsKey(identifier);

        public Property FindProperty(string identifier) => identifierPropertyMap[identifier];

        public List<Property> GetProperties() => RequiredImplementedProperties.ConvertAll((InterfaceDeclaration.InterfaceProperty property) => (Property)property);

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

            foreach(InterfaceDeclaration.InterfaceProperty requiredProperty in RequiredImplementedProperties)
            {
                if (!SupportsProperty(requiredProperty))
                    return false;
            }
            return true;
        }
    }
}