using NoHoPython.IntermediateRepresentation.Statements;

namespace NoHoPython.Typing
{
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class EnumType : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => EnumDeclaration.Name; }

        public EnumDeclaration EnumDeclaration { get; private set; }
        public readonly List<IType> TypeArguments;

        public readonly List<IType> Options;

        public EnumType(EnumDeclaration enumDeclaration, List<IType> typeArguments)
        {
            EnumDeclaration = enumDeclaration;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(enumDeclaration.TypeParameters, typeArguments);

            Options = enumDeclaration.GetOptions(this);
        }

        public bool SupportsType(IType type)
        {
            foreach (IType option in Options)
                if (option.IsCompatibleWith(type))
                    return true;
            return false;
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is EnumType enumType)
                return Equals(enumType);
            if (type is TypeParameterReference typeParameterReference)
            {
                foreach (IType paramerterSupportedType in typeParameterReference.TypeParameter.RequiredSupportedTypes)
                    if (SupportsType(paramerterSupportedType))
                        return true;
            }
            return SupportsType(type);
        }

        public bool Equals(EnumType enumType)
        {
            if (EnumDeclaration != enumType.EnumDeclaration)
                return false;
            for (int i = 0; i < TypeArguments.Count; i++)
                if (!TypeArguments[i].Equals(enumType.TypeArguments[i]))
                    return false;
            return true;
        }

        public bool Equals(IType type)
        {
            if (type is EnumType enumType)
                return Equals(enumType);
            return false;
        }
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class RecordType : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => RecordPrototype.Name; }

        public RecordDeclaration RecordPrototype;
        public readonly List<IType> TypeArguments;

        public readonly List<RecordDeclaration.RecordProperty> Properties;
        public readonly List<InterfaceType> SupportedInterfaces;

        private Dictionary<string, RecordDeclaration.RecordProperty> identifierPropertyMap;

        public RecordType(RecordDeclaration recordPrototype, List<IType> typeArguments)
        {
            RecordPrototype = recordPrototype;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(recordPrototype.TypeParameters, typeArguments);

            Properties = recordPrototype.GetRecordProperties(this);
            SupportedInterfaces = recordPrototype.GetSupportedInterfaces(this);

            identifierPropertyMap = new Dictionary<string, RecordDeclaration.RecordProperty>(Properties.Count);
            foreach(RecordDeclaration.RecordProperty property in Properties)
                identifierPropertyMap.Add(property.Name, property);
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is RecordType recordType)
                return Equals(recordType);
            else if (type is TypeParameterReference typeParameterReference)
            {
                foreach (IType supportedType in typeParameterReference.TypeParameter.RequiredSupportedTypes)
                    if (this.IsCompatibleWith(supportedType))
                        return true;
            }
            return false;
        }

        public bool Equals(RecordType recordType)
        {
            if (this.RecordPrototype != recordType.RecordPrototype)
                return false;
            for (int i = 0; i < TypeArguments.Count; i++)
                if (!this.TypeArguments[i].Equals(recordType.TypeArguments[i]))
                    return false;
            return true;
        }

        public bool Equals(IType type)
        {
            if (type is RecordType recordType)
                return Equals(recordType);
            return false;
        }
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class InterfaceType : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => InterfaceDeclaration.Name; }

        public InterfaceDeclaration InterfaceDeclaration { get; private set; }
        public readonly List<IType> TypeArguments;
        public readonly List<InterfaceDeclaration.InterfaceProperty> RequiredImplementedProperties; 

        public InterfaceType(InterfaceDeclaration interfaceDeclaration, List<IType> typeArguments)
        {
            this.InterfaceDeclaration = interfaceDeclaration;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(interfaceDeclaration.TypeParameters, typeArguments);

            RequiredImplementedProperties = interfaceDeclaration.GetRequiredProperties(this);
        }

        public void ValidateSupportForRecord(List<RecordDeclaration.RecordProperty> recordProperties, RecordDeclaration recordDeclaration)
        {
            foreach(InterfaceDeclaration.InterfaceProperty interfaceProperty in RequiredImplementedProperties)
            {
                if (!interfaceProperty.SatisfiesRequirement(recordProperties))
                    throw new PropertyNotImplementedException(interfaceProperty, recordDeclaration);
            }
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is InterfaceType interfaceType)
                return Equals(interfaceType);
            else
            {
                if (type is TypeParameterReference typeParameterReference)
                {
                    foreach (InterfaceType @interface in typeParameterReference.TypeParameter.RequiredSupportedTypes)
                        if (this.IsCompatibleWith(@interface))
                            return true;
                }
                else if (type is RecordType recordType)
                {
                    foreach (InterfaceType supportedInterface in recordType.SupportedInterfaces)
                        if (this.IsCompatibleWith(supportedInterface))
                            return true;
                }
                return false;
            }
        }

        public bool Equals(InterfaceType interfaceType)
        {
            if (InterfaceDeclaration != interfaceType.InterfaceDeclaration)
                return false;

            for (int i = 0; i < TypeArguments.Count; i++)
                if (!TypeArguments[i].Equals(interfaceType.TypeArguments[i]))
                    return false;
            return true;
        }

        public bool Equals(IType type)
        {
            if (type is InterfaceType interfaceType)
                return Equals(interfaceType);
            return false;
        }
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class TypeParameterReference : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => TypeParameter.Name; }

        public TypeParameter TypeParameter { get; private set; }

        public TypeParameterReference(TypeParameter typeParameter)
        {
            TypeParameter = typeParameter;
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is TypeParameterReference typeParameterReference)
                return Equals(typeParameterReference);
            return TypeParameter.SupportsType(type);
        }

        public bool Equals(TypeParameterReference typeParameterReference) => TypeParameter == typeParameterReference.TypeParameter;

        public bool Equals(IType type)
        {
            if (type is TypeParameterReference typeParameterReference)
                return Equals(typeParameterReference);
            return false;
        }
    }
}
