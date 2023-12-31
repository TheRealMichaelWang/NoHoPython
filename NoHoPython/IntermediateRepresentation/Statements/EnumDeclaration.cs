using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class EnumDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public static RefinementContext.RefinementEmitter GetRefinedEnumEmitter(EnumType enumType, IType type)
        {
            if (type.IsEmpty)
                return (IRProgram irProgram, Emitter emitter, Emitter.Promise value, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(enumType.GetCEnumOptionForType(irProgram, type));

            return (IRProgram irProgram, Emitter emitter, Emitter.Promise value, Dictionary<TypeParameter, IType> typeargs) =>
            {
                value(emitter);
                emitter.Append($".data.{type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_set");
            };
        }

        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }

        public override bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public EnumType GetSelfType(Syntax.AstIRProgramBuilder irBuilder) => new(this, TypeParameters.ConvertAll((TypeParameter parameter) => (IType)new TypeParameterReference(irBuilder.ScopedProcedures.Count > 0 ? irBuilder.ScopedProcedures.Peek().SanitizeTypeParameter(parameter) : parameter)), ErrorReportedElement);

        public readonly List<TypeParameter> TypeParameters;
        public readonly Dictionary<string, string?> Attributes;

        private List<IType>? options;

        public EnumDeclaration(string name, List<TypeParameter> typeParameters, Dictionary<string, string?> attributes, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement) : base()
        {
            Name = name;
            TypeParameters = typeParameters;
            Attributes = attributes;
            ErrorReportedElement = errorReportedElement;
            ParentContainer = parentContainer;
        }

        public Dictionary<IType, int> GetOptions(EnumType enumType)
        {
            if (enumType.EnumDeclaration != this)
                throw new InvalidOperationException();
            if (options == null)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new(TypeParameters.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], enumType.TypeArguments[i]);

            Dictionary<IType, int> typeOptions = new(options.Count, new ITypeComparer());
            for (int i = 0; i < options.Count; i++)
                typeOptions.Add(options[i].SubstituteWithTypearg(typeargs), i);

            return typeOptions;
        }

        public void DelayedLinkSetOptions(List<IType> options)
        {
            if (this.options != null)
                throw new InvalidOperationException();
            this.options = options;
            if (options.Count < 2)
                throw new InsufficientEnumOptions(ErrorReportedElement);
            Stack<IType> stack = new(this.options);
            while(stack.Count > 0)
            {
                IType option = stack.Pop();
                if (stack.Contains(option, new ITypeComparer()))
                    throw new UnexpectedTypeException(option, ErrorReportedElement);
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class MarshalIntoEnum : IRValue
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TargetType;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public EnumType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoEnum(EnumType targetType, IRValue value, Syntax.AstIRProgramBuilder irBuilder, Syntax.IAstElement errorReportedElement)
        {
            TargetType = targetType;
            Value = value;
            ErrorReportedElement = errorReportedElement;

            if (TargetType.SupportsType(value.Type))
                return;
            else if (value.Type is TypeParameterReference typeParameterReference)
            {
                if (typeParameterReference.TypeParameter.RequiredImplementedInterface != null)
                {
                    if (!targetType.SupportsType(typeParameterReference.TypeParameter.RequiredImplementedInterface))
                        throw new UnexpectedTypeException(value.Type, errorReportedElement);
                    Value = new MarshalIntoInterface(typeParameterReference.TypeParameter.RequiredImplementedInterface, Value, ErrorReportedElement);
                }
                else
                    throw new UnexpectedTypeException(value.Type, errorReportedElement);
            }
            else
            {
                foreach(IType options in TargetType.GetOptions())
                    try
                    {
                        Value = ArithmeticCast.CastTo(value, options, irBuilder);
                        return;
                    }
                    catch (UnexpectedTypeException)
                    {
                        continue;
                    }
                throw new UnexpectedTypeException(value.Type, errorReportedElement);
            }
        }

        private MarshalIntoEnum(EnumType targetType, IRValue value, Syntax.IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            TargetType = targetType;
            Value = value;
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MarshalIntoEnum((EnumType)TargetType.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    public sealed partial class UnwrapEnumValue : IRValue, IRStatement
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        
        public IRValue EnumValue { get; private set; }
        public IType Type { get; private set; }
        public EnumType? ErrorReturnEnum { get; private set; }

        public bool IsTruey => false;
        public bool IsFalsey => false;

        public UnwrapEnumValue(IRValue enumValue, IType type, Syntax.AstIRProgramBuilder irBuilder, Syntax.IAstElement errorReportedElement)
        {
            EnumValue = enumValue;
            ErrorReportedElement = errorReportedElement;
            if (EnumValue.Type is EnumType enumType)
            {
                if (!enumType.SupportsType(type) || type.IsEmpty)
                    throw new UnexpectedTypeException(type, errorReportedElement);

                Type = type;

#pragma warning disable CS8600 //Return type already linked by now
                IType returnType = irBuilder.ScopedProcedures.Peek().ReturnType;
                if (returnType is EnumType errorReturn)
                {
                    if (!errorReturn.SupportsType(Primitive.Nothing))
                    {
                        foreach (IType option in enumType.GetOptions())
                            if (!option.IsCompatibleWith(type) && !errorReturn.SupportsType(option))
                                throw new UnexpectedTypeException(errorReturn, ErrorReportedElement);
                    }
                    ErrorReturnEnum = errorReturn;
                }
#pragma warning restore CS8600
            }
            else
                throw new UnexpectedTypeException(EnumValue.Type, errorReportedElement);
        }

        private UnwrapEnumValue(IRValue enumValue, IType type, EnumType? errorReturnEnum, Syntax.IAstElement errorReportedElement)
        {
            EnumValue = enumValue;
            Type = type;
            ErrorReturnEnum = errorReturnEnum;
            ErrorReportedElement = errorReportedElement;
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new UnwrapEnumValue(EnumValue.SubstituteWithTypearg(typeargs), Type.SubstituteWithTypearg(typeargs), (EnumType?)ErrorReturnEnum?.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    public sealed partial class CheckEnumOption : IRValue
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }

        public IRValue EnumValue { get; private set; }
        public IType Option { get; private set; }

        public IType Type => Primitive.Boolean;

        public bool IsTruey => false;
        public bool IsFalsey => false;

        public CheckEnumOption(IRValue enumValue, IType option, Syntax.IAstElement errorReportedElement)
        {
            EnumValue = enumValue;
            ErrorReportedElement = errorReportedElement;
            if(EnumValue.Type is EnumType enumType)
            {
                if (!enumType.SupportsType(option))
                    throw new UnexpectedTypeException(option, errorReportedElement);
                Option = option;
            }
            else
                throw new UnexpectedTypeException(EnumValue.Type, errorReportedElement);
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new CheckEnumOption(EnumValue.SubstituteWithTypearg(typeargs), Option.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }
}

namespace NoHoPython.Typing
{
    public sealed partial class EmptyEnumOption : IType, IScopeSymbol
    {
        public SymbolContainer ParentContainer => EnumDeclaration;
        public EnumDeclaration EnumDeclaration { get; private set; }
        public Syntax.IAstElement ErrorReportedElement { get; private set; }

        public string Name { get; private set; }

        public string TypeName => Name;
        public string Identifier => IScopeSymbol.GetAbsolouteName(this);
        public string PrototypeIdentifier => Identifier;
        public bool IsEmpty => true;
        public bool HasMutableChildren => false;
        public bool IsReferenceType => false;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder) => new EmptyTypeLiteral(this, errorReportedElement);

        public EmptyEnumOption(string name, EnumDeclaration enumDeclaration, Syntax.IAstElement errorReportedElement)
        {
            Name = name;
            EnumDeclaration = enumDeclaration;
            ErrorReportedElement = errorReportedElement;
        }

        public bool IsCompatibleWith(IType type)
        {
            if(type is EmptyEnumOption emptyEnumOption)
            {
                return emptyEnumOption.EnumDeclaration == EnumDeclaration && emptyEnumOption.Name == Name;
            }
            return false;
        }
    }

    public sealed partial class EnumType : IType, IPropertyContainer
    {
        sealed partial class EnumProperty : Property
        {
            public override bool IsReadOnly => false;

            public EnumType EnumType { get; private set; }

            public EnumProperty(string name, IType type, EnumType enumType) : base(name, type)
            {
                EnumType = enumType;
            }

            public override bool Equals(object? obj) => obj is EnumProperty enumProperty ? enumProperty.Type.IsCompatibleWith(Type) && enumProperty.Name == Name : false;
            public override int GetHashCode() => Type.Identifier.GetHashCode() ^ Name.GetHashCode();
        }

        private static Dictionary<EnumType, Lazy<Dictionary<string, EnumProperty>>> globalSupportedProperties = new(new ITypeComparer());
        private static Dictionary<EnumType, Lazy<Dictionary<IType, int>>> globalSupportedOptions = new(new ITypeComparer());
        private Lazy<Dictionary<TypeParameter, IType>> typeargMap;

        public bool IsNativeCType => false;
        public string TypeName => $"{EnumDeclaration.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => IType.GetIdentifier(IScopeSymbol.GetAbsolouteName(EnumDeclaration), TypeArguments.ToArray());
        public string PrototypeIdentifier => IType.GetPrototypeIdentifier(IScopeSymbol.GetAbsolouteName(EnumDeclaration), EnumDeclaration.TypeParameters);
        public bool IsEmpty => false;
        public bool HasMutableChildren => GetOptions().Any(option => option.HasMutableChildren);
        public bool IsReferenceType => GetOptions().Any(option => option.IsReferenceType);

        public EnumDeclaration EnumDeclaration { get; private set; }
        public readonly List<IType> TypeArguments;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public EnumType(EnumDeclaration enumDeclaration, List<IType> typeArguments, Syntax.IAstElement errorReportedElement) : this(enumDeclaration, TypeParameter.ValidateTypeArguments(enumDeclaration.TypeParameters, typeArguments, errorReportedElement))
        {
            
        }

        private EnumType(EnumDeclaration enumDeclaration, List<IType> typeArguments)
        {
            EnumDeclaration = enumDeclaration;
            TypeArguments = typeArguments;

            typeargMap = TypeParameter.GetTypeargMap(enumDeclaration.TypeParameters, typeArguments);

            if (globalSupportedProperties.ContainsKey(this))
                return;

            if(!globalSupportedOptions.ContainsKey(this))
                globalSupportedOptions[this] = new(() => EnumDeclaration.GetOptions(this));

            globalSupportedProperties[this] = new(() =>
            {
                if (!globalSupportedOptions[this].Value.Keys.All((option) => option is IPropertyContainer))
                    return new();
                IPropertyContainer firstType = (IPropertyContainer)globalSupportedOptions[this].Value.Keys.First();

                List<Property> firstTypeProperties = firstType.GetProperties();
                Dictionary<string, EnumProperty> propertyIdMap = new(firstTypeProperties.Count);

                List<IPropertyContainer> propertyContainers = globalSupportedOptions[this].Value.Keys.ToList().GetRange(1, globalSupportedOptions[this].Value.Count - 1).ConvertAll((option) => (IPropertyContainer)option);
                foreach (Property property in firstTypeProperties)
                {
                    bool foundFlag = true;
                    foreach(IPropertyContainer optionContainer in propertyContainers)
                    {
                        if (!optionContainer.HasProperty(property.Name))
                        {
                            foundFlag = false;
                            break;
                        }
                        Property optionFoundProperty = optionContainer.FindProperty(property.Name);
                        if (!property.Type.IsCompatibleWith(optionFoundProperty.Type))
                        {
                            foundFlag = false;
                            break;
                        }
                    }
                    if (foundFlag)
                        propertyIdMap.Add(property.Name, new EnumProperty(property.Name, property.Type, this));
                };
                return propertyIdMap;
            });

        }

        public List<IType> GetOptions() => globalSupportedOptions[this].Value.Keys.ToList();

        public List<Property> GetProperties() => globalSupportedProperties[this].Value.Values.Select((property) => property as Property).ToList();

        public Property FindProperty(string identifier) => globalSupportedProperties[this].Value[identifier];

        public bool HasProperty(string identifier) => globalSupportedProperties[this].Value.ContainsKey(identifier);

        public bool SupportsType(IType type) => globalSupportedOptions[this].Value.Keys.Any((option) => option.IsCompatibleWith(type));

        public bool IsCompatibleWith(IType type)
        {
            if (type is EnumType enumType)
            {
                if (EnumDeclaration != enumType.EnumDeclaration)
                    return false;
                if (TypeArguments.Count != enumType.TypeArguments.Count)
                    return false;

                for (int i = 0; i < TypeArguments.Count; i++)
                    if (!TypeArguments[i].IsCompatibleWith(enumType.TypeArguments[i]))
                        return false;

                return true;
            }

            return false;
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class EnumDeclaration
    {
        private IntermediateRepresentation.Statements.EnumDeclaration IREnumDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            IREnumDeclaration = new IntermediateRepresentation.Statements.EnumDeclaration(Identifier, typeParameters, Attributes, irBuilder.SymbolMarshaller.CurrentModule, this);
            irBuilder.SymbolMarshaller.DeclareSymbol(IREnumDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IREnumDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            irBuilder.SymbolMarshaller.GoBack();

            irBuilder.AddEnumDeclaration(IREnumDeclaration);
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IREnumDeclaration);
            IREnumDeclaration.DelayedLinkSetOptions(Options.ConvertAll((AstType option) =>
            {
                try
                {
                    return option.ToIRType(irBuilder, this);
                }
                catch (SymbolNotFoundException)
                {
                    option.MatchTypeArgCount(0, this);
                    EmptyEnumOption enumOption = new(option.Identifier, IREnumDeclaration, this);
                    irBuilder.SymbolMarshaller.DeclareSymbol(enumOption, this);
                    return enumOption;
                }
            }));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            EnumType selfType = IREnumDeclaration.GetSelfType(irBuilder);
            foreach(IType requriedImplementedType in RequiredImplementedInterfaces.ConvertAll((astType) => astType.ToIRType(irBuilder, this)))
            {
                if (requriedImplementedType is InterfaceType requiredImplementedInterface)
                {
                    if (!requiredImplementedInterface.SupportsProperties(selfType.GetProperties()))
                        throw new UnsupportedInterfaceException(IREnumDeclaration, requiredImplementedInterface, this);
                }
                else
                    throw new UnexpectedTypeException(requriedImplementedType, this);
            }

            return IREnumDeclaration;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class CheckEnumOption
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.CheckEnumOption(Enum.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), Option.ToIRType(irBuilder, this), this);
    }
}