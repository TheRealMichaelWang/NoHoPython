using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.Syntax
{
    public sealed class AstIRProgramBuilder
    {
        private List<EnumDeclaration> EnumDeclarations;
        private List<InterfaceDeclaration> InterfaceDeclarations;
        private List<RecordDeclaration> RecordDeclarations;
        private List<ProcedureDeclaration> ProcedureDeclarations;

        public Stack<ProcedureDeclaration> ScopedProcedures { get; private set; }
        public RecordDeclaration? ScopedRecordDeclaration { get; private set; }
        public SymbolMarshaller SymbolMarshaller { get; private set; }

        public AstIRProgramBuilder(List<IAstStatement> statements)
        {
            SymbolMarshaller = new SymbolMarshaller(new List<IScopeSymbol>());
            EnumDeclarations = new List<EnumDeclaration>();
            InterfaceDeclarations = new List<InterfaceDeclaration>();
            RecordDeclarations = new List<RecordDeclaration>();
            ProcedureDeclarations = new List<ProcedureDeclaration>();
            ScopedProcedures = new Stack<ProcedureDeclaration>();
            ScopedRecordDeclaration = null;

            statements.ForEach((IAstStatement statement) => statement.ForwardTypeDeclare(this));
            statements.ForEach((IAstStatement statement) => statement.ForwardDeclare(this));
            statements.ForEach((IAstStatement statement) => statement.GenerateIntermediateRepresentationForStatement(this));
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

        public void AddRecordDeclaration(RecordDeclaration recordDeclaration) => RecordDeclarations.Add(recordDeclaration);
        public void AddInterfaceDeclaration(InterfaceDeclaration interfaceDeclaration) => InterfaceDeclarations.Add(interfaceDeclaration);
        public void AddEnumDeclaration(EnumDeclaration enumDeclaration) => EnumDeclarations.Add(enumDeclaration);
        public void AddProcDeclaration(ProcedureDeclaration procedureDeclaration) => ProcedureDeclarations.Add(procedureDeclaration);

        public IRProgram ToIRProgram() => new IRProgram(RecordDeclarations, InterfaceDeclarations, EnumDeclarations, ProcedureDeclarations);
    }
}

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

    public sealed class IRProgram
    {
        public readonly List<RecordDeclaration> RecordDeclarations;
        public readonly List<InterfaceDeclaration> InterfaceDeclarations;
        public readonly List<EnumDeclaration> EnumDeclarations;
        public readonly List<ProcedureDeclaration> ProcedureDeclarations;

        public IRProgram(List<RecordDeclaration> recordDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<EnumDeclaration> enumDeclarations, List<ProcedureDeclaration> procedureDeclarations)
        {
            RecordDeclarations = recordDeclarations;
            InterfaceDeclarations = interfaceDeclarations;
            EnumDeclarations = enumDeclarations;
            ProcedureDeclarations = procedureDeclarations;
        }
    }
}