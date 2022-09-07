using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    public interface IRValue
    {
        public IType Type { get; }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
    }

    public interface IRStatement
    {

    }

    public sealed class IRProgramBuilder
    {
        public List<EnumDeclaration> EnumDeclarations;
        public List<InterfaceDeclaration> InterfaceDeclarations;
        public List<RecordDeclaration> RecordDeclarations;

        public List<ProcedureDeclaration> ProcedureDeclarations;
        public SymbolMarshaller SymbolMarshaller;

        public IRProgramBuilder()
        {
            SymbolMarshaller = new SymbolMarshaller(new List<IScopeSymbol>());
            EnumDeclarations = new List<EnumDeclaration>();
            InterfaceDeclarations = new List<InterfaceDeclaration>();
            RecordDeclarations = new List<RecordDeclaration>();
            ProcedureDeclarations = new List<ProcedureDeclaration>();
        }
    }
}