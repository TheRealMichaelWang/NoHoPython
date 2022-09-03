using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class CannotMutateReadonlyPropertyException : Exception
    {
        public Property Property { get; private set; }

        public CannotMutateReadonlyPropertyException(Property property) : base($"Cannot mutate read-only property {property.Name}.")
        {
            Property = property;
            Debug.Assert(Property.IsReadOnly);
        }
    }

    public interface IPropertyContainer
    {
        public bool HasProperty(string identifier);
        public Property FindProperty(string identifier);

        public List<Property> GetProperties();
    }

    public abstract class Property
    {
        public abstract bool IsReadOnly { get; }

        public readonly string Name;
        public IType Type { get; private set; }

        public Property(string name, IType type)
        {
            Name = name;
            Type = type;
        }
    }

    public sealed partial class RecordDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed partial class RecordProperty : Property
        {
            public override bool IsReadOnly => isReadOnly; 
            public IRValue? DefaultValue { get; private set; }

            private bool isReadOnly;

            public RecordProperty(string name, IType type, bool isReadOnly, IRValue? defaultValue) : base(name, type)
            {
                this.isReadOnly = isReadOnly;
                DefaultValue = defaultValue;

                if (defaultValue != null)
                    DefaultValue = ArithmeticCast.CastTo(defaultValue, type);
            }

            public RecordProperty SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs), isReadOnly, DefaultValue == null ? null : DefaultValue.SubstituteWithTypearg(typeargs));
        }

        public bool IsGloballyNavigable => false;

        public string Name { get; private set; }
        public readonly List<TypeParameter> TypeParameters;

        private readonly List<RecordProperty> properties;

        public RecordDeclaration(string name, List<TypeParameter> typeParameters, List<RecordProperty> properties) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            TypeParameters = typeParameters;
            this.properties = properties;
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

namespace NoHoPython.Typing
{
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class RecordType : IType, IPropertyContainer
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => RecordPrototype.Name; }

        public RecordDeclaration RecordPrototype;
        public readonly List<IType> TypeArguments;

        public readonly List<RecordDeclaration.RecordProperty> Properties;

        private Dictionary<string, RecordDeclaration.RecordProperty> identifierPropertyMap;

        public RecordType(RecordDeclaration recordPrototype, List<IType> typeArguments)
        {
            RecordPrototype = recordPrototype;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(recordPrototype.TypeParameters, typeArguments);

            Properties = recordPrototype.GetRecordProperties(this);

            identifierPropertyMap = new Dictionary<string, RecordDeclaration.RecordProperty>(Properties.Count);
            foreach (RecordDeclaration.RecordProperty property in Properties)
                identifierPropertyMap.Add(property.Name, property);
        }

        public bool HasProperty(string identifier) => identifierPropertyMap.ContainsKey(identifier);

        public Property FindProperty(string identifier) => identifierPropertyMap[identifier];

        public List<Property> GetProperties() => Properties.ConvertAll((RecordDeclaration.RecordProperty property) => (Property)property);

        public bool IsCompatibleWith(IType type)
        {
            if (type is RecordType recordType)
            {
                if (this.RecordPrototype != recordType.RecordPrototype)
                    return false;
                for (int i = 0; i < TypeArguments.Count; i++)
                    if (!this.TypeArguments[i].IsCompatibleWith(recordType.TypeArguments[i]))
                        return false;
                return true;
            }
            return false;
        }
    }
}