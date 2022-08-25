using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class EnumDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        private readonly List<IType> options;

        public EnumDeclaration(string name, List<IType> options, List<TypeParameter> typeParameters) : base(typeParameters.ConvertAll<IScopeSymbol>((TypeParameter typeParam) => typeParam))
        {
            Name = name;
            this.options = options;
            TypeParameters = typeParameters;
        }

        public List<IType> GetOptions(EnumType enumType)
        {
            if (enumType.EnumDeclaration != this)
                throw new InvalidOperationException();

            Dictionary<TypeParameter, IType> typeargs = new (TypeParameters.Count);
            for (int i = 0; i < TypeParameters.Count; i++)
                typeargs.Add(TypeParameters[i], enumType.TypeArguments[i]);

            List<IType> typeOptions = new(options.Count);
            foreach (IType option in options)
                typeOptions.Add(option.SubstituteWithTypearg(typeargs));

            return typeOptions;
        }
    }
}
