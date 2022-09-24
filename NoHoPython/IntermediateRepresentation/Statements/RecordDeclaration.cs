using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;
using System.Text;

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

        public void EmitGetProperty(StringBuilder emitter, string valueCSource, Property property);
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
            public IRValue DefaultValue { get; private set; }

            private bool isReadOnly;

#pragma warning disable CS8618 // Not immediatley initialized because of linking
            private RecordProperty(string name, IType type, bool isReadOnly, IRValue? defaultValue) : base(name, type)
#pragma warning restore CS8618 //
            {
                this.isReadOnly = isReadOnly;
                if(defaultValue != null)
                    DefaultValue = ArithmeticCast.CastTo(defaultValue, Type);
            }

            public RecordProperty(string name, IType type, bool isReadOnly) : this(name, type, isReadOnly, null) { }

            public RecordProperty SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs), isReadOnly, DefaultValue == null ? null : DefaultValue.SubstituteWithTypearg(typeargs));

            public void DelayedLinkSetDefaultValue(IRValue defaultValue)
            {
                if (DefaultValue != null)
                    throw new InvalidOperationException();
                DefaultValue = ArithmeticCast.CastTo(defaultValue, Type);
            }
        }

        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer? ParentContainer { get; private set; }

        public bool IsGloballyNavigable => false;

        public RecordType SelfType => new(this, TypeParameters.ConvertAll((TypeParameter parameter) => (IType)new TypeParameterReference(parameter)), ErrorReportedElement);

        public string Name { get; private set; }
        public readonly List<TypeParameter> TypeParameters;

        private List<RecordProperty>? properties;
        private List<ProcedureDeclaration>? messageRecievers;

        public RecordDeclaration(string name, List<TypeParameter> typeParameters, SymbolContainer? parentContainer, Syntax.IAstElement errorReportedElement) : base()
        {
            Name = name;
            TypeParameters = typeParameters;
            ErrorReportedElement = errorReportedElement;
            ParentContainer = parentContainer;
            properties = null;
            messageRecievers = null;
        }

        public List<RecordProperty> GetRecordProperties(RecordType recordType)
        {
            if (recordType.RecordPrototype != this)
                throw new InvalidOperationException();
            if (properties == null)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new(TypeParameters.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], recordType.TypeArguments[i]);

            List<RecordProperty> typeProperties = new(properties.Count);
            foreach (RecordProperty recordProperty in properties)
                typeProperties.Add(recordProperty.SubstituteWithTypearg(typeargs));

            return typeProperties;
        }

        public void DelayedLinkSetProperties(List<RecordProperty> properties)
        {
            if (this.properties != null)
                throw new InvalidOperationException();
            this.properties = properties;
        }

        public void DelayedLinkSetMessageRecievers(List<ProcedureDeclaration> messageRecievers)
        {
            if (this.messageRecievers != null)
                throw new InvalidOperationException();
            this.messageRecievers = messageRecievers;
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

        private Lazy<List<RecordDeclaration.RecordProperty>> properties;
        private Lazy<Dictionary<string, RecordDeclaration.RecordProperty>> identifierPropertyMap;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public RecordType(RecordDeclaration recordPrototype, List<IType> typeArguments, Syntax.IAstElement errorReportedElement) : this(recordPrototype, TypeParameter.ValidateTypeArguments(recordPrototype.TypeParameters, typeArguments, errorReportedElement))
        {

        }

        private RecordType(RecordDeclaration recordPrototype, List<IType> typeArguments)
        {
            RecordPrototype = recordPrototype;
            TypeArguments = typeArguments;

            properties = new Lazy<List<RecordDeclaration.RecordProperty>>(() => recordPrototype.GetRecordProperties(this));

            identifierPropertyMap = new Lazy<Dictionary<string, RecordDeclaration.RecordProperty>>(() =>
            {
                var toret = new Dictionary<string, RecordDeclaration.RecordProperty>(properties.Value.Count);
                foreach (RecordDeclaration.RecordProperty property in properties.Value)
                    toret.Add(property.Name, property);
                return toret;
            });
        }

        public bool HasProperty(string identifier) => identifierPropertyMap.Value.ContainsKey(identifier);

        public Property FindProperty(string identifier) => identifierPropertyMap.Value[identifier];

        public List<Property> GetProperties() => properties.Value.ConvertAll((RecordDeclaration.RecordProperty property) => (Property)property);

        public bool IsCompatibleWith(IType type)
        {
            if (type is RecordType recordType)
            {
                if (RecordPrototype != recordType.RecordPrototype)
                    return false;
                for (int i = 0; i < TypeArguments.Count; i++)
                    if (!TypeArguments[i].IsCompatibleWith(recordType.TypeArguments[i]))
                        return false;
                return true;
            }
            return false;
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class RecordDeclaration
    {
        private IntermediateRepresentation.Statements.RecordDeclaration IRRecordDeclaration;
        private List<IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty> IRProperties;
        private Dictionary<ProcedureDeclaration, IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty> messageRecieverPropertyMap;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            IRRecordDeclaration = new IntermediateRepresentation.Statements.RecordDeclaration(Identifier, typeParameters, irBuilder.CurrentMasterScope, this);
            irBuilder.SymbolMarshaller.DeclareSymbol(IRRecordDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRRecordDeclaration);
            irBuilder.ScopeToRecord(IRRecordDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopeBackFromRecord();

            irBuilder.AddRecordDeclaration(IRRecordDeclaration);
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRRecordDeclaration);
            irBuilder.ScopeToRecord(IRRecordDeclaration);

            IRProperties = Properties.ConvertAll((RecordProperty property) => new IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty(property.Identifier, property.Type.ToIRType(irBuilder, this), property.IsReadOnly));
            messageRecieverPropertyMap = new(MessageRecievers.Count);

            foreach (ProcedureDeclaration messageReciever in MessageRecievers)
            {
                messageReciever.ForwardDeclare(irBuilder);
                var recordProperty = messageReciever.GenerateProperty();
                IRProperties.Add(recordProperty);
                messageRecieverPropertyMap.Add(messageReciever, recordProperty);
            }

            IRRecordDeclaration.DelayedLinkSetProperties(IRProperties);
            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopeBackFromRecord();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRRecordDeclaration);
            irBuilder.ScopeToRecord(IRRecordDeclaration);

            for (int i = 0; i < Properties.Count; i++)
            {
                var propertyValue = Properties[i].DefaultValue;
                if (propertyValue != null)
                    IRProperties[i].DelayedLinkSetDefaultValue(propertyValue.GenerateIntermediateRepresentationForValue(irBuilder, IRProperties[i].Type));
                else
                    IRProperties[i].DelayedLinkSetDefaultValue(IRProperties[i].Type.GetDefaultValue(this));
            }
            IRRecordDeclaration.DelayedLinkSetMessageRecievers(MessageRecievers.ConvertAll((ProcedureDeclaration reciever) => {
                var irProcedure = (IntermediateRepresentation.Statements.ProcedureDeclaration)reciever.GenerateIntermediateRepresentationForStatement(irBuilder);
                messageRecieverPropertyMap[reciever].DelayedLinkSetDefaultValue(new AnonymizeProcedure(irProcedure, this));
                return irProcedure;
            }));

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopeBackFromRecord();

            return IRRecordDeclaration;
        }
    }
}