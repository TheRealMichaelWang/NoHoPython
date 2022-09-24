using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

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
        
        public SymbolContainer? CurrentMasterScope => ScopedProcedures.Count > 0 ? ScopedProcedures.Peek() : (ScopedRecordDeclaration != null) ? ScopedRecordDeclaration : SymbolMarshaller.CurrentModule;

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

        public IRProgram ToIRProgram() => new(RecordDeclarations, InterfaceDeclarations, EnumDeclarations, ProcedureDeclarations, new List<string>()
        {
            "<stdio.h>",
            "<stdlib.h>",
            "<string.h>",
            "<math.h>"
        });
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    public interface IRElement
    {
        public Syntax.IAstElement ErrorReportedElement { get; }

        //scope for used types
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs);
    }

    public partial interface IRValue : IRElement
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs);

        public IType Type { get; }

        //equivalent value but with type parameter references replaced
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);

        //emit corresponding C code
        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs);
    }

    public interface IRStatement : IRElement
    {
        //forward declare type definitions
        public void ForwardDeclareType(StringBuilder emitter);

        //forward declare functions
        public void ForwardDeclare(StringBuilder emitter);

        //emit corresponding C code
        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent);
    }

    public sealed class IRProgram
    {
        public readonly List<RecordDeclaration> RecordDeclarations;
        public readonly List<InterfaceDeclaration> InterfaceDeclarations;
        public readonly List<EnumDeclaration> EnumDeclarations;
        public readonly List<ProcedureDeclaration> ProcedureDeclarations;

        public readonly List<string> IncludedCFiles;

        private List<ProcedureDeclaration> compileHeads;

        public IRProgram(List<RecordDeclaration> recordDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<EnumDeclaration> enumDeclarations, List<ProcedureDeclaration> procedureDeclarations, List<string> includedCFiles)
        {
            RecordDeclarations = recordDeclarations;
            InterfaceDeclarations = interfaceDeclarations;
            EnumDeclarations = enumDeclarations;
            ProcedureDeclarations = procedureDeclarations;

            compileHeads = new List<ProcedureDeclaration>();
            foreach (ProcedureDeclaration procedureDeclaration in procedureDeclarations)
                if (procedureDeclaration.IsCompileHead)
                    compileHeads.Add(procedureDeclaration);

            foreach (ProcedureDeclaration procedureDeclaration in compileHeads)
                procedureDeclaration.ScopeAsCompileHead();
            ProcedureDeclaration.ScopeForAllSecondaryProcedures();
            IncludedCFiles = includedCFiles;
        }

        public void Emit(StringBuilder emitter)
        {
            foreach (string includedCFile in IncludedCFiles)
                if (includedCFile.StartsWith('<'))
                    emitter.AppendLine($"#include {includedCFile}");
                else
                    emitter.AppendLine($"#include \"{includedCFile}\"");

            emitter.AppendLine();

            //emit typedefs
            ArrayType.ForwardDeclareArrayTypes(emitter);
            ProcedureType.EmitTypedefs(emitter);
            EnumDeclaration.ForwardDeclareInterfaceTypes(emitter);
            InterfaceDeclaration.ForwardDeclareInterfaceTypes(emitter);
            RecordDeclaration.ForwardDeclareRecordTypes(emitter);

            //emit c structs
            ArrayType.EmitCStructs(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclareType(emitter));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclareType(emitter));
            RecordDeclarations.ForEach((record) => record.ForwardDeclareType(emitter));
            ProcedureType.ForwardDeclareProcedureTypes(emitter);

            //emit function headers
            ArrayType.ForwardDeclare(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclare(emitter));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclare(emitter));
            RecordDeclarations.ForEach((record) => record.ForwardDeclare(emitter));
            ProcedureDeclarations.ForEach((procedure) => procedure.ForwardDeclare(emitter));
            ProcedureDeclaration.EmitCapturedContecies(emitter);

            //emit function behavior
            ArrayType.EmitMarshallers(emitter);
            ProcedureType.EmitMovers(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.Emit(emitter, new Dictionary<TypeParameter, IType>(), 0));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.Emit(emitter, new Dictionary<TypeParameter, IType>(), 0));
            RecordDeclarations.ForEach((record) => record.Emit(emitter, new Dictionary<TypeParameter, IType>(), 0));
            ProcedureDeclarations.ForEach((proc) => proc.Emit(emitter, new Dictionary<TypeParameter, IType>(), 0));
        }
    }
}