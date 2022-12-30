using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class EnumDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }

        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public EnumType GetSelfType(Syntax.AstIRProgramBuilder irBuilder) => new(this, TypeParameters.ConvertAll((TypeParameter parameter) => (IType)new TypeParameterReference(irBuilder.ScopedProcedures.Count > 0 ? irBuilder.ScopedProcedures.Peek().SanitizeTypeParameter(parameter) : parameter)), ErrorReportedElement);

        public readonly List<TypeParameter> TypeParameters;

        private List<IType>? options;

        public EnumDeclaration(string name, List<TypeParameter> typeParameters, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement) : base()
        {
            Name = name;
            TypeParameters = typeParameters;
            ErrorReportedElement = errorReportedElement;
            ParentContainer = parentContainer;
        }

        public List<IType> GetOptions(EnumType enumType)
        {
            if (enumType.EnumDeclaration != this)
                throw new InvalidOperationException();
            if (options == null)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new(TypeParameters.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], enumType.TypeArguments[i]);

            List<IType> typeOptions = new(options.Count);
            foreach (IType option in options)
                typeOptions.Add(option.SubstituteWithTypearg(typeargs));

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

        public MarshalIntoEnum(EnumType targetType, IRValue value, Syntax.IAstElement errorReportedElement)
        {
            TargetType = targetType;
            Value = value;
            ErrorReportedElement = errorReportedElement;

            if (TargetType.SupportsType(value.Type))
                return;
            else if (value.Type is TypeParameterReference typeParameterReference)
            {
                if (typeParameterReference.TypeParameter.RequiredImplementedInterface is not null)
                {
                    if (!targetType.SupportsType(typeParameterReference.TypeParameter.RequiredImplementedInterface))
                        throw new UnexpectedTypeException(value.Type, errorReportedElement);
                }
                else
                    throw new UnexpectedTypeException(value.Type, errorReportedElement);
            }
            else
            {
                foreach(IType options in TargetType.GetOptions())
                    try
                    {
                        Value = ArithmeticCast.CastTo(value, options);
                        return;
                    }
                    catch (UnexpectedTypeException)
                    {
                        continue;
                    }
                throw new UnexpectedTypeException(value.Type, errorReportedElement);
            }
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => ArithmeticCast.CastTo(Value.SubstituteWithTypearg(typeargs), TargetType.SubstituteWithTypearg(typeargs));
    }
}

namespace NoHoPython.Typing
{
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class EnumType : IType, IPropertyContainer
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        private static Dictionary<EnumType, Lazy<Dictionary<string, Property>>> globalSupportedProperties = new(new ITypeComparer());

        public bool IsNativeCType => false;
        public string TypeName => $"{EnumDeclaration.Name}{(TypeArguments.Count == 0 ? string.Empty : $"<{string.Join(", ", TypeArguments.ConvertAll((arg) => arg.TypeName))}>")}";
        public string Identifier => $"{EnumDeclaration.Name}{(TypeArguments.Count == 0 ? string.Empty : $"_with_{string.Join("_", TypeArguments.ConvertAll((arg) => arg.TypeName))}")}";

        public EnumDeclaration EnumDeclaration { get; private set; }
        public readonly List<IType> TypeArguments;

        private Lazy<List<IType>> options;

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => throw new NoDefaultValueError(this, errorReportedElement);

        public EnumType(EnumDeclaration enumDeclaration, List<IType> typeArguments, Syntax.IAstElement errorReportedElement) : this(enumDeclaration, TypeParameter.ValidateTypeArguments(enumDeclaration.TypeParameters, typeArguments, errorReportedElement))
        {
            
        }

        private EnumType(EnumDeclaration enumDeclaration, List<IType> typeArguments)
        {
            EnumDeclaration = enumDeclaration;
            TypeArguments = typeArguments;

            options = new Lazy<List<IType>>(() => enumDeclaration.GetOptions(this));

            if (globalSupportedProperties.ContainsKey(this))
                return;

            globalSupportedProperties[this] = new(() =>
            {
                if (!options.Value.TrueForAll((option) => option is IPropertyContainer))
                    return new();
                IPropertyContainer firstType = (IPropertyContainer)options.Value[0];

                List<Property> firstTypeProperties = firstType.GetProperties();
                Dictionary<string, Property> propertyIdMap = new(firstTypeProperties.Count);
                foreach (Property property in firstTypeProperties)
                {
                    bool foundFlag = true;
                    for (int i = 1; i < options.Value.Count; i++)
                    {
                        IPropertyContainer optionContainer = (IPropertyContainer)options.Value[i];
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
                        propertyIdMap.Add(property.Name, property);
                };
                //foreach (InterfaceType requiredImplementedInterface in EnumDeclaration.GetRequiredImplementedInterfaces(this))
                //    if (!requiredImplementedInterface.SupportsProperties(propertyIdMap.Values.ToList()))
                //        throw new UnexpectedTypeException(this, EnumDeclaration.ErrorReportedElement);
                return propertyIdMap;
            });
        }

        public List<IType> GetOptions() => options.Value;

        public List<Property> GetProperties() => globalSupportedProperties[this].Value.Values.ToList();

        public Property FindProperty(string identifier) => globalSupportedProperties[this].Value[identifier];

        public bool HasProperty(string identifier) => globalSupportedProperties[this].Value.ContainsKey(identifier);

        public bool SupportsType(IType type)
        {
            foreach (IType option in options.Value)
                if (option.IsCompatibleWith(type))
                    return true;
            return false;
        }

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

            IREnumDeclaration = new IntermediateRepresentation.Statements.EnumDeclaration(Identifier, typeParameters, irBuilder.SymbolMarshaller.CurrentModule, this);
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
            IREnumDeclaration.DelayedLinkSetOptions(Options.ConvertAll((AstType option) => option.ToIRType(irBuilder, this)));
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