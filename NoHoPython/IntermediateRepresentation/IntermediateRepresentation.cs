using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

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

        public readonly SortedSet<string> Flags;

        private Dictionary<IType, HashSet<IType>> typeDependencyTree;

        public AstIRProgramBuilder(List<IAstStatement> statements, List<string> flags)
        {
            SymbolMarshaller = new SymbolMarshaller();
            EnumDeclarations = new List<EnumDeclaration>();
            InterfaceDeclarations = new List<InterfaceDeclaration>();
            RecordDeclarations = new List<RecordDeclaration>();
            ProcedureDeclarations = new List<ProcedureDeclaration>();
            ScopedProcedures = new Stack<ProcedureDeclaration>();
            Flags = new SortedSet<string>(flags);
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

        public IRProgram ToIRProgram(bool doBoundsChecking, bool eliminateAsserts, bool emitExpressionStatements, bool doCallStack, bool nameRuntimeTypes, bool emitLineDirectives, MemoryAnalyzer memoryAnalyzer)
        {
            List<ProcedureDeclaration> compileHeads = new();
            foreach (ProcedureDeclaration procedureDeclaration in ProcedureDeclarations)
                if (procedureDeclaration.IsCompileHead)
                    compileHeads.Add(procedureDeclaration);
            foreach (ProcedureDeclaration procedureDeclaration in compileHeads)
                procedureDeclaration.ScopeAsCompileHead(this);
            ScopeForAllSecondaryProcedures();

            return new(doBoundsChecking, eliminateAsserts, emitExpressionStatements, doCallStack, nameRuntimeTypes, emitLineDirectives,
                RecordDeclarations, InterfaceDeclarations, EnumDeclarations, ProcedureDeclarations, new List<string>()
                {
                    "<stdio.h>",
                    "<stdlib.h>",
                    "<string.h>",
                    "<math.h>"
                },
                usedArrayTypes, usedTupleTypes.ToList(), uniqueProcedureTypes, usedProcedureReferences, procedureOverloads, usedEnumTypes, enumTypeOverloads, usedInterfaceTypes, interfaceTypeOverloads, usedRecordTypes.ToList(), recordTypeOverloads, typeDependencyTree, memoryAnalyzer);
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
        public bool IsTruey { get; }
        public bool IsFalsey { get; }

        //equivalent value but with type parameter references replaced
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);

        //emit corresponding C code
        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer);
    }

    public partial interface IRStatement : IRElement
    {
        //emit corresponding C code
        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent);
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
        public bool DoCallStack { get; private set; }
        public bool NameRuntimeTypes { get; private set; }
        public bool EmitLineDirectives { get;private set; } 

        private Dictionary<IType, HashSet<IType>> typeDependencyTree;
        private HashSet<IType> compiledTypes;

        public MemoryAnalyzer MemoryAnalyzer { get; private set; }

        public int ExpressionDepth;

        public IRProgram(bool doBoundsChecking, bool eliminateAsserts, bool emitExpressionStatements, bool doCallStack, bool nameRuntimeTypes, bool emitLineDirectives, List<RecordDeclaration> recordDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<EnumDeclaration> enumDeclarations, List<ProcedureDeclaration> procedureDeclarations, List<string> includedCFiles, List<ArrayType> usedArrayTypes, List<TupleType> usedTupleTypes, List<Tuple<ProcedureType, string>> uniqueProcedureTypes, List<ProcedureReference> usedProcedureReferences, Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads, List<EnumType> usedEnumTypes, Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads, List<InterfaceType> usedInterfaceTypes, Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverload, List<RecordType> usedRecordTypes, Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads, Dictionary<IType, HashSet<IType>> typeDependencyTree, MemoryAnalyzer memoryAnalyzer)
        {
            DoBoundsChecking = doBoundsChecking;
            EliminateAsserts = eliminateAsserts;
            EmitExpressionStatements = emitExpressionStatements;
            DoCallStack = doCallStack;
            NameRuntimeTypes = nameRuntimeTypes;
            EmitLineDirectives = emitLineDirectives;

            RecordDeclarations = recordDeclarations;
            InterfaceDeclarations = interfaceDeclarations;
            EnumDeclarations = enumDeclarations;
            ProcedureDeclarations = procedureDeclarations;
            IncludedCFiles = new SortedSet<string>(includedCFiles);

            this.uniqueProcedureTypes = uniqueProcedureTypes;
            this.usedArrayTypes = usedArrayTypes;
            this.usedTupleTypes = usedTupleTypes;
            this.usedProcedureReferences = usedProcedureReferences;
            ProcedureOverloads = procedureOverloads;
            this.usedEnumTypes = usedEnumTypes;
            EnumTypeOverloads = enumTypeOverloads;
            this.usedInterfaceTypes = usedInterfaceTypes;
            InterfaceTypeOverloads = interfaceTypeOverload;
            this.usedRecordTypes = usedRecordTypes;
            RecordTypeOverloads = recordTypeOverloads;

            MemoryAnalyzer = memoryAnalyzer;
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

        public bool DeclareCompiledType(StatementEmitter emitter, IType type)
        {
            if (!compiledTypes.Add(type))
                return false;
            CompileUncompiledDependencies(emitter, type);
            return true;
        }

        public void CompileUncompiledDependencies(StatementEmitter emitter, IType type)
        {
            foreach (IType dependency in typeDependencyTree[type])
                if (!compiledTypes.Contains(dependency))
                    dependency.EmitCStruct(this, emitter);
        }

        public void Emit(string outputFile, string? headerFile)
        {
            using (StatementEmitter emitter = new(outputFile, this))
            using(StatementEmitter headerEmitter = (headerFile == null) ? emitter : new StatementEmitter(headerFile, this))
            {
                ExpressionDepth = 0;

                if (headerEmitter != emitter)
                {
                    headerEmitter.AppendLine("#ifndef _NHP_HEADER_GUARD_");
                    headerEmitter.AppendLine("#define _NHP_HEADER_GUARD_");
                }
                foreach (string includedCFile in IncludedCFiles)
                    if (includedCFile.StartsWith('<'))
                        emitter.AppendLine($"#include {includedCFile}");
                    else
                        emitter.AppendLine($"#include \"{includedCFile}\"");
                emitter.AppendLine();

                //emit typedefs
                this.compiledTypes.Clear();
                EmitArrayTypeTypedefs(headerEmitter);
                EmitTupleTypeTypedefs(headerEmitter);
                ForwardDeclareEnumTypes(headerEmitter);
                ForwardDeclareInterfaceTypes(headerEmitter);
                EmitAnonProcedureTypedefs(headerEmitter);
                ForwardDeclareRecordTypes(headerEmitter);

                //emit c structs
                RecordDeclaration.EmitRecordMaskProto(headerEmitter);
                RecordDeclaration.EmitRecordChildFinder(headerEmitter);
                EmitArrayTypeCStructs(headerEmitter);
                EmitTupleCStructs(headerEmitter);
                EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclareType(this, headerEmitter));
                InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclareType(this, headerEmitter));
                RecordDeclarations.ForEach((record) => record.ForwardDeclareType(this, headerEmitter));
                ForwardDeclareAnonProcedureTypes(this, headerEmitter);

                //emit function headers
                ForwardDeclareArrayTypes(headerEmitter);
                ForwardDeclareTupleTypes(headerEmitter);
                EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclare(this, headerEmitter));
                InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclare(this, headerEmitter));
                RecordDeclarations.ForEach((record) => record.ForwardDeclare(this, headerEmitter));
                ProcedureDeclarations.ForEach((procedure) => procedure.ForwardDeclareActual(this, headerEmitter));
                usedProcedureReferences.ForEach((procedure) => procedure.EmitCaptureContextCStruct(this, headerEmitter));

                if (headerEmitter != emitter)
                    headerEmitter.AppendLine("#endif");

                //emit utility functions
                MemoryAnalyzer.EmitAnalyzers(emitter);
                if (DoCallStack)
                    CallStackReporting.EmitReporter(emitter);
                if (!EliminateAsserts)
                    AssertStatement.EmitAsserter(emitter, DoCallStack);

                //emit function behavior
                EmitAnonProcedureCapturedContecies(emitter);
                EmitArrayTypeMarshallers(emitter, DoCallStack);
                EmitTupleTypeMarshallers(emitter);
                EmitAnonProcedureMovers(this, emitter);
                EnumDeclarations.ForEach((enumDecl) => enumDecl.Emit(this, emitter, new(), 0));
                InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.Emit(this, emitter, new(), 0));
                RecordDeclarations.ForEach((record) => record.Emit(this, emitter, new(), 0));
                ProcedureDeclarations.ForEach((proc) => proc.EmitActual(this, emitter, 0));
            }
        }
    }
}