using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class EnumDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        private List<IType>? options;

        public EnumDeclaration(string name, List<TypeParameter> typeParameters) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            TypeParameters = typeParameters;
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
        public IType Type => TargetType;

        public EnumType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoEnum(EnumType targetType, IRValue value)
        {
            TargetType = targetType;
            Value = value;

            if (value.Type is TypeParameterReference typeParameterReference)
            {
                if (typeParameterReference.TypeParameter.RequiredImplementedInterface is not null)
                {
                    if (!targetType.SupportsType(typeParameterReference.TypeParameter.RequiredImplementedInterface))
                        throw new UnexpectedTypeException(value.Type);
                }
                else
                    throw new UnexpectedTypeException(value.Type);
            }
            else if (!TargetType.SupportsType(value.Type))
                throw new UnexpectedTypeException(value.Type);
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

        public readonly List<IType> Options;

        public EnumType(EnumDeclaration enumDeclaration, List<IType> typeArguments)
        {
            EnumDeclaration = enumDeclaration;
            TypeArguments = typeArguments;
            TypeParameter.ValidateTypeArguments(enumDeclaration.TypeParameters, typeArguments);

            Options = enumDeclaration.GetOptions(this);
        }

        public bool SupportsType(IType type)
        {
            foreach (IType option in Options)
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

            IREnumDeclaration = new IntermediateRepresentation.Statements.EnumDeclaration(Identifier, typeParameters);
            irBuilder.SymbolMarshaller.DeclareSymbol(IREnumDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IREnumDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            IREnumDeclaration.DelayedLinkSetOptions(Options.ConvertAll((AstType option) => option.ToIRType(irBuilder, this)));
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IREnumDeclaration;
    }
}