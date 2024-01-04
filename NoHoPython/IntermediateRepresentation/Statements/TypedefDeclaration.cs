using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class TypedefDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public string Name { get; private set; }
        public AstType DefinedType { get; private set; }
        public readonly List<Typing.TypeParameter> TypeParameters;

        public IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }
        public override bool IsGloballyNavigable => false;

        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;

        public TypedefDeclaration(string name, AstType definedType, List<Typing.TypeParameter> typeParameters, SymbolContainer parentContainer, IAstElement errorReportedElement)
        {
            Name = name;
            TypeParameters = typeParameters;
            DefinedType = definedType;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }

        public void Emit(IRProgram irProgram, Emitter emitter, Dictionary<Typing.TypeParameter, IType> typeargs) { }

        public void ScopeForUsedTypes(Dictionary<Typing.TypeParameter, IType> typeargs, AstIRProgramBuilder irBuilder) { }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public void FunctionMutabilityAnalysis() { }

        public void EnsureMinimumPurity(Purity purity) { }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class TypedefDeclaration
    {
        private IntermediateRepresentation.Statements.TypedefDeclaration IRTypedefedDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            IRTypedefedDeclaration = new IntermediateRepresentation.Statements.TypedefDeclaration(Identifier, DefinedType, typeParameters, irBuilder.SymbolMarshaller.CurrentScope, this);

            irBuilder.SymbolMarshaller.DeclareSymbol(IRTypedefedDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRTypedefedDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            irBuilder.SymbolMarshaller.GoBack();
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IRTypedefedDeclaration;
    }
}

namespace NoHoPython.Syntax
{
    partial class AstType
    {
        private IType TypedefToIRType(TypedefDeclaration typedefDeclaration, List<IType> typeArguments, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (typeArguments.Count != typedefDeclaration.TypeParameters.Count)
                throw new UnexpectedTypeArgumentsException(typeArguments.Count, typedefDeclaration.TypeParameters.Count, errorReportedElement);

            Dictionary<Typing.TypeParameter, IType> typeargMap = new(typeArguments.Count);
            for (int i = 0; i < typeArguments.Count; i++)
                typeargMap.Add(typedefDeclaration.TypeParameters[i], typeArguments[i]);

            irBuilder.SymbolMarshaller.NavigateToScope(typedefDeclaration);

            IType expanded = typedefDeclaration.DefinedType.ToIRType(irBuilder, errorReportedElement).SubstituteWithTypearg(typeargMap);

            irBuilder.SymbolMarshaller.GoBack();

            return expanded;
        }
    }
}