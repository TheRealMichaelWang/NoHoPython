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
        public sealed class InterfaceProperty
        {
            public string Name { get; private set; }
            public IType Type { get; private set; }

            public InterfaceProperty(string name, IType type)
            {
                Name = name;
                Type = type;
            }

            public bool SatisfiesRequirement(RecordDeclaration.RecordProperty recordProperty)
            {
                return recordProperty.Name == Name && Type.IsCompatibleWith(recordProperty.Type);
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
