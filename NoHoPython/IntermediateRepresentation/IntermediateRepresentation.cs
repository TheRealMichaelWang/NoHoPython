using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    public sealed partial class AstIRProgramBuilder
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
            SymbolMarshaller = new SymbolMarshaller();
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

        public IRProgram ToIRProgram()
        {
            List<ProcedureDeclaration> compileHeads = new List<ProcedureDeclaration>();
            foreach (ProcedureDeclaration procedureDeclaration in ProcedureDeclarations)
                if (procedureDeclaration.IsCompileHead)
                    compileHeads.Add(procedureDeclaration);
            foreach (ProcedureDeclaration procedureDeclaration in compileHeads)
                procedureDeclaration.ScopeAsCompileHead(this);
            ScopeForAllSecondaryProcedures();

            return new(true,
                RecordDeclarations, InterfaceDeclarations, EnumDeclarations, ProcedureDeclarations, new List<string>()
                {
                    "<stdio.h>",
                    "<stdlib.h>",
                    "<string.h>",
                    "<math.h>"
                },
                usedArrayTypes, uniqueProcedureTypes, usedProcedureReferences, procedureOverloads, usedEnumTypes, enumTypeOverloads, usedInterfaceTypes, interfaceTypeOverloads, usedRecordTypes, recordTypeOverloads);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    public interface IRElement
    {
        public Syntax.IAstElement ErrorReportedElement { get; }

        //scope for used types
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder);
    }

    public partial interface IRValue : IRElement
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs);

        public IType Type { get; }

        //equivalent value but with type parameter references replaced
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);

        //emit corresponding C code
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs);
    }

    public partial interface IRStatement : IRElement
    {
        //forward declare type definitions
        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter);

        //forward declare functions
        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter);

        //emit corresponding C code
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent);
    }

    public sealed partial class IRProgram
    {
        public readonly List<RecordDeclaration> RecordDeclarations;
        public readonly List<InterfaceDeclaration> InterfaceDeclarations;
        public readonly List<EnumDeclaration> EnumDeclarations;
        public readonly List<ProcedureDeclaration> ProcedureDeclarations;

        public readonly SortedSet<string> IncludedCFiles;

        public bool DoBoundsChecking { get; private set; }

        public IRProgram(bool doBoundsChecking, List<RecordDeclaration> recordDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<EnumDeclaration> enumDeclarations, List<ProcedureDeclaration> procedureDeclarations, List<string> includedCFiles, List<ArrayType> usedArrayTypes, List<Tuple<ProcedureType, string>> uniqueProcedureTypes, List<ProcedureReference> usedProcedureReferences, Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads, List<EnumType> usedEnumTypes, Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads, List<InterfaceType> usedInterfaceTypes, Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverload, List<RecordType> usedRecordTypes, Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads)
        {
            DoBoundsChecking = doBoundsChecking;

            RecordDeclarations = recordDeclarations;
            InterfaceDeclarations = interfaceDeclarations;
            EnumDeclarations = enumDeclarations;
            ProcedureDeclarations = procedureDeclarations;
            IncludedCFiles = new SortedSet<string>(includedCFiles);
            
            this.uniqueProcedureTypes = uniqueProcedureTypes;
            this.usedArrayTypes = usedArrayTypes;
            this.usedProcedureReferences = usedProcedureReferences;
            ProcedureOverloads = procedureOverloads;
            this.usedEnumTypes = usedEnumTypes;
            EnumTypeOverloads = enumTypeOverloads;
            this.usedInterfaceTypes = usedInterfaceTypes;
            InterfaceTypeOverloads = interfaceTypeOverload;
            this.usedRecordTypes = usedRecordTypes;
            RecordTypeOverloads = recordTypeOverloads;
        }

        public void IncludeCFile(string cFile)
        {
            if (!IncludedCFiles.Contains(cFile))
                IncludedCFiles.Add(cFile);
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
            EmitArrayTypeTypedefs(emitter);
            EmitAnonProcedureTypedefs(emitter);
            ForwardDeclareEnumTypes(emitter);
            ForwardDeclareInterfaceTypes(emitter);
            ForwardDeclareRecordTypes(emitter);

            //emit c structs
            EmitArrayTypeCStructs(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclareType(this, emitter));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclareType(this, emitter));
            RecordDeclarations.ForEach((record) => record.ForwardDeclareType(this, emitter));
            ForwardDeclareAnonProcedureTypes(emitter);

            //emit function headers
            ForwardDeclareArrayTypes(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclare(this, emitter));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclare(this, emitter));
            RecordDeclarations.ForEach((record) => record.ForwardDeclare(this, emitter));
            ProcedureDeclarations.ForEach((procedure) => procedure.ForwardDeclareActual(this, emitter));
            EmitAnonProcedureCapturedContecies(emitter);

            //emit function behavior
            EmitArrayTypeMarshallers(emitter);
            AssertStatement.EmitAsserter(emitter);
            EmitAnonProcedureMovers(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.Emit(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.Emit(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
            RecordDeclarations.ForEach((record) => record.Emit(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
            ProcedureDeclarations.ForEach((proc) => proc.EmitActual(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
        }
    }
}