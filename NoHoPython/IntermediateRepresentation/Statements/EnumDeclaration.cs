using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class EnumDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer? ParentContainer { get; private set; }

        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        private List<IType>? options;

        public EnumDeclaration(string name, List<TypeParameter> typeParameters, SymbolContainer? parentContainer, Syntax.IAstElement errorReportedElement) : base()
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
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class MarshalIntoEnum : IRValue
    {
        public Syntax.IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TargetType;

        public EnumType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoEnum(EnumType targetType, IRValue value, Syntax.IAstElement errorReportedElement)
        {
            TargetType = targetType;
            Value = value;
            ErrorReportedElement = errorReportedElement;

            if (value.Type is TypeParameterReference typeParameterReference)
            {
                if (typeParameterReference.TypeParameter.RequiredImplementedInterface is not null)
                {
                    if (!targetType.SupportsType(typeParameterReference.TypeParameter.RequiredImplementedInterface))
                        throw new UnexpectedTypeException(value.Type, errorReportedElement);
                }
                else
                    throw new UnexpectedTypeException(value.Type, errorReportedElement);
            }
            else if (!TargetType.SupportsType(value.Type))
                throw new UnexpectedTypeException(value.Type, errorReportedElement);
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => ArithmeticCast.CastTo(Value.SubstituteWithTypearg(typeargs), TargetType.SubstituteWithTypearg(typeargs));
    }
}

namespace NoHoPython.Typing
{
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class EnumType : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => EnumDeclaration.Name; }

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
        }

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
                if (enumType.EnumDeclaration != enumType.EnumDeclaration)
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

            IREnumDeclaration = new IntermediateRepresentation.Statements.EnumDeclaration(Identifier, typeParameters, irBuilder.CurrentMasterScope, this);
            irBuilder.SymbolMarshaller.DeclareSymbol(IREnumDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IREnumDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            irBuilder.SymbolMarshaller.GoBack();

            irBuilder.AddEnumDeclaration(IREnumDeclaration);
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            IREnumDeclaration.DelayedLinkSetOptions(Options.ConvertAll((AstType option) => option.ToIRType(irBuilder, this)));
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IREnumDeclaration;
    }
}