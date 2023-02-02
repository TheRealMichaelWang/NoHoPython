using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private List<Tuple<ProcedureType, string>> uniqueProcedureTypes = new();
        private List<ProcedureReference> usedProcedureReferences = new();
        private Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads = new();

        public void ScopeAnonProcedure(ProcedureType procedureType)
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
                if (IRProgram.ProcedureTypeCCompatible(procedureType, uniqueProcedure.Item1))
                    return;

            string name = $"_nhp_anonProcTypeNo{uniqueProcedureTypes.Count}";
            uniqueProcedureTypes.Add(new(procedureType, name));

            List<IType> dependencies = new() { procedureType.ReturnType };
            typeDependencyTree.Add(procedureType, new HashSet<IType>(dependencies.Where((type) => type is not RecordType && type is not ProcedureType), new ITypeComparer()));
        }

        public bool DeclareUsedProcedureReference(ProcedureReference procedureReference)
        {
            foreach (ProcedureReference usedReference in usedProcedureReferences)
                if (usedReference.IsCompatibleWith(procedureReference))
                    return false;

            usedProcedureReferences.Add(procedureReference);
            if (!procedureOverloads.ContainsKey(procedureReference.ProcedureDeclaration))
                procedureOverloads.Add(procedureReference.ProcedureDeclaration, new List<ProcedureReference>());
            procedureOverloads[procedureReference.ProcedureDeclaration].Add(procedureReference);

            return true;
        }

        public void ScopeForAllSecondaryProcedures()
        {
            for (int i = 0; i < usedProcedureReferences.Count; i++)
                usedProcedureReferences[i].ScopeForUsedTypes(this);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public static bool ProcedureTypeCCompatible(ProcedureType a, ProcedureType b)
        {
            static bool isCCompatible(IType a, IType b)
            {
                if (a.IsCompatibleWith(b))
                    return true;

                return (a is RecordType || a is ProcedureType || a is HandleType) && (b is RecordType || b is ProcedureType || b is HandleType);
            }

            if (a.ParameterTypes.Count != b.ParameterTypes.Count)
                return false;
            if (!isCCompatible(a.ReturnType, b.ReturnType))
                return false;
            for (int i = 0; i < a.ParameterTypes.Count; i++)
                if (!isCCompatible(a.ParameterTypes[i], b.ParameterTypes[i]))
                    return false;
            return true;
        }

        private List<Tuple<ProcedureType, string>> uniqueProcedureTypes;
        private List<ProcedureReference> usedProcedureReferences;
        public readonly Dictionary<ProcedureDeclaration, List<ProcedureReference>> ProcedureOverloads;

        public void EmitAnonProcedureTypedefs(StatementEmitter emitter)
        {
            static void EmitProcTypedef(IRProgram irProgram, StatementEmitter emitter, Tuple<ProcedureType, string> procedureInfo)
            {
                static void emitType(IRProgram irProgram, StatementEmitter emitter, IType type)
                {
                    if (type is RecordType ||
                       type is ProcedureType ||
                       type is HandleType)
                        emitter.Append("void*");
                    else if (type is TypeParameterReference)
                        throw new InvalidOperationException();
                    else
                        emitter.Append(type.GetCName(irProgram));
                }

                emitter.Append("typedef ");
                emitType(irProgram, emitter, procedureInfo.Item1.ReturnType);
                emitter.Append(" (*");
                emitter.Append(procedureInfo.Item2);
                emitter.Append("_t)(void* _nhp_capture");

                for (int i = 0; i < procedureInfo.Item1.ParameterTypes.Count; i++)
                {
                    emitter.Append(", ");
                    emitType(irProgram, emitter, procedureInfo.Item1.ParameterTypes[i]);
                    emitter.Append($" param{i}");
                }
                emitter.AppendLine(");");

                emitter.AppendLine($"typedef struct {procedureInfo.Item2}_info {procedureInfo.Item2}_info_t;");

                emitter.AppendLine($"typedef void (*{procedureInfo.Item2}_destructor_t)({procedureInfo.Item1.GetCName(irProgram)} to_free);");
                emitter.AppendLine($"typedef {procedureInfo.Item1.GetCName(irProgram)} (*{procedureInfo.Item2}_copier_t)({procedureInfo.Item1.GetCName(irProgram)} to_copy, void* responsible_destroyer);");
                emitter.AppendLine($"typedef {procedureInfo.Item1.GetCName(irProgram)} (*{procedureInfo.Item2}_record_copier_t)({procedureInfo.Item1.GetCName(irProgram)} to_copy, void* record);");

                emitter.AppendLine($"struct {procedureInfo.Item2}_info {{");
                emitter.AppendLine($"\t{procedureInfo.Item2}_t _nhp_this_anon;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_destructor_t _nhp_destructor;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_copier_t _nhp_copier;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_record_copier_t _nhp_record_copier;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_record_copier_t _nhp_resp_mutator;");
                emitter.AppendLine("};");
            }

            foreach (var uniqueProc in uniqueProcedureTypes)
            {
                EmitProcTypedef(this, emitter, uniqueProc);
                emitter.AppendLine();
            }
        }

        public void ForwardDeclareAnonProcedureTypes(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            foreach (var uniqueProcedure in uniqueProcedureTypes)
            {
                emitter.AppendLine($"{uniqueProcedure.Item1.GetCName(this)} move{uniqueProcedure.Item2}({uniqueProcedure.Item1.GetCName(this)}* dest, {uniqueProcedure.Item1.GetCName(this)} src, void* child_agent);");
            }
        }

        public void EmitAnonProcedureMovers(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            foreach (var uniqueProcedure in uniqueProcedureTypes)
            {
                emitter.AppendLine($"{uniqueProcedure.Item1.GetCName(this)} move{uniqueProcedure.Item2}({uniqueProcedure.Item1.GetCName(this)}* dest, {uniqueProcedure.Item1.GetCName(this)} src, void* child_agent) {{");
                emitter.Append('\t');
                uniqueProcedure.Item1.EmitFreeValue(this, emitter, "*dest", "child_agent");
                emitter.AppendLine();
                emitter.AppendLine("\t*dest = src;");
                emitter.AppendLine("\treturn src;");
                emitter.AppendLine("}");
            }
        }

        public string GetAnonProcedureStandardIdentifier(ProcedureType procedureType)
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
                if (ProcedureTypeCCompatible(procedureType, uniqueProcedure.Item1))
                    return uniqueProcedure.Item2;
            throw new InvalidOperationException();
        }

        public void EmitAnonProcedureCapturedContecies(StatementEmitter emitter)
        {
            foreach (ProcedureReference procedureReference in usedProcedureReferences)
            {
                procedureReference.EmitAnonDestructor(this, emitter);
                procedureReference.EmitAnonCopier(this, emitter);
                procedureReference.EmitAnonRecordCopier(this, emitter);
                procedureReference.EmitResponsibleDestroyerMutator(this, emitter);
                procedureReference.EmitAnonymizer(this, emitter);
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public static void EmitMemorySafe(IRValue value, IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeArgs)
        {
            if (value.RequiresDisposal(typeArgs))
                throw new CannotEmitDestructorError(value);
            value.Emit(irProgram, emitter, typeArgs, "NULL");
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ProcedureType
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => true;

        public string GetStandardIdentifier(IRProgram irProgram) => irProgram.GetAnonProcedureStandardIdentifier(this);

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_info_t*";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) => emitter.Append($"({valueCSource})->_nhp_destructor({valueCSource});"); 
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => emitter.Append($"({valueCSource})->_nhp_copier({valueCSource}, {responsibleDestroyer})");
        
        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (irProgram.EmitExpressionStatements)
                IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
            else
                emitter.Append($"move{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource}, {childAgent})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string recordCSource) => emitter.Append($"({valueCSource})->_nhp_record_copier({valueCSource}, {recordCSource})");
        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, IEmitter emitter, string valueCSource, string newResponsibleDestroyer) => emitter.Append($"({valueCSource})->_nhp_resp_mutator({valueCSource}, {newResponsibleDestroyer})");

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            irBuilder.ScopeAnonProcedure(this);
            foreach (IType type in ParameterTypes)
                type.ScopeForUsedTypes(irBuilder);
            ReturnType.ScopeForUsedTypes(irBuilder);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class ProcedureDeclaration
    {
        public void ScopeAsCompileHead(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (!IsCompileHead)
                throw new InvalidOperationException();

            irBuilder.DeclareUsedProcedureReference(new ProcedureReference(this, false, ErrorReportedElement));
        }

        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        private bool scoping = false;
        public void SubstitutedScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (scoping)
                return;

            scoping = true;
#pragma warning disable CS8602 // Parameters not null when compilation begins
            foreach (Variable variable in Parameters)
                variable.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            foreach (Variable capturedItem in CapturedVariables)
                capturedItem.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
#pragma warning restore CS8602
            base.ScopeForUsedTypes(typeargs, irBuilder);
            scoping = false; 
        }

        public void ForwardDeclareActual(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.ProcedureOverloads.ContainsKey(this))
                return;

            foreach (ProcedureReference procedureReference in irProgram.ProcedureOverloads[this])
                procedureReference.ForwardDeclare(irProgram, emitter);
        }

        public override void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent) { }

        public void EmitActual(IRProgram irProgram, StatementEmitter emitter, int indent)
        {
            if (!irProgram.ProcedureOverloads.ContainsKey(this))
                return;

            foreach (ProcedureReference procedureReference in irProgram.ProcedureOverloads[this])
            {
                emitter.LastSourceLocation = ErrorReportedElement.SourceLocation;
                Dictionary<TypeParameter, IType> typeargs = procedureReference.Emit(irProgram, emitter);
                EmitInitialize(irProgram, emitter, typeargs, indent);
                EmitNoOpen(irProgram, emitter, typeargs, indent, false);
            }
        }
    }

    partial class ProcedureReference
    {
        public string GetStandardIdentifier(IRProgram irProgram) => $"{IScopeSymbol.GetAbsolouteName(ProcedureDeclaration, ProcedureDeclaration.LastMasterScope is IScopeSymbol parentSymbol ? parentSymbol : null)}{string.Join(string.Empty, ProcedureDeclaration.UsedTypeParameters.ToList().ConvertAll((typeParam) => $"_{typeArguments[typeParam].GetStandardIdentifier(irProgram)}_as_{typeParam.Name}"))}" + (IsAnonymous ? "_anon_capture" : string.Empty);

        public string GetClosureCaptureCType(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_captured_t";

        private ProcedureType anonProcedureType;

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            ReturnType.ScopeForUsedTypes(irBuilder);
            foreach (IType type in ParameterTypes)
                type.ScopeForUsedTypes(irBuilder);
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                variable.Type.SubstituteWithTypearg(typeArguments).ScopeForUsedTypes(irBuilder);
            ProcedureDeclaration.SubstitutedScopeForUsedTypes(typeArguments, irBuilder);
            if (IsAnonymous)
                anonProcedureType.ScopeForUsedTypes(irBuilder);

            irBuilder.DeclareUsedProcedureReference(this);
        }

        private void EmitCFunctionHeader(IRProgram irProgram, StatementEmitter emitter)
        {
#pragma warning disable CS8604 // Parameters not null when compilation begins
            emitter.Append($"{ReturnType.GetCName(irProgram)} {GetStandardIdentifier(irProgram)}(");
            if (IsAnonymous)
            {
                //if (ProcedureDeclaration.CapturedVariables.Count > 0)
                emitter.Append($"{GetClosureCaptureCType(irProgram)}* _nhp_captured");
#pragma warning disable CS8602 // Parameters initialized by compilation
                ProcedureDeclaration.Parameters.ForEach((parameter) =>
                {
                    emitter.Append(", ");
                    emitter.Append($"{parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {parameter.GetStandardIdentifier()}");
                });
                emitter.Append(") ");
#pragma warning restore CS8602 
            }
            else
            {
                if (ProcedureDeclaration.CapturedVariables.Count > 0)
                    emitter.Append(string.Join(", ", (ProcedureDeclaration.Parameters.Concat(ProcedureDeclaration.CapturedVariables)).Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) + " " + parameter.GetStandardIdentifier())));
                else
                    emitter.Append(string.Join(", ", ProcedureDeclaration.Parameters.Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) + " " + parameter.GetStandardIdentifier())));
                emitter.Append(')');
            }
#pragma warning restore CS8604 
        }

        public void EmitCaptureCFunctionHeader(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{anonProcedureType.GetCName(irProgram)} capture_{GetStandardIdentifier(irProgram)}(");
            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
                emitter.Append($"{capturedVariable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {capturedVariable.GetStandardIdentifier()}, ");
            emitter.Append("void* responsible_destroyer)");
        }

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
        {
            if(IsAnonymous)
                emitter.AppendLine($"typedef struct {GetStandardIdentifier(irProgram)}_captured {GetClosureCaptureCType(irProgram)};");
            EmitCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(";");
            if (IsAnonymous)
            {
                EmitCaptureCFunctionHeader(irProgram, emitter);
                emitter.AppendLine(";");
            }
        }

        public Dictionary<TypeParameter, IType> Emit(IRProgram irProgram, StatementEmitter emitter)
        {
            EmitCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(" {");
            if (IsAnonymous)
            {
                foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                    emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {variable.GetStandardIdentifier()} = _nhp_captured->{variable.GetStandardIdentifier()};");
            }
            if(ReturnType is not NothingType)
                emitter.AppendLine($"\t{ReturnType.GetCName(irProgram)} _nhp_toret;");
            return typeArguments;
        }

        public void EmitCaptureContextCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)}_captured {{");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_t _nhp_this_anon;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_destructor_t _nhp_destructor;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_copier_t _nhp_copier;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_record_copier_t _nhp_record_copier;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_record_copier_t _nhp_resp_mutator;"); 
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {variable.GetStandardIdentifier()};");
            emitter.AppendLine("\tint _nhp_lock;");
            emitter.AppendLine("\tvoid* _nhp_child_agent;");
            emitter.AppendLine("};");
        }

        public void EmitAnonymizer(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!IsAnonymous)
                return;

            EmitCaptureCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(" {");

            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* closure = {irProgram.MemoryAnalyzer.Allocate($"sizeof({GetStandardIdentifier(irProgram)}_captured_t)")};");
            emitter.AppendLine($"\tclosure->_nhp_this_anon = ({anonProcedureType.GetStandardIdentifier(irProgram)}_t)(&{GetStandardIdentifier(irProgram)});");
            emitter.AppendLine($"\tclosure->_nhp_destructor = ({anonProcedureType.GetStandardIdentifier(irProgram)}_destructor_t)(&free{GetStandardIdentifier(irProgram)});");
            emitter.AppendLine($"\tclosure->_nhp_copier = ({anonProcedureType.GetStandardIdentifier(irProgram)}_copier_t)(&copy{GetStandardIdentifier(irProgram)});");
            emitter.AppendLine($"\tclosure->_nhp_record_copier = ({anonProcedureType.GetStandardIdentifier(irProgram)}_record_copier_t)(&record_copy{GetStandardIdentifier(irProgram)});");
            emitter.AppendLine($"\tclosure->_nhp_resp_mutator = ({anonProcedureType.GetStandardIdentifier(irProgram)}_record_copier_t)(&change_resp_owner{GetStandardIdentifier(irProgram)});");
            emitter.AppendLine("\tclosure->_nhp_lock = 0;");
            emitter.AppendLine("\tclosure->_nhp_child_agent = responsible_destroyer;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier()} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(irProgram, emitter, capturedVariable.GetStandardIdentifier(), "responsible_destroyer");
                emitter.AppendLine(";");
            }
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName(irProgram)})closure;");
            emitter.AppendLine("}");
        }

        public void EmitAnonDestructor(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"void free{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_free_anon) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_free = ({GetClosureCaptureCType(irProgram)}*)to_free_anon;");

            emitter.AppendLine("\tif(to_free->_nhp_lock)");
            emitter.AppendLine("\t\treturn;");
            emitter.AppendLine("\tto_free->_nhp_lock = 1;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables) 
                if (capturedVariable.Type.SubstituteWithTypearg(typeArguments).RequiresDisposal)
                {
                    emitter.Append('\t');
                    capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitFreeValue(irProgram, emitter, $"to_free->{capturedVariable.GetStandardIdentifier()}", "to_free->_nhp_child_agent");
                    emitter.AppendLine();
                }

            emitter.AppendLine($"\t{irProgram.MemoryAnalyzer.Dealloc("to_free", $"sizeof({GetStandardIdentifier(irProgram)}_captured_t)")};");
            emitter.AppendLine("}");
        }

        public void EmitAnonCopier(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName(irProgram)} copy{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_copy_anon, void* responsible_destroyer) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_copy = ({GetClosureCaptureCType(irProgram)}*)to_copy_anon;");
            emitter.Append($"\treturn capture_{GetStandardIdentifier(irProgram)}(");
            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
                emitter.Append($"to_copy->{capturedVariable.GetStandardIdentifier()}, ");
            emitter.AppendLine("responsible_destroyer);");
            emitter.AppendLine("}");
        }

        public void EmitAnonRecordCopier(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName(irProgram)} record_copy{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_copy_anon, {RecordType.StandardRecordMask} record) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_copy = ({GetClosureCaptureCType(irProgram)}*)to_copy_anon;");
            emitter.Append($"\treturn capture_{GetStandardIdentifier(irProgram)}(");
            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append(capturedVariable.IsRecordSelf ? $"(({capturedVariable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)})record)" : $"to_copy->{capturedVariable.GetStandardIdentifier()}");
                emitter.Append(", ");
            }
            emitter.AppendLine("record);");
            emitter.AppendLine("}");
        }

        public void EmitResponsibleDestroyerMutator(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName(irProgram)} change_resp_owner{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_mutate, void* responsible_destroyer) {{");

            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* closure = ({GetClosureCaptureCType(irProgram)}*)to_mutate;");

            emitter.AppendLine("\tif(closure->_nhp_lock)");
            emitter.AppendLine($"\t\treturn ({anonProcedureType.GetCName(irProgram)})closure;");
            emitter.AppendLine("\tclosure->_nhp_lock = 1;");
            emitter.AppendLine("\tclosure->_nhp_child_agent = responsible_destroyer;");

            foreach(Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier()} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitMutateResponsibleDestroyer(irProgram, emitter, $"closure->{capturedVariable.GetStandardIdentifier()}", "responsible_destroyer");
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\tclosure->_nhp_lock = 0;");
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName(irProgram)})closure;");
            emitter.AppendLine("}");
        }
    }

    partial class ReturnStatement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ToReturn.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ToReturn.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            Variable? localToReturn = null;
            if (ToReturn.Type is not NothingType)
            {
                CodeBlock.CIndent(emitter, indent);
                emitter.Append("_nhp_toret = ");

                if (ToReturn is VariableReference variableReference && parentProcedure.IsLocalVariable(variableReference.Variable))
                {
                    localToReturn = variableReference.Variable;
                    emitter.Append(localToReturn.GetStandardIdentifier());
                }
                else if (ToReturn.RequiresDisposal(typeargs))
                    ToReturn.Emit(irProgram, emitter, typeargs, "NULL");
                else
                    ToReturn.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(ToReturn, irProgram, typeargs, "NULL"), "NULL");

                emitter.AppendLine(";");
            }

            foreach (Variable variable in activeVariables)
                if (localToReturn != variable && variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    CodeBlock.CIndent(emitter, indent);
                    variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, variable.GetStandardIdentifier(), "NULL");
                    emitter.AppendLine();
                }

            CodeBlock.CIndent(emitter, indent);
            if (ToReturn.Type is not NothingType)
                emitter.AppendLine("return _nhp_toret;");
            else
                emitter.AppendLine("return;");
        }
    }

    partial class AbortStatement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (AbortMessage != null)
            {
                AbortMessage.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
                AbortMessage.ScopeForUsedTypes(typeargs, irBuilder);
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.AppendLine("{");

            if (irProgram.DoCallStack)
            {
                CallStackReporting.EmitErrorLoc(emitter, ErrorReportedElement, indent + 1);
                CallStackReporting.EmitPrintStackTrace(emitter, indent + 1);
            }

            CodeBlock.CIndent(emitter, indent + 1);
            if (AbortMessage != null)
            {
                emitter.Append($"{AbortMessage.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_abort_msg = ");
                AbortMessage.Emit(irProgram, emitter, typeargs, "NULL");
                emitter.AppendLine(";");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.AppendLine("printf(\"AbortError: %.*s\\n\", _nhp_abort_msg.length, _nhp_abort_msg.buffer);");

                CodeBlock.CIndent(emitter, indent + 1);
                AbortMessage.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, "_nhp_abort_msg", "NULL");
                emitter.AppendLine();
            }
            else
                emitter.Append("puts(\"AbortError: No message given.\");");

            CodeBlock.CIndent(emitter, indent + 1);
            emitter.AppendLine("abort();");

            CodeBlock.CIndent(emitter, indent);
            emitter.AppendLine("}");
        }
    }

    partial class ForeignCProcedureDeclaration
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent) { }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ProcedureCall
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Arguments.ForEach((arg) => arg.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public abstract void EmitCall(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> bufferedArguments, int currentNestedCall, string responsibleDestroyer);

        private void EmitCallWithResponsibleDestroyer(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> bufferedArguments, int currentNestedCall, string responsibleDestroyer)
        {
            if (assignResponsibleDestroyer && responsibleDestroyer != "NULL")
            {
                BufferedEmitter valueBuilder = new();
                EmitCall(irProgram, valueBuilder, typeargs, bufferedArguments, currentNestedCall, responsibleDestroyer);
                Type.SubstituteWithTypearg(typeargs).EmitMutateResponsibleDestroyer(irProgram, emitter, valueBuilder.ToString(), responsibleDestroyer);
            }
            else
                EmitCall(irProgram, emitter, typeargs, bufferedArguments, currentNestedCall, responsibleDestroyer);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            irProgram.ExpressionDepth++;
            if (irProgram.DoCallStack)
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotPerformCallStackReporting(this);
                emitter.Append("({");
                CallStackReporting.EmitReportCall(emitter, ErrorReportedElement);
                emitter.Append($"{Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_callrep_res{irProgram.ExpressionDepth} = ");
            }

            if ((irProgram.EmitExpressionStatements && Arguments.Any((arg) => arg.RequiresDisposal(typeargs))) 
                || !IRValue.EvaluationOrderGuarenteed(Arguments))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEnsureOrderOfEvaluation(this);

                emitter.Append("({");
                SortedSet<int> bufferedArguments = new();
                for (int i = 0; i < Arguments.Count; i++)
                    if (Arguments[i].RequiresDisposal(typeargs) || !Arguments[i].IsConstant || !Arguments[i].IsPure)
                    {
                        emitter.Append($"{Arguments[i].Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_argbuf_{i}{irProgram.ExpressionDepth} = ");
                        Arguments[i].Emit(irProgram, emitter, typeargs, "NULL");
                        emitter.Append(";");
                        bufferedArguments.Add(i);
                    }
                emitter.Append($"{Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_res{irProgram.ExpressionDepth} = ");
                EmitCallWithResponsibleDestroyer(irProgram, emitter, typeargs, bufferedArguments, irProgram.ExpressionDepth, responsibleDestroyer);
                emitter.Append(';');

                foreach (int i in bufferedArguments)
                    if (Arguments[i].RequiresDisposal(typeargs))
                        Arguments[i].Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"_nhp_argbuf_{i}{irProgram.ExpressionDepth}", "NULL");
                emitter.Append($"_nhp_res{irProgram.ExpressionDepth};}})");
            }
            else
                EmitCallWithResponsibleDestroyer(irProgram, emitter, typeargs, new SortedSet<int>(), irProgram.ExpressionDepth, responsibleDestroyer);

            if (irProgram.DoCallStack)
            {
                emitter.Append(';');
                CallStackReporting.EmitReportReturn(emitter);
                emitter.Append($"_nhp_callrep_res{irProgram.ExpressionDepth};}})");
            }

            irProgram.ExpressionDepth--;
        }

        protected void EmitArguments(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> bufferedArguments, int currentNestedCall)
        {
            bool cannotGuarenteeEvaluationOrder = !IRValue.EvaluationOrderGuarenteed(Arguments);

            for (int i = 0; i < Arguments.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");

                if (bufferedArguments.Contains(i))
                    emitter.Append($"_nhp_argbuf_{i}{currentNestedCall}");
                else
                {
                    if (Arguments[i].RequiresDisposal(typeargs))
                        throw new CannotEmitDestructorError(Arguments[i]);
                    else if (cannotGuarenteeEvaluationOrder && (!Arguments[i].IsPure || !Arguments[i].IsConstant))
                        throw new CannotEnsureOrderOfEvaluation(this);
                    else
                        Arguments[i].Emit(irProgram, emitter, typeargs, "NULL");
                }
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            void emitAndDestroyCall(SortedSet<int> bufferedArguments, int indent)
            {
                if (RequiresDisposal(typeargs))
                {
                    emitter.AppendLine("{");
                    CodeBlock.CIndent(emitter, indent + 1);
                    emitter.Append($"{Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_callrep_res0 = ");
                    EmitCall(irProgram, emitter, typeargs, bufferedArguments, 0, "NULL");
                    emitter.AppendLine(";");
                    CodeBlock.CIndent(emitter, indent + 1);
                    Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, "_nhp_callrep_res0", "NULL");
                    emitter.AppendLine();
                    CodeBlock.CIndent(emitter, indent);
                    emitter.AppendLine("}");
                }
                else
                {
                    EmitCall(irProgram, emitter, typeargs, bufferedArguments, 0, "NULL");
                    emitter.AppendLine(";");
                }
            }

            if(irProgram.DoCallStack)
                CallStackReporting.EmitReportCall(emitter, ErrorReportedElement, indent);

            CodeBlock.CIndent(emitter, indent);

            if (!Arguments.TrueForAll((arg) => !arg.RequiresDisposal(typeargs)) || !IRValue.EvaluationOrderGuarenteed(Arguments))
            {
                SortedSet<int> bufferedArguments = new();
                emitter.AppendLine("{");
                for (int i = 0; i < Arguments.Count; i++)
                    if (Arguments[i].RequiresDisposal(typeargs) || !Arguments[i].IsConstant || !Arguments[i].IsPure)
                    {
                        CodeBlock.CIndent(emitter, indent + 1);
                        emitter.Append($"{Arguments[i].Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_argbuf_{i}0 = ");
                        Arguments[i].Emit(irProgram, emitter, typeargs, "NULL");
                        emitter.AppendLine(";");
                        bufferedArguments.Add(i);
                    }

                CodeBlock.CIndent(emitter, indent + 1);
                emitAndDestroyCall(bufferedArguments, indent + 1);

                foreach (int i in bufferedArguments)
                    if (Arguments[i].RequiresDisposal(typeargs))
                    {
                        CodeBlock.CIndent(emitter, indent + 1);
                        Arguments[i].Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"_nhp_argbuf_{i}0", "NULL");
                        emitter.AppendLine();
                    }
                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine("}");
            }
            else
                emitAndDestroyCall(new SortedSet<int>(), indent);

            if(irProgram.DoCallStack)
                CallStackReporting.EmitReportReturn(emitter, indent);
        }
    }

    partial class LinkedProcedureCall
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> bufferedArguments, int currentNestedCall, string responsibleDestroyer)
        {
            emitter.Append($"{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            EmitArguments(irProgram, emitter, typeargs, bufferedArguments, currentNestedCall);
            for(int i = 0; i < Procedure.ProcedureDeclaration.CapturedVariables.Count; i++)
            {
                if (i > 0 || Arguments.Count > 0)
                    emitter.Append(", ");
                VariableReference variableReference = new(Procedure.ProcedureDeclaration.CapturedVariables[i], true, ErrorReportedElement);
                variableReference.Emit(irProgram, emitter, typeargs, "NULL");
            }
            emitter.Append(')');
        }
    }

    partial class AnonymousProcedureCall
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ProcedureValue.ScopeForUsedTypes(typeargs, irBuilder);
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> bufferedArguments, int currentNestedCall, string responsibleDestroyer)
        {
            if (!ProcedureValue.IsPure)
                throw new CannotEnsureOrderOfEvaluation(this);

            IRValue.EmitMemorySafe(ProcedureValue, irProgram, emitter, typeargs);
            emitter.Append("->_nhp_this_anon(");
            IRValue.EmitMemorySafe(ProcedureValue.GetPostEvalPure(), irProgram, emitter, typeargs);
            if(Arguments.Count > 0)
                emitter.Append(", ");
            EmitArguments(irProgram, emitter, typeargs, bufferedArguments, currentNestedCall);
            emitter.Append(')');
        }
    }

    partial class AnonymizeProcedure
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            emitter.Append($"capture_{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}({string.Join("", Procedure.SubstituteWithTypearg(typeargs).ProcedureDeclaration.CapturedVariables.ConvertAll((capturedVar) => $"{((capturedVar.IsRecordSelf && parentProcedure == null) ? "_nhp_self" : capturedVar.GetStandardIdentifier())}, "))}{responsibleDestroyer})");
        }
    }

    partial class ForeignFunctionCall
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> bufferedArguments, int currentNestedCall, string responsibleDestroyer)
        {
            emitter.Append($"{ForeignCProcedure.Name}(");
            EmitArguments(irProgram, emitter, typeargs, bufferedArguments, currentNestedCall);
            emitter.Append(')');
        }
    }
}