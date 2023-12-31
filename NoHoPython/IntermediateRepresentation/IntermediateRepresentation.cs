using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Syntax
{
    public sealed partial class AstIRProgramBuilder
    {
        private List<EnumDeclaration> EnumDeclarations;
        private List<InterfaceDeclaration> InterfaceDeclarations;
        private List<RecordDeclaration> RecordDeclarations;
        private List<(ProcedureDeclaration, Statements.ProcedureDeclaration.Type)> ProcedureDeclarations;

        public Stack<ProcedureDeclaration> ScopedProcedures { get; private set; }
        public Stack<RefinementContext> Refinements { get; private set; }
        public RecordDeclaration? ScopedRecordDeclaration { get; private set; }
        public SymbolMarshaller SymbolMarshaller { get; private set; }
        
        public SymbolContainer CurrentMasterScope => ScopedProcedures.Count > 0 ? ScopedProcedures.Peek() : (ScopedRecordDeclaration != null) ? ScopedRecordDeclaration : SymbolMarshaller.CurrentModule;

        public readonly SortedSet<string> Flags;
        public bool VerboseOutput { get; private set; }

        private Dictionary<IType, HashSet<IType>> typeDependencyTree;

        public AstIRProgramBuilder(List<IAstStatement> statements, List<string> flags)
        {
            SymbolMarshaller = new SymbolMarshaller();
            EnumDeclarations = new List<EnumDeclaration>();
            InterfaceDeclarations = new List<InterfaceDeclaration>();
            RecordDeclarations = new List<RecordDeclaration>();
            ProcedureDeclarations = new();
            ScopedProcedures = new Stack<ProcedureDeclaration>();
            Refinements = new Stack<RefinementContext>();
            Flags = new SortedSet<string>(flags);
            ScopedRecordDeclaration = null;
            VerboseOutput = flags.Contains("-verbose");

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
        public void AddProcDeclaration(ProcedureDeclaration procedureDeclaration, Statements.ProcedureDeclaration.Type type) => ProcedureDeclarations.Add((procedureDeclaration, type));

        public void DeclareTypeDependencies(IType type, params IType[] dependencies)
        {
            Debug.Assert(!typeDependencyTree.ContainsKey(type));

            HashSet<IType> dependencySet = new(new ITypeComparer());
            foreach (IType dependency in dependencies)
                if (dependency.IsTypeDependency)
                    dependencySet.Add(dependency);

            typeDependencyTree.Add(type, dependencySet);
        }

        public void PrintVerbose(string message)
        {
            if (VerboseOutput)
                Console.WriteLine(message);
        }

        public IRProgram ToIRProgram(bool doBoundsChecking, bool eliminateAsserts, bool doCallStack, bool nameRuntimeTypes, bool emitLineDirectives, bool mainEntryPoint, MemoryAnalyzer memoryAnalyzer)
        {
            PrintVerbose("Scoping compile heads...");
            List<ProcedureDeclaration> compileHeads = new();
            foreach (ProcedureDeclaration procedureDeclaration in ProcedureDeclarations.ConvertAll(e => e.Item1))
                if (mainEntryPoint)
                {
                    if(procedureDeclaration.IsCompileHead && procedureDeclaration.Name == "main")
                        compileHeads.Add(procedureDeclaration);
                }
                else if (procedureDeclaration.IsCompileHead)
                    compileHeads.Add(procedureDeclaration);

            foreach (ProcedureDeclaration procedureDeclaration in compileHeads)
                procedureDeclaration.ScopeAsCompileHead(this);
            ScopeForAllSecondaryProcedures();

            PrintVerbose("Analyzing mutability and side effects...");
            foreach (var toAnalyze in ProcedureDeclarations)
            {
                switch (toAnalyze.Item2)
                {
                    case Statements.ProcedureDeclaration.Type.MessageReceiver:
                        toAnalyze.Item1.NonConstructorPropertyAnalysis();
                        break;
                    case Statements.ProcedureDeclaration.Type.Normal:
                        toAnalyze.Item1.NonMessageReceiverAnalysis();
                        break;
                }
            }

            return new(doBoundsChecking, eliminateAsserts, doCallStack, nameRuntimeTypes, emitLineDirectives,
                RecordDeclarations, InterfaceDeclarations, EnumDeclarations, foreignTypeOverloads.Keys.ToList(), ProcedureDeclarations.ConvertAll((p) => p.Item1), new List<string>()
                {
                    "<stdio.h>",
                    "<stdlib.h>",
                    "<string.h>",
                    "<math.h>"
                },
                usedArrayTypes.ToList(), usedTupleTypes.ToList(), bufferTypes.ToList(), usedReferenceTypes.ToList(), usedProcedureTypes.ToList(), usedProcedureReferences, procedureOverloads, enumTypeOverloads, interfaceTypeOverloads, recordTypeOverloads, foreignTypeOverloads, typeDependencyTree, memoryAnalyzer);
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
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval);
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval);

        public IType Type { get; }
        public bool IsTruey { get; }
        public bool IsFalsey { get; }

        //equivalent value but with type parameter references replaced
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);

        //emit corresponding C code
        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval);
    }

    public partial interface IRStatement : IRElement
    {
        //emit corresponding C code
        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs);
    }

    public sealed partial class IRProgram
    {
        public readonly List<RecordDeclaration> RecordDeclarations;
        public readonly List<InterfaceDeclaration> InterfaceDeclarations;
        public readonly List<EnumDeclaration> EnumDeclarations;
        public readonly List<ForeignCDeclaration> ForeignCDeclarations;
        public readonly List<ProcedureDeclaration> ProcedureDeclarations;

        public readonly Dictionary<string, string[]?> IncludedCFiles;

        public bool DoBoundsChecking { get; private set; }
        public bool EliminateAsserts { get; private set; }
        public bool DoCallStack { get; private set; }
        public bool NameRuntimeTypes { get; private set; }
        public bool EmitLineDirectives { get;private set; } 

        private Dictionary<IType, HashSet<IType>> typeDependencyTree;
        private HashSet<IType> compiledTypes;

        public MemoryAnalyzer MemoryAnalyzer { get; private set; }

        public IRProgram(bool doBoundsChecking, bool eliminateAsserts, bool doCallStack, bool nameRuntimeTypes, bool emitLineDirectives, List<RecordDeclaration> recordDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<EnumDeclaration> enumDeclarations, List<ForeignCDeclaration> foreignCDeclarations, List<ProcedureDeclaration> procedureDeclarations, List<string> includedCFiles, List<ArrayType> usedArrayTypes, List<TupleType> usedTupleTypes, List<IType> bufferTypes, List<ReferenceType> usedReferenceTypes, List<ProcedureType> usedProcedureTypes, List<ProcedureReference> usedProcedureReferences, Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads, Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads, Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverload, Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads, Dictionary<ForeignCDeclaration, List<ForeignCType>> foreignTypeOverloads, Dictionary<IType, HashSet<IType>> typeDependencyTree, MemoryAnalyzer memoryAnalyzer)
        {
            DoBoundsChecking = doBoundsChecking;
            EliminateAsserts = eliminateAsserts;
            DoCallStack = doCallStack;
            NameRuntimeTypes = nameRuntimeTypes;
            EmitLineDirectives = emitLineDirectives;

            RecordDeclarations = recordDeclarations;
            InterfaceDeclarations = interfaceDeclarations;
            EnumDeclarations = enumDeclarations;
            ProcedureDeclarations = procedureDeclarations;
            ForeignCDeclarations = foreignCDeclarations;
            IncludedCFiles = new(includedCFiles.Count);

            this.usedProcedureTypes = usedProcedureTypes;
            this.usedArrayTypes = usedArrayTypes;
            this.usedTupleTypes = usedTupleTypes;
            this.bufferTypes = bufferTypes;
            this.usedReferenceTypes = usedReferenceTypes;
            this.usedProcedureReferences = usedProcedureReferences;
            ProcedureOverloads = procedureOverloads;
            EnumTypeOverloads = enumTypeOverloads;
            InterfaceTypeOverloads = interfaceTypeOverload;
            RecordTypeOverloads = recordTypeOverloads;
            ForeignTypeOverloads = foreignTypeOverloads;

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
            foreach (string includedFile in includedCFiles)
                IncludedCFiles.Add(includedFile, null);
        }

        public void IncludeCFile((string, string[]?) toInclude)
        {
            if (!IncludedCFiles.ContainsKey(toInclude.Item1))
                IncludedCFiles.Add(toInclude.Item1, toInclude.Item2);
        }

        public bool DeclareCompiledType(Emitter emitter, IType type)
        {
            if (!type.IsTypeDependency)
                return true;

            if (!compiledTypes.Add(type))
                return false;
            CompileUncompiledDependencies(emitter, type);
            return true;
        }

        public void CompileUncompiledDependencies(Emitter emitter, IType type)
        {
            if (!type.IsTypeDependency)
                return;
            if (!typeDependencyTree.ContainsKey(type))
                return;

            foreach (IType dependency in typeDependencyTree[type])
                if (!compiledTypes.Contains(dependency))
                    dependency.EmitCStruct(this, emitter);
        }

        public void Emit(string outputFile, string? headerFile)
        {
            using(Emitter emitter = new(outputFile, EmitLineDirectives))
            using(Emitter headerEmitter = (headerFile == null) ? emitter : new Emitter(headerFile, false))
            {
                if (headerEmitter != emitter)
                {
                    headerEmitter.AppendLine("#ifndef _NHP_HEADER_GUARD_");
                    headerEmitter.AppendLine("#define _NHP_HEADER_GUARD_");
                }
                foreach (var includedCFile in IncludedCFiles)
                {
                    if (includedCFile.Value != null)
                        foreach (string preincludeDefinition in includedCFile.Value)
                            emitter.AppendLine($"#define {preincludeDefinition}");

                    if (includedCFile.Key.StartsWith('<'))
                        emitter.AppendLine($"#include {includedCFile.Key}");
                    else
                        emitter.AppendLine($"#include \"{includedCFile.Key}\"");
                }
                emitter.AppendLine();

                //emit typedefs
                this.compiledTypes.Clear();
                EmitArrayTypeTypedefs(headerEmitter);
                EmitTupleTypeTypedefs(headerEmitter);
                ForwardDeclareEnumTypes(headerEmitter);
                ForwardDeclareInterfaceTypes(headerEmitter);
                EmitAnonProcedureTypedefs(headerEmitter);
                ForwardDeclareRecordTypes(headerEmitter);
                ForwardDeclareForeignTypes(headerEmitter);

                //emit c structs
                MemoryAnalyzer.EmitAnalyzers(emitter);
                RecordDeclaration.EmitRecordMaskProto(this, headerEmitter);
                EmitArrayTypeCStructs(headerEmitter);
                EmitTupleCStructs(headerEmitter);
                EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclareType(this, headerEmitter));
                InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclareType(this, headerEmitter));
                RecordDeclarations.ForEach((record) => record.ForwardDeclareType(this, headerEmitter));
                ForeignCDeclarations.ForEach((foreign) => foreign.ForwardDeclareType(this, headerEmitter));

                //emit function headers
                ForwardDeclareArrayTypes(headerEmitter);
                ForwardDeclareTupleTypes(headerEmitter);
                EnumDeclarations.ForEach((enumDecl) => enumDecl.ForwardDeclare(this, headerEmitter));
                InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.ForwardDeclare(this, headerEmitter));
                RecordDeclarations.ForEach((record) => record.ForwardDeclare(this, headerEmitter));
                ForeignCDeclarations.ForEach((foreign) => foreign.ForwardDeclare(this, headerEmitter));
                usedProcedureReferences.ForEach((procedure) => procedure.EmitCaptureContextCStruct(this, headerEmitter, usedProcedureReferences.FindAll((procedureReference) => procedureReference.IsAnonymous)));
                ProcedureDeclarations.ForEach((procedure) => procedure.ForwardDeclareActual(this, headerEmitter));

                if (headerEmitter != emitter)
                    headerEmitter.AppendLine("#endif");

                //emit utility functions
                ProcedureType.EmitStandardAnonymizer(this, emitter);
                if (DoCallStack)
                    CallStackReporting.EmitReporter(emitter);
                if (!EliminateAsserts)
                    AssertStatement.EmitAsserter(emitter, DoCallStack);

                //emit function behavior
                EmitAnonProcedureCapturedContecies(emitter);
                EmitBufferCopiers(emitter);
                EmitArrayTypeMarshallers(emitter);
                EmitTupleTypeMarshallers(emitter);
                EnumDeclarations.ForEach((enumDecl) => enumDecl.Emit(this, emitter, new()));
                InterfaceDeclarations.ForEach((interfaceDecl) => interfaceDecl.Emit(this, emitter, new()));
                RecordDeclarations.ForEach((record) => record.Emit(this, emitter, new()));
                ForeignCDeclarations.ForEach((foreign) => foreign.Emit(this, emitter, new()));
                ProcedureDeclarations.ForEach((proc) => proc.EmitActual(this, emitter));
            }
        }
    }
}