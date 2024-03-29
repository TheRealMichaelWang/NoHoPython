﻿using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class CannotMutateReadonlyProperty : IRGenerationError
    {
        public RecordDeclaration.RecordProperty Property { get; private set; }

        public CannotMutateReadonlyProperty(RecordDeclaration.RecordProperty property, Syntax.IAstElement astElement) : base(astElement, $"Cannot mutate read-only property {property.Name}.")
        {
            Property = property;
            Debug.Assert(Property.IsReadOnly);
        }
    }

    public sealed class CannotMutateReadonlyValue : IRGenerationError
    {
        public IRValue Value { get; private set; }

        public CannotMutateReadonlyValue(IRValue value, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Cannot mutate read-only value {value.ErrorReportedElement}.")
        {
            Debug.Assert(value.IsReadOnly);
            Value = value;
        }

        public override void Print()
        {
            Console.WriteLine("Please note the following reasons for this error besides directly mutating a read only value:");
            Console.WriteLine("1. Passing a read-only value with mutable children into a function where the parameter is read only. ");
            Console.WriteLine("2. Moving a reference-type marked as read only with mutable children.");
            Console.WriteLine("3. Setting a read only value (ie variable/property) with a reference type with mutable children.");
            base.Print();
        }
    }

    public sealed class CannotImplementRecordCopier : IRGenerationError
    {
        public CannotImplementRecordCopier(Syntax.IAstElement errorReportedElement, string recordName) : base(errorReportedElement, $"Cannot implement user-defined record copier for {recordName}, because records that are passed by reference are never copied.")
        {

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

        public static bool HasProperty(IType type, string name, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (type is IPropertyContainer propertyContainer && propertyContainer.HasProperty(name))
                return true;

            IScopeSymbol? getter = irBuilder.SymbolMarshaller.FindSymbol($"{type.Identifier}_get_{name}");
            if (getter == null)
                getter = irBuilder.SymbolMarshaller.FindSymbol($"{type.PrototypeIdentifier}_get_{name}");

            return getter is ProcedureDeclaration;
        }

        public bool HasProperty(string identifier);
        public Property FindProperty(string identifier);

        public List<Property> GetProperties();
    }

    public abstract partial class Property
    {
        public abstract bool IsReadOnly { get; }

        public string Name { get; private set; }
        public IType Type { get; private set; }

        public Property(string name, IType type)
        {
            Name = name;
            Type = type;
        }
    }

    public sealed partial class RecordDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public sealed partial class RecordProperty : Property, IComparable<RecordProperty>
        {
            public override bool IsReadOnly => _isReadOnly;
            private bool _isReadOnly;

            private RecordDeclaration RecordDeclaration;
            private RecordType RecordType;

            public RecordProperty(string name, IType type, bool isReadOnly, RecordType recordType, RecordDeclaration recordDeclaration) : base(name, type)
            {
                _isReadOnly = isReadOnly;
                RecordDeclaration = recordDeclaration;
                RecordType = recordType;
                DefaultValue = null;
            }

            public RecordProperty SubstituteWithTypeargs(Dictionary<TypeParameter, IType> typeargs) => new(Name, Type.SubstituteWithTypearg(typeargs), IsReadOnly, (RecordType)RecordType.SubstituteWithTypearg(typeargs), RecordDeclaration);

            public void DelayedLinkSetDefaultValue(IRValue defaultValue, Syntax.AstIRProgramBuilder irBuilder)
            {
                Debug.Assert(!HasDefaultValue);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                Debug.Assert(RecordDeclaration.properties.Contains(this));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                RecordDeclaration.defaultValues[Name] = ArithmeticCast.CastTo(defaultValue, Type, irBuilder);
            }

            public int CompareTo(RecordProperty? recordProperty) => Name.CompareTo(recordProperty?.Name);
        }

        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }

        public override bool IsGloballyNavigable => false;

        public RecordType GetSelfType(Syntax.AstIRProgramBuilder irBuilder) => new(this, TypeParameters.ConvertAll((TypeParameter parameter) => (IType)new TypeParameterReference(irBuilder.ScopedProcedures.Count > 0 ? irBuilder.ScopedProcedures.Peek().SanitizeTypeParameter(parameter) : parameter)), ErrorReportedElement);

        public string Name { get; private set; }
        public readonly List<TypeParameter> TypeParameters;

        private List<RecordProperty>? properties;
        private Dictionary<string, IRValue> defaultValues;
        private SortedSet<string> messageReceiverNames;

        public ProcedureDeclaration Constructor { get; private set; }
        public ProcedureDeclaration? Destructor { get; private set; }
        public ProcedureDeclaration? Copier { get; private set; }
        public List<IType> ConstructorParameterTypes { get; private set; }
        public bool PassByReference { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RecordDeclaration(string name, List<TypeParameter> typeParameters, bool passByReference, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement) : base()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Name = name;
            TypeParameters = typeParameters;
            PassByReference = passByReference; 
            ErrorReportedElement = errorReportedElement;
            ParentContainer = parentContainer;
            properties = null;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Constructor = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            Destructor = null;
            Copier = null;
            defaultValues = new();
            messageReceiverNames = new();
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
                typeProperties.Add(recordProperty.SubstituteWithTypeargs(typeargs));

            return typeProperties;
        }

        public void DelayedLinkSetProperties(List<RecordProperty> properties)
        {
            Debug.Assert(this.properties == null);
            this.properties = properties;
            IPropertyContainer.SanitizePropertyNames(properties.ConvertAll((prop) => (Property)prop), ErrorReportedElement);
        }

        public void DeclareMessageReceiver(string name) => messageReceiverNames.Add(name);

        public void DelayedLinkSetConstructor(ProcedureDeclaration constructor, ProcedureDeclaration? destructor, ProcedureDeclaration? copier)
        {
            Debug.Assert(Constructor == null);
            Debug.Assert(Destructor == null);
            Debug.Assert(Copier == null);
            Constructor = constructor;
            Destructor = destructor;
            Copier = copier;

            if (copier != null && PassByReference)
                throw new CannotImplementRecordCopier(copier.ErrorReportedElement, Name);
        }

        public void DelayedLinkSetConstructorParameterTypes(List<IType> constructorParameters)
        {
            Debug.Assert(ConstructorParameterTypes == null);
            ConstructorParameterTypes = constructorParameters;
        }

        public void ConstructorMutabilityAnalysis(ProcedureDeclaration constructor)
        {
#pragma warning disable CS8602 // Only called during IR generation, following linking
            SortedSet<RecordProperty> initializedProperties = new(properties.FindAll((property) => property.HasDefaultValue));
            constructor.ConstructorMutabilityAnalysis(initializedProperties, this);
            foreach (RecordProperty property in properties)
                if (!initializedProperties.Contains(property))
                    throw new PropertyNotInitialized(property, ErrorReportedElement);
#pragma warning restore CS8602
        }

#pragma warning disable CS8602 // Only called during IR generation, following linking
        public bool AllPropertiesInitialized(SortedSet<RecordProperty> initializedProperties) => properties.TrueForAll(property => initializedProperties.Contains(property));
#pragma warning restore CS8602

        public AnonymizeProcedure? GetMessageReceiver(string name)
        {
            if (!messageReceiverNames.Contains(name))
                return null;

            IRValue value = defaultValues[name];
            while (value is AutoCast autoCast)
                value = autoCast.Input;
            return (AnonymizeProcedure)value;
        }
    }
}

namespace NoHoPython.Typing
{
    public sealed partial class RecordType : IType, IPropertyContainer
    {
        public bool IsNativeCType => false;
        public string TypeName => $"{RecordPrototype.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => IType.GetIdentifier(IScopeSymbol.GetAbsolouteName(RecordPrototype), TypeArguments.ToArray());
        public string PrototypeIdentifier => IType.GetPrototypeIdentifier(IScopeSymbol.GetAbsolouteName(RecordPrototype), RecordPrototype.TypeParameters);

        public bool IsEmpty => false;
        public bool HasMutableChildren => GetProperties().Any(property => !property.IsReadOnly);
        public bool IsReferenceType => RecordPrototype.PassByReference;

        public RecordDeclaration RecordPrototype;
        public readonly List<IType> TypeArguments;

        private Lazy<List<RecordDeclaration.RecordProperty>> properties;
        private Lazy<Dictionary<string, RecordDeclaration.RecordProperty>> identifierPropertyMap;
        private Lazy<Dictionary<TypeParameter, IType>> typeargMap;
        private Lazy<List<IType>> constructorParameterTypes;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public RecordType(RecordDeclaration recordPrototype, List<IType> typeArguments, Syntax.IAstElement errorReportedElement) : this(recordPrototype, TypeParameter.ValidateTypeArguments(recordPrototype.TypeParameters, typeArguments, errorReportedElement))
        {

        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private RecordType(RecordDeclaration recordPrototype, List<IType> typeArguments)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            RecordPrototype = recordPrototype;
            TypeArguments = typeArguments;

            properties = new(() => recordPrototype.GetRecordProperties(this));

            identifierPropertyMap = new(() =>
            {
                var toret = new Dictionary<string, RecordDeclaration.RecordProperty>(properties.Value.Count);
                foreach (RecordDeclaration.RecordProperty property in properties.Value)
                    toret.Add(property.Name, property);
                return toret;
            });

            typeargMap = TypeParameter.GetTypeargMap(RecordPrototype.TypeParameters, TypeArguments);

            constructorParameterTypes = new(() => recordPrototype.ConstructorParameterTypes.Select((parameter) => parameter.SubstituteWithTypearg(typeargMap.Value)).ToList());
        }

        public bool HasProperty(string identifier) => identifierPropertyMap.Value.ContainsKey(identifier);

        public Property FindProperty(string identifier) => identifierPropertyMap.Value[identifier];

        public List<Property> GetProperties() => properties.Value.ConvertAll((RecordDeclaration.RecordProperty property) => (Property)property);

        public List<IType> GetConstructorParameterTypes() => constructorParameterTypes.Value;

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

            IRRecordDeclaration = new IntermediateRepresentation.Statements.RecordDeclaration(Identifier, typeParameters, PassByReference, irBuilder.SymbolMarshaller.CurrentModule, this);
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

            RecordType selfType = IRRecordDeclaration.GetSelfType(irBuilder);
            IRProperties = Properties.ConvertAll((RecordProperty property) => new IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty(property.Identifier, property.Type.ToIRType(irBuilder, this), property.IsReadOnly, selfType, IRRecordDeclaration));
            messageRecieverPropertyMap = new(MessageRecievers.Count);

            foreach (ProcedureDeclaration messageReciever in MessageRecievers)
            {
                messageReciever.ForwardDeclare(irBuilder);
                if (messageReciever.Name == "__init__")
                    IRRecordDeclaration.DelayedLinkSetConstructorParameterTypes(messageReciever.Parameters.ConvertAll((parameter) => parameter.Type.ToIRType(irBuilder, messageReciever)));
                else if (messageReciever.Name != "__copy__" && messageReciever.Name != "__del__")
                {
                    var recordProperty = messageReciever.GenerateProperty(irBuilder, selfType);
                    IRProperties.Add(recordProperty);
                    messageRecieverPropertyMap.Add(messageReciever, recordProperty);
                    IRRecordDeclaration.DeclareMessageReceiver(messageReciever.Name);
                }
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
                    IRProperties[i].DelayedLinkSetDefaultValue(propertyValue.GenerateIntermediateRepresentationForValue(irBuilder, IRProperties[i].Type, false), irBuilder);
            }

            IntermediateRepresentation.Statements.ProcedureDeclaration? Constructor = null;
            IntermediateRepresentation.Statements.ProcedureDeclaration? Destructor = null;
            IntermediateRepresentation.Statements.ProcedureDeclaration? Copier = null;
            MessageRecievers.ForEach((ProcedureDeclaration reciever) => {
                if (reciever.Name == "__init__")
                    Constructor = (IntermediateRepresentation.Statements.ProcedureDeclaration)reciever.GenerateIntermediateRepresentationForStatement(irBuilder);
                else if (reciever.Name == "__del__")
                    Destructor = (IntermediateRepresentation.Statements.ProcedureDeclaration)reciever.GenerateIntermediateRepresentationForStatement(irBuilder);
                else if (reciever.Name == "__copy__")
                    Copier = (IntermediateRepresentation.Statements.ProcedureDeclaration)reciever.GenerateIntermediateRepresentationForStatement(irBuilder);
                else
                    messageRecieverPropertyMap[reciever].DelayedLinkSetDefaultValue(new AnonymizeProcedure((IntermediateRepresentation.Statements.ProcedureDeclaration)reciever.GenerateIntermediateRepresentationForStatement(irBuilder), false, reciever, null), irBuilder);
            });

            if (Constructor == null)
                throw new RecordMustDefineConstructorError(IRRecordDeclaration);
            else if (Constructor.ReturnType is not NothingType)
#pragma warning disable CS8604 // Return type already linked
                throw new UnexpectedTypeException(Constructor.ReturnType, Primitive.Nothing, Constructor.ErrorReportedElement);
#pragma warning restore CS8604
            else if (Constructor.Purity != Purity.Pure)
                throw new RecordConstructorMustBePure(IRRecordDeclaration, Constructor.ErrorReportedElement);
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
            IRRecordDeclaration.DelayedLinkSetConstructor(Constructor, Destructor, Copier);

            RecordType selfType = IRRecordDeclaration.GetSelfType(irBuilder);
            if(Copier != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (!Copier.ReturnType.IsCompatibleWith(selfType))
                    throw new UnexpectedTypeException(Copier.ReturnType, selfType, Copier.ErrorReportedElement);
                else if(Copier.Parameters.Count != 0)
                    throw new UnexpectedTypeArgumentsException(0, Copier.Parameters.Count, Copier.ErrorReportedElement);
                else if (Copier.Purity != Purity.Pure)
                    throw new RecordConstructorMustBePure(IRRecordDeclaration, Copier.ErrorReportedElement, false);
#pragma warning restore CS8602
            }

            IRRecordDeclaration.ConstructorMutabilityAnalysis(Constructor);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopeBackFromRecord();

            return IRRecordDeclaration;
        }
    }
}