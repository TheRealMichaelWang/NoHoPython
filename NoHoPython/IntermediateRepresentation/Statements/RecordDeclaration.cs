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
        private readonly List<InterfaceType> supportedInterfaces;

        public RecordDeclaration(string name, List<TypeParameter> typeParameters, List<RecordProperty> properties, List<InterfaceType> supportedInterfaces) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            TypeParameters = typeParameters;
            this.properties = properties;
            this.supportedInterfaces = supportedInterfaces;

            foreach (InterfaceType interfaceType in supportedInterfaces)
                interfaceType.ValidateSupportForRecord(properties, this);
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
