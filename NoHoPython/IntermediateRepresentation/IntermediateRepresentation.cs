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
        public List<EnumDeclaration> EnumDeclarations { get; private set; }
        public List<InterfaceDeclaration> InterfaceDeclarations { get; private set; }
        public List<RecordDeclaration> RecordDeclarations { get; private set; }
        public List<ProcedureDeclaration> ProcedureDeclarations { get; private set; }

        public Stack<ProcedureDeclaration> ScopedProcedures { get; private set; }
        public RecordDeclaration? ScopedRecordDeclaration { get; private set; }
        public SymbolMarshaller SymbolMarshaller { get; private set; }

        public IRProgramBuilder()
        {
            SymbolMarshaller = new SymbolMarshaller(new List<IScopeSymbol>());
            EnumDeclarations = new List<EnumDeclaration>();
            InterfaceDeclarations = new List<InterfaceDeclaration>();
            RecordDeclarations = new List<RecordDeclaration>();
            ProcedureDeclarations = new List<ProcedureDeclaration>();
            ScopedProcedures = new Stack<ProcedureDeclaration>();
            ScopedRecordDeclaration = null;
        }

        public void ScopeToRecord(RecordDeclaration recordDeclaration)
        {
            if (ScopedRecordDeclaration != null)
                throw new InvalidOperationException();
            ScopedRecordDeclaration = recordDeclaration;
        }

        public void ScopeBackFromRecord()
        {
            if (ScopedRecordDeclaration == null)
                throw new InvalidOperationException();
            ScopedRecordDeclaration = null;
        }
    }
}