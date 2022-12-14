using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class CannotMutateReadonlyPropertyException : IRGenerationError
    {
        public Property Property { get; private set; }

        public CannotMutateReadonlyPropertyException(Property property, Syntax.IAstElement astElement) : base(astElement, $"Cannot mutate read-only property {property.Name}.")
        {
            Property = property;
            Debug.Assert(Property.IsReadOnly);
        }
    }

    public interface IPropertyContainer
    {
        public static void SanitizePropertyNames(List<Property> properties, Syntax.IAstElement errorReportedElement)
        {
            Stack<string> checkedProperties = new(properties.ConvertAll((prop) => prop.Name));
            while (checkedProperties.Count > 0)
            {
                string property = checkedProperties.Pop();
                if (checkedProperties.Contains(property))
                    throw new PropertyAlreadyDefinedError(property, errorReportedElement);
            }
        }

        public bool HasProperty(string identifier);
        public Property FindProperty(string identifier);

        public List<Property> GetProperties();

        public void EmitGetProperty(IRProgram irProgram, StringBuilder emitter, string valueCSource, Property property);
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
        public sealed partial class RecordProperty : Property, IComparable
        {
            public override bool IsReadOnly => isReadOnly;

            public IRValue? DefaultValue => defaultValue ?? parentProperty?.DefaultValue; 

            private RecordProperty? parentProperty;
            private IRValue? defaultValue = null;
            private bool isReadOnly;

            private RecordProperty(string name, IType type, bool isReadOnly, RecordProperty? parentProperty, IRValue? defaultValue) : base(name, type)
            {
                this.isReadOnly = isReadOnly;
                this.parentProperty = parentProperty;
                if(defaultValue != null)
                    this.defaultValue = ArithmeticCast.CastTo(defaultValue, Type);
            }

            public RecordProperty(string name, IType type, bool isReadOnly, RecordProperty? parentProperty = null) : this(name, type, isReadOnly, parentProperty, null) { }

            public RecordProperty SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs), isReadOnly, parentProperty ?? this, defaultValue?.SubstituteWithTypearg(typeargs));

            public void DelayedLinkSetDefaultValue(IRValue defaultValue)
            {
                if (this.defaultValue != null)
                    throw new InvalidOperationException();
                this.defaultValue = ArithmeticCast.CastTo(defaultValue, Type);
            }

            public int CompareTo(object? obj)
            {
                if (obj is Property property)
                    return Name.CompareTo(property.Name);
                throw new InvalidOperationException();
            }
        }

        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }

        public override bool IsGloballyNavigable => false;

        public RecordType GetSelfType(Syntax.AstIRProgramBuilder irBuilder) => new(this, TypeParameters.ConvertAll((TypeParameter parameter) => (IType)new TypeParameterReference(irBuilder.ScopedProcedures.Count > 0 ? irBuilder.ScopedProcedures.Peek().SanitizeTypeParameter(parameter) : parameter)), ErrorReportedElement);

        public string Name { get; private set; }
        public readonly List<TypeParameter> TypeParameters;

        private List<RecordProperty>? properties;
        private List<ProcedureDeclaration>? messageRecievers;

        public RecordDeclaration(string name, List<TypeParameter> typeParameters, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement) : base()
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
            IPropertyContainer.SanitizePropertyNames(properties.ConvertAll((prop) => (Property)prop), ErrorReportedElement);
        }

        public void DelayedLinkSetMessageRecievers(List<ProcedureDeclaration> messageRecievers)
        {
            if (this.messageRecievers != null)
                throw new InvalidOperationException();
            this.messageRecievers = messageRecievers;
        }

        public void AnalyzePropertyInitialization(ProcedureDeclaration constructor)
        {
#pragma warning disable CS8602 // Only called during IR generation, following linking
            SortedSet<RecordProperty> initializedProperties = new(properties.FindAll((property) => property.DefaultValue != null));
            constructor.CodeBlockAnalyzePropertyInitialization(initializedProperties, this);
            foreach (RecordProperty property in properties)
                if (!initializedProperties.Contains(property))
                    throw new PropertyNotInitialized(property, ErrorReportedElement);
#pragma warning restore CS8602
        }

#pragma warning disable CS8602 // Only called during IR generation, following linking
        public bool AllPropertiesInitialized(SortedSet<RecordProperty> initializedProperties) => properties.TrueForAll(property => initializedProperties.Contains(property));
#pragma warning restore CS8602
    }
}

namespace NoHoPython.Typing
{
    public sealed partial class RecordType : IType, IPropertyContainer
    {
        public bool IsNativeCType => false;
        public string TypeName => $"{RecordPrototype.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => $"{IScopeSymbol.GetAbsolouteName(RecordPrototype)}{(TypeArguments.Count == 0 ? string.Empty : $"_with_{string.Join("_", TypeArguments.ConvertAll((arg) => arg.TypeName))}")}";
        public bool IsEmpty => false;

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

            IRRecordDeclaration = new IntermediateRepresentation.Statements.RecordDeclaration(Identifier, typeParameters, irBuilder.SymbolMarshaller.CurrentModule, this);
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

            //link property definitions
            IRRecordDeclaration.DelayedLinkSetProperties(IRProperties);
            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopeBackFromRecord();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRRecordDeclaration);
            irBuilder.ScopeToRecord(IRRecordDeclaration);

            //link default values to existing properties
            for (int i = 0; i < Properties.Count; i++)
            {
                var propertyValue = Properties[i].DefaultValue;
                if (propertyValue != null)
                    IRProperties[i].DelayedLinkSetDefaultValue(propertyValue.GenerateIntermediateRepresentationForValue(irBuilder, IRProperties[i].Type, false));
            }

            IntermediateRepresentation.Statements.ProcedureDeclaration? Constructor = null;
            IntermediateRepresentation.Statements.ProcedureDeclaration? Destructor = null;
            IntermediateRepresentation.Statements.ProcedureDeclaration? Copier = null;
            IRRecordDeclaration.DelayedLinkSetMessageRecievers(MessageRecievers.ConvertAll((ProcedureDeclaration reciever) => {
                var irProcedure = (IntermediateRepresentation.Statements.ProcedureDeclaration)reciever.GenerateIntermediateRepresentationForStatement(irBuilder);
                messageRecieverPropertyMap[reciever].DelayedLinkSetDefaultValue(new AnonymizeProcedure(irProcedure, this, null));

                if (reciever.Name == "__init__")
                    Constructor = irProcedure;
                else if (reciever.Name == "__del__")
                    Destructor = irProcedure;
                else if (reciever.Name == "__copy__")
                    Copier = irProcedure;

                return irProcedure;
            }));

            if (Constructor == null)
                throw new RecordMustDefineConstructorError(IRRecordDeclaration);
            else if (Constructor.ReturnType is not NothingType)
#pragma warning disable CS8604 // Return type already linked
                throw new UnexpectedTypeException(Constructor.ReturnType, Primitive.Nothing, Constructor.ErrorReportedElement);
#pragma warning restore CS8604
            if (Destructor != null) 
            {
#pragma warning disable CS8604 // return type already linked
                if (Destructor.ReturnType is not NothingType)
                    throw new UnexpectedTypeException(Destructor.ReturnType, Primitive.Nothing, Destructor.ErrorReportedElement);
#pragma warning restore CS8604
#pragma warning disable CS8602 //parameters already linked
                else if (Destructor.Parameters.Count != 0)
                    throw new UnexpectedTypeArgumentsException(0, Destructor.Parameters.Count, Destructor.ErrorReportedElement);
#pragma warning restore CS8602
                else if (Copier == null)
                    throw new RecordMustDefineCopierError(IRRecordDeclaration);
            }

            IType selfType = IRRecordDeclaration.GetSelfType(irBuilder);
            if(Copier != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (!Copier.ReturnType.IsCompatibleWith(selfType))
                    throw new UnexpectedTypeException(Copier.ReturnType, selfType, Copier.ErrorReportedElement);
                else if(Copier.Parameters.Count != 0)
                    throw new UnexpectedTypeArgumentsException(0, Copier.Parameters.Count, Copier.ErrorReportedElement);
#pragma warning restore CS8602
            }

            IRRecordDeclaration.AnalyzePropertyInitialization(Constructor);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopeBackFromRecord();

            return IRRecordDeclaration;
        }
    }
}