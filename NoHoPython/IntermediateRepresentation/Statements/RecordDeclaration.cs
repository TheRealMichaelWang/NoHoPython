using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class CannotMutateReadonlyPropertyException : Exception
    {
        public RecordDeclaration.RecordProperty Property { get; private set; }

        public CannotMutateReadonlyPropertyException(RecordDeclaration.RecordProperty property) : base($"Cannot mutate read-only property {property.Name}.")
        {
            Property = property;
        }
    }

    public sealed partial class RecordDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed partial class RecordProperty
        {
            public string Name { get; private set; }
            public IType Type { get; private set; }

            public bool IsReadonly { get; private set; }
            public IRValue? DefaultValue { get; private set; }

            public RecordProperty(string name, IType type, bool isReadonly, IRValue? defaultValue)
            {
                Name = name;
                Type = type;
                IsReadonly = isReadonly;
                DefaultValue = defaultValue;

                if (defaultValue != null && !Type.IsCompatibleWith(defaultValue.Type))
                    throw new UnexpectedTypeException(Type, defaultValue.Type);
            }

            public RecordProperty SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs), IsReadonly, DefaultValue == null ? null : DefaultValue.SubstituteWithTypearg(typeargs));
        }

        public bool IsGloballyNavigable => false;

        public string Name { get; private set; }
        public readonly List<TypeParameter> TypeParameters;

        private readonly List<RecordProperty> properties;
        private readonly List<InterfaceType> supportedInterfaces;

        public RecordDeclaration(string name, List<TypeParameter> typeParameters, List<RecordProperty> properties, List<InterfaceType> supportedInterfaces) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            TypeParameters = typeParameters;
            this.properties = properties;
            this.supportedInterfaces = supportedInterfaces;

            foreach (InterfaceType interfaceType in supportedInterfaces)
                interfaceType.ValidateSupportForRecord(this);
        }

        public List<InterfaceType> GetSupportedInterfaces(RecordType recordType)
        {
            if (recordType.RecordPrototype != this)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new(TypeParameters.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], recordType.TypeArguments[i]);

            List<InterfaceType> typeSupportedInterfaces = new(supportedInterfaces.Count);
            foreach (InterfaceType supportedInterface in supportedInterfaces)
                typeSupportedInterfaces.Add((InterfaceType)supportedInterface.SubstituteWithTypearg(typeargs));

            return typeSupportedInterfaces;
        }

        public List<RecordProperty> GetRecordProperties(RecordType recordType)
        {
            if (recordType.RecordPrototype != this)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new(TypeParameters.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], recordType.TypeArguments[i]);

            List<RecordProperty> typeProperties = new(properties.Count);
            foreach (RecordProperty recordProperty in properties)
                typeProperties.Add(recordProperty.SubstituteWithTypearg(typeargs));

            return typeProperties;
        }
    }
}
