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
        
        public SymbolContainer CurrentMasterScope => ScopedProcedures.Count > 0 ? ScopedProcedures.Peek() : (ScopedRecordDeclaration != null) ? ScopedRecordDeclaration : SymbolMarshaller.CurrentModule;

        private Dictionary<IType, HashSet<IType>> typeDependencyTree;

        public AstIRProgramBuilder(List<IAstStatement> statements)
        {
            SymbolMarshaller = new SymbolMarshaller();
            EnumDeclarations = new List<EnumDeclaration>();
            InterfaceDeclarations = new List<InterfaceDeclaration>();
            RecordDeclarations = new List<RecordDeclaration>();
            ProcedureDeclarations = new List<ProcedureDeclaration>();
            ScopedProcedures = new Stack<ProcedureDeclaration>();
            ScopedRecordDeclaration = null;

            typeDependencyTree = new Dictionary<IType, HashSet<IType>>(new ITypeComparer());

            statements.ForEach((IAstStatement statement) => statement.ForwardTypeDeclare(this));
            statements.ForEach((IAstStatement statement) => statement.ForwardDeclare(this));
            statements.ForEach((IAstStatement statement) => statement.GenerateIntermediateRepresentationForStatement(this));
            LinkCapturedVariables();
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
            List<ProcedureDeclaration> compileHeads = new();
            foreach (ProcedureDeclaration procedureDeclaration in ProcedureDeclarations)
                if (procedureDeclaration.IsCompileHead)
                    compileHeads.Add(procedureDeclaration);
            foreach (ProcedureDeclaration procedureDeclaration in compileHeads)
                procedureDeclaration.ScopeAsCompileHead(this);
            ScopeForAllSecondaryProcedures();

            return new(true, false, true,
                RecordDeclarations, InterfaceDeclarations, EnumDeclarations, ProcedureDeclarations, new List<string>()
                {
                    "<stdio.h>",
                    "<stdlib.h>",
                    "<string.h>",
                    "<math.h>"
                },
                usedArrayTypes, uniqueProcedureTypes, usedProcedureReferences, procedureOverloads, usedEnumTypes, enumTypeOverloads, usedInterfaceTypes, interfaceTypeOverloads, usedRecordTypes, recordTypeOverloads, typeDependencyTree);
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

        //gets a pure value - one that doesn't mutate state once evaluated - that can be safley evaluated following evaluation of the parent value
        public IRValue GetPostEvalPure();

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
        public bool EliminateAsserts { get; private set; }
        public bool EmitExpressionStatements { get; private set; }

        private Dictionary<IType, HashSet<IType>> typeDependencyTree;
        private HashSet<IType> compiledTypes;

        public IRProgram(bool doBoundsChecking, bool eliminateAsserts, bool emitExpressionStatements, List<RecordDeclaration> recordDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<EnumDeclaration> enumDeclarations, List<ProcedureDeclaration> procedureDeclarations, List<string> includedCFiles, List<ArrayType> usedArrayTypes, List<Tuple<ProcedureType, string>> uniqueProcedureTypes, List<ProcedureReference> usedProcedureReferences, Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads, List<EnumType> usedEnumTypes, Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads, List<InterfaceType> usedInterfaceTypes, Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverload, List<RecordType> usedRecordTypes, Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads, Dictionary<IType, HashSet<IType>> typeDependencyTree)
        {
            DoBoundsChecking = doBoundsChecking;
            EliminateAsserts = eliminateAsserts;
            EmitExpressionStatements = emitExpressionStatements;

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

            this.typeDependencyTree = typeDependencyTree;
            compiledTypes = new HashSet<IType>(new ITypeComparer());

            void SantizeNoCircularDependencies(IType type, List<IType> dependentTypes)
            {
                if (!typeDependencyTree.ContainsKey(type))
                    return;

                if (dependentTypes.Contains(type, new ITypeComparer()))
                    throw new CircularDependentTypesError(dependentTypes, type);
                dependentTypes.Add(type);
                foreach (IType dependency in typeDependencyTree[type])
                    SantizeNoCircularDependencies(dependency, dependentTypes);
                dependentTypes.Remove(type);
            }
            foreach (IType type in typeDependencyTree.Keys)
                SantizeNoCircularDependencies(type, new List<IType>());
        }

        public void IncludeCFile(string cFile)
        {
            if (!IncludedCFiles.Contains(cFile))
                IncludedCFiles.Add(cFile);
        }

        public bool DeclareCompiledType(StringBuilder emitter, IType type)
        {
            if (!compiledTypes.Add(type))
                return false;
            CompileUncompiledDependencies(emitter, type);
            return true;
        }

        public void CompileUncompiledDependencies(StringBuilder emitter, IType type)
        {
            foreach (IType dependency in typeDependencyTree[type])
                if (!compiledTypes.Contains(dependency))
                    dependency.EmitCStruct(this, emitter);
        }

        public void Emit(StringBuilder emitter)
        {
            foreach (string includedCFile in IncludedCFiles)
                if (includedCFile.StartsWith('<'))
                    emitter.AppendLine($"#include {includedCFile}");
                else
                    emitter.AppendLine($"#include \"{includedCFile}\"");

            emitter.AppendLine();

            this.compiledTypes.Clear();
            //emit typedefs
            EmitArrayTypeTypedefs(emitter);
            ForwardDeclareEnumTypes(emitter);
            ForwardDeclareInterfaceTypes(emitter);
            EmitAnonProcedureTypedefs(emitter);
            ForwardDeclareRecordTypes(emitter);

            //emit c structs
            EmitArrayTypeCStructs(emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclareType(this, emitter));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclareType(this, emitter));
            RecordDeclarations.ForEach((record) => record.ForwardDeclareType(this, emitter));
            ForwardDeclareAnonProcedureTypes(this, emitter);

            //emit function headers
            ForwardDeclareArrayTypes(this, emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclare(this, emitter));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclare(this, emitter));
            RecordDeclarations.ForEach((record) => record.ForwardDeclare(this, emitter));
            ProcedureDeclarations.ForEach((procedure) => procedure.ForwardDeclareActual(this, emitter));
            EmitAnonProcedureCapturedContecies(emitter);

            //emit function behavior
            EmitArrayTypeMarshallers(emitter);
            AssertStatement.EmitAsserter(emitter);
            EmitAnonProcedureMovers(this, emitter);
            EnumDeclarations.ForEach((enumDecl) => enumDecl.Emit(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
            InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.Emit(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
            RecordDeclarations.ForEach((record) => record.Emit(this, emitter, new Dictionary<TypeParameter, IType>(), 0));
            ProcedureDeclarations.ForEach((proc) => proc.EmitActual(this, emitter, 0));
        }
    }
}