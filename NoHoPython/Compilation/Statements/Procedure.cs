using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<ProcedureType> usedProcedureTypes = new(new ITypeComparer());
        private List<ProcedureReference> usedProcedureReferences = new();
        private Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads = new();

        public void ScopeAnonProcedure(ProcedureType procedureType)
        {
            if (usedProcedureTypes.Contains(procedureType))
                return;

            usedProcedureTypes.Add(procedureType);

            List<IType> dependencies = new(procedureType.ParameterTypes.Count + 1) { procedureType.ReturnType };
            dependencies.AddRange(procedureType.ParameterTypes);

            DeclareTypeDependencies(procedureType, dependencies.ToArray());
        }

        public ProcedureReference? DeclareUsedProcedureReference(ProcedureReference procedureReference)
        {
            //foreach (ProcedureReference usedReference in usedProcedureReferences)
            //    if (usedReference.IsCompatibleWith(procedureReference))
            //        return usedReference;

            if (!procedureOverloads.ContainsKey(procedureReference.ProcedureDeclaration))
            {
                procedureOverloads.Add(procedureReference.ProcedureDeclaration, new List<ProcedureReference>());
                usedProcedureReferences.Add(procedureReference);
            }
            else
            {
                foreach (ProcedureReference overload in procedureOverloads[procedureReference.ProcedureDeclaration])
                    if (overload.IsCompatibleWith(procedureReference))
                        return overload;
            }
            procedureOverloads[procedureReference.ProcedureDeclaration].Add(procedureReference);

            return procedureReference;
        }

        public void ScopeForAllSecondaryProcedures()
        {
            for (int i = 0; i < usedProcedureReferences.Count; i++)
                usedProcedureReferences[i] = usedProcedureReferences[i].ScopeForUsedTypes(this);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<ProcedureType> usedProcedureTypes;
        private HashSet<string> compiledProcedureTypes = new();
        private List<ProcedureReference> usedProcedureReferences;
        public readonly Dictionary<ProcedureDeclaration, List<ProcedureReference>> ProcedureOverloads;

        public void EmitAnonProcedureTypedefs(Emitter emitter)
        {
            void EmitProcTypedef(ProcedureType procedureInfo)
            {
                void emitType(IType type)
                {
                    if (type.GetCName(this).EndsWith('*'))
                        emitter.Append("void*");
                    else if (type is TypeParameterReference typeParameterReference)
                        throw new UnexpectedTypeParameterError(typeParameterReference.TypeParameter, null);
                    else
                        emitter.Append(type.GetCName(this));
                }

                string standardId = procedureInfo.GetStandardIdentifier(this);
                if (compiledProcedureTypes.Contains(standardId))
                    return;
                compiledProcedureTypes.Add(standardId);

                emitter.Append("typedef ");
                emitType(procedureInfo.ReturnType);
                emitter.Append(" (*");
                emitter.Append(standardId);
                emitter.Append("_t)(void* nhp_capture");

                for (int i = 0; i < procedureInfo.ParameterTypes.Count; i++)
                {
                    emitter.Append(", ");
                    emitType(procedureInfo.ParameterTypes[i]);
                    emitter.Append($" param{i}");
                }

                if (procedureInfo.ReturnType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* ret_responsible_dest");

                emitter.AppendLine(");");
            }

            emitter.AppendLine("typedef struct nhp_anon_proc_info nhp_anon_proc_info_t;");
            emitter.AppendLine("typedef void (*nhp_anon_proc_destructor)(void* to_free, void* child_agent);");
            emitter.AppendLine("typedef nhp_anon_proc_info_t* (*nhp_anon_proc_copier)(void* to_copy, void* responsible_destroyer);");

            emitter.AppendLine("struct nhp_anon_proc_info {");
            emitter.AppendLine("\tvoid* nhp_this_anon;");
            emitter.AppendLine("\tnhp_anon_proc_destructor nhp_destructor;");
            emitter.AppendLine("\tnhp_anon_proc_copier nhp_copier;");
            emitter.AppendLine("\tnhp_anon_proc_copier nhp_record_copier;");
            emitter.AppendLine("};");

            foreach (var uniqueProc in usedProcedureTypes)
                EmitProcTypedef(uniqueProc);
        }

        public void EmitAnonProcedureCapturedContecies(Emitter emitter)
        {
            foreach (ProcedureReference procedureReference in usedProcedureReferences)
            {
                procedureReference.EmitAnonDestructor(this, emitter);
                procedureReference.EmitAnonCopier(this, emitter);
                procedureReference.EmitAnonRecordCopier(this, emitter);
                procedureReference.EmitAnonymizer(this, emitter);
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public static void EmitDirect(IRProgram irProgram, Emitter emitter, IRValue value, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            Debug.Assert(!value.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval));
            value.Emit(irProgram, emitter, typeargs, (setPromise) => setPromise(emitter), responsibleDestroyer, isTemporaryEval);
        }

        public static Emitter.Promise EmitDirectPromise(IRProgram irProgram, IRValue value, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => (emitter) => EmitDirect(irProgram, emitter, value, typeargs, responsibleDestroyer, isTemporaryEval);

        public static void EmitAsStatement(IRProgram irProgram, Emitter emitter, IRValue value, Dictionary<TypeParameter, IType> typeargs)
        {
            if (value.IsPure)
                return;

            if (value.RequiresDisposal(irProgram, typeargs, true))
                value.Emit(irProgram, emitter, typeargs, (valuePromise) => {
                    value.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, valuePromise, Emitter.NullPromise);
                    emitter.AppendLine();
                }, Emitter.NullPromise, true);
            else
            {
                value.Emit(irProgram, emitter, typeargs, (valuePromise) =>
                {
                    valuePromise(emitter);
                    emitter.AppendLine(';');
                }, Emitter.NullPromise, true);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ProcedureType
    {
        public static string StandardProcedureType => "nhp_anon_proc_info_t*";

        public static void EmitStandardAnonymizer(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine("static nhp_anon_proc_info_t* anon_proc_empty_copier(void* to_copy, void* responsible_destroyer);");
            emitter.AppendLine("static nhp_anon_proc_info_t* capture_anon_proc(void* nhp_this_anon) {");
            emitter.AppendLine($"\tnhp_anon_proc_info_t* closure = {irProgram.MemoryAnalyzer.Allocate("sizeof(nhp_anon_proc_info_t)")};");
            emitter.AppendLine("\tclosure->nhp_this_anon = nhp_this_anon;");
            emitter.AppendLine("\tclosure->nhp_destructor = NULL;");
            emitter.AppendLine("\tclosure->nhp_copier = anon_proc_empty_copier;");
            emitter.AppendLine("\tclosure->nhp_record_copier = anon_proc_empty_copier;");
            emitter.AppendLine("\treturn closure;");
            emitter.AppendLine("}");

            emitter.AppendLine("static nhp_anon_proc_info_t* anon_proc_empty_copier(void* to_copy, void* responsible_destroyer) {");
            emitter.AppendLine("\treturn capture_anon_proc(((nhp_anon_proc_info_t*)to_copy)->nhp_this_anon);");
            emitter.AppendLine("}");
            emitter.AppendLine("static nhp_anon_proc_info_t* anon_proc_copy(nhp_anon_proc_info_t* to_copy, void* responsible_destroyer) {");
            emitter.AppendLine("\treturn to_copy->nhp_copier(to_copy, responsible_destroyer);");
            emitter.AppendLine("}");
            emitter.AppendLine("static nhp_anon_proc_info_t* anon_proc_reccopy(nhp_anon_proc_info_t* to_copy, void* responsible_destroyer) {");
            emitter.AppendLine("\treturn to_copy->nhp_record_copier(to_copy, responsible_destroyer);");
            emitter.AppendLine("}");

            emitter.AppendLine("static void anon_proc_destroy(nhp_anon_proc_info_t* to_free, void* child_agent) {");
            emitter.AppendLine("\tif(to_free->nhp_destructor) { to_free->nhp_destructor(to_free, child_agent); }");
            emitter.AppendLine($"\telse {{ {irProgram.MemoryAnalyzer.Dealloc($"to_free", "sizeof(nhp_anon_proc_info_t)")} }}");
            emitter.AppendLine("}");
        }

        public bool IsNativeCType => false;
        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => true;
        public bool IsTypeDependency => true;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => false;

        public string GetStandardIdentifier(IRProgram irProgram) //=> irProgram.GetAnonProcedureStandardIdentifier(this);
        {
            string GetArgStandardIdentifier(IType type)
            {
                if (type.GetCName(irProgram).EndsWith('*'))
                    return "ptr";
                else
                    return type.GetStandardIdentifier(irProgram);
            }

            StringBuilder builder = new();
            builder.Append("nhp_anon_proc_");
            builder.Append(GetArgStandardIdentifier(ReturnType));
            foreach(IType paramType in ParameterTypes)
            {
                builder.Append('_');
                builder.Append(GetArgStandardIdentifier(paramType));
            }
            return builder.ToString();
        }

        public string GetCName(IRProgram irProgram) => StandardProcedureType;

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent) 
        {
            emitter.Append("anon_proc_destroy(");
            valuePromise(emitter);
            emitter.Append(", ");
            childAgent(emitter);
            emitter.AppendLine(");");
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement) 
        {
            emitter.Append($"({GetCName(irProgram)})anon_proc_copy(");
            valueCSource(emitter);
            emitter.Append(", ");
            responsibleDestroyer(emitter);
            emitter.Append(')');
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer, null);

        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)
        {
            emitter.Append($"({GetCName(irProgram)})anon_proc_reccopy(");
            valueCSource(emitter);
            emitter.Append(", ");
            responsibleDestroyer(emitter);
            emitter.Append(')');
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter) { }

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

            ProcedureReference procedureReference = new ProcedureReference(this, false, ErrorReportedElement);
            procedureReference.ScopeForUsedTypes(irBuilder);
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

        public void ForwardDeclareActual(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.ProcedureOverloads.ContainsKey(this))
                return;

            foreach (ProcedureReference procedureReference in irProgram.ProcedureOverloads[this])
                procedureReference.ForwardDeclare(irProgram, emitter);
        }

        public override void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) { }

        public void EmitActual(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.ProcedureOverloads.ContainsKey(this))
                return;

            foreach (ProcedureReference procedureReference in irProgram.ProcedureOverloads[this])
            {
                emitter.LastSourceLocation = ErrorReportedElement.SourceLocation;
                Dictionary<TypeParameter, IType> typeargs = procedureReference.Emit(irProgram, emitter);
                EmitInitialize(irProgram, emitter, typeargs);
                EmitNoOpen(irProgram, emitter, typeargs, false);
                emitter.EndFunctionBlock();
            }
        }
    }

    partial class ProcedureReference
    {
        public string GetStandardIdentifier(IRProgram irProgram) => $"{IScopeSymbol.GetAbsolouteName(ProcedureDeclaration, ProcedureDeclaration.LastMasterScope is IScopeSymbol parentSymbol ? parentSymbol : null)}{string.Join(string.Empty, ProcedureDeclaration.UsedTypeParameters.ToList().ConvertAll((typeParam) => $"_{typeArguments[typeParam].GetStandardIdentifier(irProgram)}_as_{typeParam.Name}"))}" + (IsAnonymous ? "_anon_capture" : string.Empty);

        public string GetClosureCaptureCType(IRProgram irProgram) => ProcedureDeclaration.CapturedVariables.Count == 0 ? "nhp_anon_proc_info_t" : (complementaryProcedureReference == null ? $"{GetStandardIdentifier(irProgram)}_captured_t" : complementaryProcedureReference.GetClosureCaptureCType(irProgram));

        public string GetComplementaryIdentifier(IRProgram iRProgram) => complementaryProcedureReference == null ? GetStandardIdentifier(iRProgram) : complementaryProcedureReference.GetStandardIdentifier(iRProgram);

        private ProcedureType anonProcedureType;
        private ProcedureReference? complementaryProcedureReference = null;
        private bool emittedCapturedContextStruct = false;

        public ProcedureReference ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            ReturnType.ScopeForUsedTypes(irBuilder);
            foreach (IType type in ParameterTypes)
                type.ScopeForUsedTypes(irBuilder);
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                variable.Type.SubstituteWithTypearg(typeArguments).ScopeForUsedTypes(irBuilder);
            ProcedureDeclaration.SubstitutedScopeForUsedTypes(typeArguments, irBuilder);
            if (IsAnonymous)
                anonProcedureType.ScopeForUsedTypes(irBuilder);

            ProcedureReference? procedureReference = irBuilder.DeclareUsedProcedureReference(this);
            return procedureReference != null ? procedureReference : this;
        }

        private void EmitCFunctionHeader(IRProgram irProgram, Emitter emitter)
        {
#pragma warning disable CS8604 // Parameters not null when compilation begins
            emitter.Append($"{ReturnType.GetCName(irProgram)} {GetStandardIdentifier(irProgram)}(");
            if (IsAnonymous)
            {
                //if (ProcedureDeclaration.CapturedVariables.Count > 0)
                emitter.Append($"{GetClosureCaptureCType(irProgram)}* nhp_captured");
#pragma warning disable CS8602 // Parameters initialized by compilation
                ProcedureDeclaration.Parameters.ForEach((parameter) =>
                {
                    emitter.Append(", ");
                    emitter.Append($"{parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {parameter.GetStandardIdentifier()}");
                });
#pragma warning restore CS8602 
                if(ReturnType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* ret_responsible_dest");
            }
            else
            {
                if (ProcedureDeclaration.CapturedVariables.Count > 0)
                    emitter.Append(string.Join(", ", (ProcedureDeclaration.Parameters.Concat(ProcedureDeclaration.CapturedVariables)).Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) + " " + parameter.GetStandardIdentifier())));
                else
                    emitter.Append(string.Join(", ", ProcedureDeclaration.Parameters.Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) + " " + parameter.GetStandardIdentifier())));

                if(ReturnType.MustSetResponsibleDestroyer)
                {
                    if (ProcedureDeclaration.Parameters.Count + ProcedureDeclaration.CapturedVariables.Count > 0)
                        emitter.Append(", ");
                    emitter.Append("void* ret_responsible_dest");
                }
            }
#pragma warning restore CS8604
            emitter.Append(')');
        }

        public void EmitCaptureCFunctionHeader(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{ProcedureType.StandardProcedureType} capture_{GetStandardIdentifier(irProgram)}(");
            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
                emitter.Append($"{capturedVariable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {capturedVariable.GetStandardIdentifier()}, ");
            emitter.Append("void* responsible_destroyer, void* fn_ptr)");
        }

        public void ForwardDeclare(IRProgram irProgram, Emitter emitter)
        {
            EmitCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(";");

            if (IsAnonymous && ProcedureDeclaration.CapturedVariables.Count > 0 && complementaryProcedureReference == null)
            {
                EmitCaptureCFunctionHeader(irProgram, emitter);
                emitter.AppendLine(";");
            }
        }

        public Dictionary<TypeParameter, IType> Emit(IRProgram irProgram, Emitter emitter)
        {
            EmitCFunctionHeader(irProgram, emitter);
            emitter.DeclareFunctionBlock();
            emitter.AppendStartBlock();
            if (IsAnonymous)
            {
                foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                    emitter.AppendLine($"{variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {variable.GetStandardIdentifier()} = nhp_captured->{variable.GetStandardIdentifier()};");
            }
            if(ReturnType is not NothingType)
                emitter.AppendLine($"{ReturnType.GetCName(irProgram)} nhp_toret;");
            return typeArguments;
        }

        public void EmitCaptureContextCStruct(IRProgram irProgram, Emitter emitter, List<ProcedureReference> anonProcedureReferences)
        {
            bool HasCompatibleCaptureContextCStruct(ProcedureReference procedureReference)
            {
                if (!procedureReference.emittedCapturedContextStruct)
                    return false;

                foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                    if (!procedureReference.ProcedureDeclaration.CapturedVariables.Any((otherCapturedVariable) => variable.GetStandardIdentifier() == otherCapturedVariable.GetStandardIdentifier() && variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) == otherCapturedVariable.Type.SubstituteWithTypearg(procedureReference.typeArguments).GetCName(irProgram) && variable.IsRecordSelf == variable.IsRecordSelf))
                        return false;

                return ProcedureDeclaration.CapturedVariables.Count == procedureReference.ProcedureDeclaration.CapturedVariables.Count;
            }

            if (!IsAnonymous || ProcedureDeclaration.CapturedVariables.Count == 0)
                return;

            complementaryProcedureReference = anonProcedureReferences.Find((procedureReference) => HasCompatibleCaptureContextCStruct(procedureReference));

            if (complementaryProcedureReference == null)
            {
                emitter.AppendLine($"typedef struct {GetStandardIdentifier(irProgram)}_captured {{");
                emitter.AppendLine("\tnhp_anon_proc_info_t common;");
                foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                    emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {variable.GetStandardIdentifier()};");
                emitter.AppendLine("\tint nhp_lock;");
                emitter.AppendLine($"}} {GetStandardIdentifier(irProgram)}_captured_t;");
                emittedCapturedContextStruct = true;
            }
        }

        public void EmitAnonymizer(IRProgram irProgram, Emitter emitter)
        {
            if (!IsAnonymous || ProcedureDeclaration.CapturedVariables.Count == 0 || complementaryProcedureReference != null)
                return;

            EmitCaptureCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(" {");

            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* closure = {irProgram.MemoryAnalyzer.Allocate($"sizeof({GetClosureCaptureCType(irProgram)})")};");

            emitter.AppendLine($"\tclosure->common.nhp_this_anon = fn_ptr;");

            emitter.Append("\tclosure->common.nhp_destructor = ");
            if (ProcedureDeclaration.CapturedVariables.Any((variable) => variable.Type.RequiresDisposal))
                emitter.AppendLine($"(nhp_anon_proc_destructor)(&free{GetComplementaryIdentifier(irProgram)});");
            else
                emitter.AppendLine("NULL;");

            emitter.AppendLine($"\tclosure->common.nhp_copier = (nhp_anon_proc_copier)(&copy{GetComplementaryIdentifier(irProgram)});");

            emitter.Append("\tclosure->common.nhp_record_copier = ");
            if(ProcedureDeclaration.CapturedVariables.Any((variable) => variable.IsRecordSelf))
                emitter.AppendLine($"(nhp_anon_proc_copier)(&record_copy{GetStandardIdentifier(irProgram)});");
            else
                emitter.AppendLine("closure->common.nhp_copier;");

            emitter.AppendLine("\tclosure->nhp_lock = 0;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier()} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(irProgram, emitter, (e) => e.Append(capturedVariable.GetStandardIdentifier()), (e) => e.Append("responsible_destroyer"));
                emitter.AppendLine(";");
            }
            emitter.AppendLine($"\treturn ({ProcedureType.StandardProcedureType})closure;");
            emitter.AppendLine("}");
        }

        public void EmitAnonDestructor(IRProgram irProgram, Emitter emitter)
        {
            if (!IsAnonymous || !ProcedureDeclaration.CapturedVariables.Any((variable) => variable.Type.RequiresDisposal) || complementaryProcedureReference != null)
                return;

            emitter.AppendStartBlock($"void free{GetStandardIdentifier(irProgram)}({ProcedureType.StandardProcedureType} to_free_anon, void* child_agent)");
            emitter.AppendLine($"{GetClosureCaptureCType(irProgram)}* to_free = ({GetClosureCaptureCType(irProgram)}*)to_free_anon;");

            emitter.AppendStartBlock("if(to_free->nhp_lock)");
            emitter.AppendLine("return;");
            emitter.AppendEndBlock();
            emitter.AppendLine("to_free->nhp_lock = 1;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables) 
                if (capturedVariable.Type.SubstituteWithTypearg(typeArguments).RequiresDisposal)
                {
                    capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitFreeValue(irProgram, emitter, (e) => e.Append($"to_free->{capturedVariable.GetStandardIdentifier()}"), (e) => e.Append("child_agent"));
                    emitter.AppendLine();
                }

            emitter.AppendLine(irProgram.MemoryAnalyzer.Dealloc("to_free", $"sizeof({GetClosureCaptureCType(irProgram)})"));
            emitter.AppendEndBlock();
        }

        public void EmitAnonCopier(IRProgram irProgram, Emitter emitter)
        {
            if (!IsAnonymous || ProcedureDeclaration.CapturedVariables.Count == 0 || complementaryProcedureReference != null)
                return;

            emitter.AppendLine($"{ProcedureType.StandardProcedureType} copy{GetStandardIdentifier(irProgram)}({ProcedureType.StandardProcedureType} to_copy_anon, void* responsible_destroyer) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_copy = ({GetClosureCaptureCType(irProgram)}*)to_copy_anon;");
            emitter.Append($"\treturn capture_{GetStandardIdentifier(irProgram)}(");
            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
                emitter.Append($"to_copy->{capturedVariable.GetStandardIdentifier()}, ");
            emitter.AppendLine("responsible_destroyer, to_copy_anon->nhp_this_anon);");
            emitter.AppendLine("}");
        }

        public void EmitAnonRecordCopier(IRProgram irProgram, Emitter emitter)
        {
            if (!IsAnonymous || !ProcedureDeclaration.CapturedVariables.Any((variable) => variable.IsRecordSelf) || complementaryProcedureReference != null)
                return;

            emitter.AppendLine($"{ProcedureType.StandardProcedureType} record_copy{GetStandardIdentifier(irProgram)}({ProcedureType.StandardProcedureType} to_copy_anon, void* record) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_copy = ({GetClosureCaptureCType(irProgram)}*)to_copy_anon;");
            emitter.Append($"\treturn capture_{GetStandardIdentifier(irProgram)}(");
            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append(capturedVariable.IsRecordSelf ? $"(({capturedVariable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)})record)" : $"to_copy->{capturedVariable.GetStandardIdentifier()}");
                emitter.Append(", ");
            }
            emitter.AppendLine("record, to_copy_anon->nhp_this_anon);");
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            Variable? localToReturn = null;
            if (ToReturn.Type is not NothingType)
            {
                if (ToReturn is VariableReference variableReference && parentProcedure.IsLocalVariable(variableReference.Variable))
                {
                    localToReturn = variableReference.Variable;
                    primaryEmitter.ExemptResourceFromDestruction(localToReturn.ResourceDestructorId);
                    primaryEmitter.AppendLine($"nhp_toret = {localToReturn.GetStandardIdentifier()};");
                }
                else
                    ToReturn.Emit(irProgram, primaryEmitter, typeargs, (toReturnPromise) =>
                    {
                        primaryEmitter.Append("nhp_toret = ");
                        if (ToReturn.RequiresDisposal(irProgram, typeargs, false))
                            toReturnPromise(primaryEmitter);
                        else
                            ToReturn.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, toReturnPromise, (e) => e.Append("ret_responsible_dest"), ToReturn);
                        primaryEmitter.AppendLine(';');
                    }, (e) => e.Append("ret_responsible_dest"), false);
            }

            primaryEmitter.DestroyFunctionResources();
            if (ToReturn.Type is not NothingType)
                primaryEmitter.AppendLine("return nhp_toret;");
            else
                primaryEmitter.AppendLine("return;");
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (irProgram.DoCallStack)
            {
                CallStackReporting.EmitErrorLoc(primaryEmitter, ErrorReportedElement);
                CallStackReporting.EmitPrintStackTrace(primaryEmitter);
            }

            if (AbortMessage != null)
            {
                if (AbortMessage.Type is HandleType) {
                    AbortMessage.Emit(irProgram, primaryEmitter, typeargs, (messagePromise) =>
                    {
                        primaryEmitter.Append("printf(\"AbortError: %s\\n\", ");
                        messagePromise(primaryEmitter);
                        primaryEmitter.AppendLine(");");
                    }, Emitter.NullPromise, true);
                } 
                else
                {
                    primaryEmitter.AppendLine($"{AbortMessage.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} nhp_abort_msg;");
                    primaryEmitter.SetArgument(AbortMessage, $"nhp_abort_msg", irProgram, typeargs, true);

                    primaryEmitter.Append("printf(\"AbortError: ");
                    if (AbortMessage.Type is ArrayType)
                        primaryEmitter.AppendLine("%.*s\\n\", nhp_abort_msg.length, nhp_abort_msg.buffer);");
                    else if (AbortMessage.Type is RecordType)
                        primaryEmitter.AppendLine("%s\\n\", nhp_abort_msg->cstr);");
                    else
                        throw new UnexpectedTypeException(AbortMessage.Type, ErrorReportedElement);
                }
            }
            else
                primaryEmitter.Append("puts(\"AbortError: No message given.\");");

            primaryEmitter.DestroyFunctionResources();
            primaryEmitter.AppendLine("abort();");
        }
    }

    partial class ForeignCProcedureDeclaration
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) { }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ProcedureCall
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        protected virtual bool ArgumentEvalautionOrderGuarenteed() => IRValue.EvaluationOrderGuarenteed(Arguments.ToArray());

        protected virtual bool ShouldBufferArg(int i, IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs)
        {
            if (Arguments[i].MustUseDestinationPromise(irProgram, typeargs, true) || Arguments[i].RequiresDisposal(irProgram, typeargs, true))
                return true;

            if (Arguments[i].IsPure && Arguments[i].IsConstant)
                return false;

            for (int j = 0; j < Arguments.Count; j++)
                if (j != i && (Arguments[i].IsAffectedByEvaluation(Arguments[j]) || Arguments[j].IsAffectedByEvaluation(Arguments[i])))
                    return true;
            return false;
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Arguments.Any((arg) => arg.RequiresDisposal(irProgram, typeargs, true) || arg.MustUseDestinationPromise(irProgram, typeargs, true)) || !ArgumentEvalautionOrderGuarenteed() || irProgram.DoCallStack;

        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Arguments.ForEach((arg) => arg.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public abstract void EmitCall(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            bool shouldBufferArg(int i) => ShouldBufferArg(i, irProgram, typeargs);

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();

                if(irProgram.DoCallStack)
                    CallStackReporting.EmitReportCall(primaryEmitter, ErrorReportedElement);

                for (int i = 0; i < Arguments.Count; i++)
                    if (shouldBufferArg(i))
                        primaryEmitter.Append($"{Arguments[i].Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arg{i}{indirection}; ");
                primaryEmitter.AppendLine();

                List<Emitter.Promise> argPromises = new(Arguments.Count);
                for (int i = 0; i < Arguments.Count; i++)
                {
                    int j = i;
                    if (shouldBufferArg(i))
                    {
                        primaryEmitter.SetArgument(Arguments[i], $"arg{i}{indirection}", irProgram, typeargs, true);
                        argPromises.Add((emitter) => emitter.Append($"arg{j}{indirection}"));
                    }
                    else
                    {
                        Debug.Assert(!Arguments[i].MustUseDestinationPromise(irProgram, typeargs, true));
                        argPromises.Add((emitter) => Arguments[j].Emit(irProgram, emitter, typeargs, (argPromise) => argPromise(emitter), Emitter.NullPromise, true));
                    }
                }

                destination((emitter) => EmitCall(irProgram, emitter, typeargs, argPromises, responsibleDestroyer));

                if (irProgram.DoCallStack)
                    CallStackReporting.EmitReportReturn(primaryEmitter);
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    EmitCall(irProgram, emitter, typeargs, Arguments.ConvertAll<Emitter.Promise>((arg) => (argEmitter) => arg.Emit(irProgram, argEmitter, typeargs, (argPromise) => argPromise(argEmitter), Emitter.NullPromise, true)), responsibleDestroyer);
                });
        }

        protected static void EmitArguments(Emitter emitter, List<Emitter.Promise> argPromises)
        {
            for (int i = 0; i < argPromises.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");

                argPromises[i](emitter);
            }
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class LinkedProcedureCall
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer)
        {
            primaryEmitter.Append($"{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            EmitArguments(primaryEmitter, argPromises);
            for (int i = 0; i < Procedure.ProcedureDeclaration.CapturedVariables.Count; i++)
            {
                if (i > 0 || Arguments.Count > 0)
                    primaryEmitter.Append(", ");
                IRValue.EmitDirect(irProgram, primaryEmitter, new VariableReference(Procedure.ProcedureDeclaration.CapturedVariables[i], true, null, ErrorReportedElement), typeargs, Emitter.NullPromise, true);
            }
            if (Type.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
            {
                if (Arguments.Count + Procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
                    primaryEmitter.Append(", ");
                responsibleDestroyer(primaryEmitter);
            }
            primaryEmitter.Append(')');
        }
    }

    partial class AnonymousProcedureCall
    {
        protected override bool ArgumentEvalautionOrderGuarenteed()
        {
            List<IRValue> arguments = new List<IRValue>(Arguments);
            arguments.Insert(0, Arguments[0]);
            return IRValue.EvaluationOrderGuarenteed(arguments.ToArray());
        }

        public override void EmitCall(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer)
        {
            primaryEmitter.Append($"(({ProcedureType.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_t)");
            argPromises[0](primaryEmitter);
            primaryEmitter.Append("->nhp_this_anon)(");
            EmitArguments(primaryEmitter, argPromises);

            if (Type.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
            {
                primaryEmitter.Append(", ");
                responsibleDestroyer(primaryEmitter);
            }
            primaryEmitter.Append(')');
        }
    }

    partial class OptimizedRecordMessageCall
    {
        ProcedureReference? toCall = null;
        private bool scoped = false;

        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (scoped)
                return;
            scoped = true;

            Property.ScopeForUse(true, typeargs, irBuilder);

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            RecordType recordType = (RecordType)Record.Type.SubstituteWithTypearg(typeargs);
            toCall = recordType.RecordPrototype.GetMessageReceiver(Property.Name).Procedure.SubstituteWithTypearg(recordType.TypeargMap).SubstituteWithTypearg(typeargs).CreateRegularReference();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            toCall = toCall.ScopeForUsedTypes(irBuilder);
            if (toCall.ProcedureDeclaration.CapturedVariables.Count > 0)
                Arguments.Add(Record);

            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            primaryEmitter.Append($"{toCall.GetStandardIdentifier(irProgram)}(");
            EmitArguments(primaryEmitter, argPromises);

            if (toCall.ProcedureDeclaration.CapturedVariables.Count > 0)
                Debug.Assert(Arguments.Last() == Record);

            if (toCall.ReturnType.MustSetResponsibleDestroyer)
            {
                if (Arguments.Count > 0)
                    primaryEmitter.Append(", ");
                responsibleDestroyer(primaryEmitter);
            }

            primaryEmitter.Append(')');
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }

    partial class AnonymizeProcedure
    {
        ProcedureReference? toAnonymize = null;

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            toAnonymize = Procedure.SubstituteWithTypearg(typeargs);
            toAnonymize = toAnonymize.ScopeForUsedTypes(irBuilder);
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (GetFunctionHandle)
                emitter.Append($"&{toAnonymize.GetStandardIdentifier(irProgram)}");
            else if (Procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
            {
                emitter.Append($"capture_{toAnonymize.GetComplementaryIdentifier(irProgram)}(");
                foreach (Variable capturedVar in toAnonymize.ProcedureDeclaration.CapturedVariables)
                {
                    if (capturedVar.IsRecordSelf && parentProcedure == null)
                        emitter.Append("nhp_self");
                    else
                        emitter.Append(capturedVar.GetStandardIdentifier());
                    emitter.Append(", ");
                }
                responsibleDestroyer(emitter);
                emitter.Append($", &{toAnonymize.GetStandardIdentifier(irProgram)})");
            }
            else
                emitter.Append($"capture_anon_proc(&{Procedure.GetStandardIdentifier(irProgram)})");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        });

        public void EmitForPropertyGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise recordPromise, Emitter.Promise responsibleDestroyer)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Debug.Assert(!GetFunctionHandle);
            Debug.Assert(toAnonymize.ProcedureDeclaration.CapturedVariables.Count <= 1);

            if(toAnonymize.ProcedureDeclaration.CapturedVariables.Count == 1)
            {
                Debug.Assert(toAnonymize.ProcedureDeclaration.CapturedVariables[0].IsRecordSelf);
                emitter.Append($"capture_{toAnonymize.GetComplementaryIdentifier(irProgram)}(");
                recordPromise(emitter);
                emitter.Append(", ");
                responsibleDestroyer(emitter);
                emitter.Append($", &{Procedure.GetStandardIdentifier(irProgram)})");
            }
            else
                emitter.Append($"capture_anon_proc(&{Procedure.GetStandardIdentifier(irProgram)})");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }

    partial class ForeignFunctionCall
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => base.ScopeForUsedTypes(ProcedureReference.SubstituteTypeargsWithTypeargs(typeArguments, typeargs), irBuilder);

        public override void EmitCall(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer)
        {
            if(ForeignCProcedure.CFunctionName == null)
            {
                primaryEmitter.Append(ForeignCProcedure.Name);
                primaryEmitter.Append('(');
                EmitArguments(primaryEmitter, argPromises);
                primaryEmitter.Append(')');

                if (Type.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer && responsibleDestroyer != Emitter.NullPromise)
                    throw new CannotConfigureResponsibleDestroyerError(this, Type.SubstituteWithTypearg(typeargs));
            }
            else
                primaryEmitter.Append(GetSource(ForeignCProcedure.CFunctionName, irProgram, argPromises, responsibleDestroyer));
        }
    }
}