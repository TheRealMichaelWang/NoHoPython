using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
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
                if (procedureType.IsCompatibleWith(uniqueProcedure.Item1))
                    return;

            string name = $"_nhp_anonProcTypeNo{uniqueProcedureTypes.Count}";
            uniqueProcedureTypes.Add(new(procedureType, name));
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
        private List<Tuple<ProcedureType, string>> uniqueProcedureTypes;
        private List<ProcedureReference> usedProcedureReferences;
        public readonly Dictionary<ProcedureDeclaration, List<ProcedureReference>> ProcedureOverloads;

        public void EmitAnonProcedureTypedefs(StringBuilder emitter)
        {
            static void EmitProcTypedef(IRProgram irProgram, StringBuilder emitter, Tuple<ProcedureType, string> procedureInfo)
            {
                static void emitType(IRProgram irProgram, StringBuilder emitter, IType type)
                {
                    if (type is RecordType ||
                       type is EnumType ||
                       type is InterfaceType ||
                       type is ProcedureType)
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
                    emitter.Append(procedureInfo.Item1.ParameterTypes[i].GetCName(irProgram));
                    emitter.Append($" param{i}");
                }
                emitter.AppendLine(");");

                emitter.AppendLine($"typedef struct {procedureInfo.Item2}_info {procedureInfo.Item2}_info_t;");

                emitter.AppendLine($"typedef void (*{procedureInfo.Item2}_destructor_t)({procedureInfo.Item1.GetCName(irProgram)} to_free);");
                emitter.AppendLine($"typedef {procedureInfo.Item1.GetCName(irProgram)} (*{procedureInfo.Item2}_copier_t)({procedureInfo.Item1.GetCName(irProgram)} to_copy);");
                emitter.AppendLine($"typedef {procedureInfo.Item1.GetCName(irProgram)} (*{procedureInfo.Item2}_record_copier_t)({procedureInfo.Item1.GetCName(irProgram)} to_copy, void* record);");

                emitter.AppendLine($"struct {procedureInfo.Item2}_info {{");
                emitter.AppendLine($"\t{procedureInfo.Item2}_t _nhp_this_anon;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_destructor_t _nhp_destructor;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_copier_t _nhp_copier;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_record_copier_t _nhp_record_copier;");
                emitter.AppendLine("};");
            }

            foreach (var uniqueProc in uniqueProcedureTypes)
            {
                EmitProcTypedef(this, emitter, uniqueProc);
                emitter.AppendLine();
            }
        }

        public void ForwardDeclareAnonProcedureTypes(StringBuilder emitter)
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
            {
                emitter.AppendLine($"{uniqueProcedure.Item1.GetCName(this)} move{uniqueProcedure.Item2}({uniqueProcedure.Item1.GetCName(this)}* dest, {uniqueProcedure.Item1.GetCName(this)} src);");
            }
        }

        public void EmitAnonProcedureMovers(StringBuilder emitter)
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
            {
                emitter.AppendLine($"{uniqueProcedure.Item1.GetCName(this)} move{uniqueProcedure.Item2}({uniqueProcedure.Item1.GetCName(this)}* dest, {uniqueProcedure.Item1.GetCName(this)} src) {{");
                emitter.Append('\t');
                uniqueProcedure.Item1.EmitFreeValue(this, emitter, "*dest");
                emitter.AppendLine("\t*dest = src;");
                emitter.AppendLine("\treturn src;");
                emitter.AppendLine("}");
            }
        }

        public string GetAnonProcedureStandardIdentifier(ProcedureType procedureType)
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
                if (procedureType.IsCompatibleWith(uniqueProcedure.Item1))
                    return uniqueProcedure.Item2;
            throw new InvalidOperationException();
        }

        public void EmitAnonProcedureCapturedContecies(StringBuilder emitter)
        {
            foreach (ProcedureReference procedureReference in usedProcedureReferences)
            {
                procedureReference.EmitCaptureContextCStruct(this, emitter);
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
        public static void EmitMemorySafe(IRValue value, IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeArgs)
        {
            if (value.RequiresDisposal(typeArgs))
                throw new CannotEmitDestructorException(value);
            value.Emit(irProgram, emitter, typeArgs);
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ProcedureType
    {
        public bool RequiresDisposal => true;

        public string GetStandardIdentifier(IRProgram irProgram) => irProgram.GetAnonProcedureStandardIdentifier(this);

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_info_t*";

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.AppendLine($"({valueCSource})->_nhp_destructor({valueCSource});"); 
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.Append($"({valueCSource})->_nhp_copier({valueCSource})");
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => EmitCopyValue(irProgram, emitter, valueCSource);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => emitter.Append($"({valueCSource})->_nhp_record_copier({valueCSource}, {recordCSource})");

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

        public override void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.ProcedureOverloads.ContainsKey(this))
                return;

            foreach (ProcedureReference procedureReference in irProgram.ProcedureOverloads[this])
                procedureReference.ForwardDeclare(irProgram, emitter);
            base.ForwardDeclare(irProgram, emitter);
        }

        public override void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.ProcedureOverloads.ContainsKey(this))
                return;
            
            foreach (ProcedureReference procedureReference in irProgram.ProcedureOverloads[this])
                base.EmitNoOpen(irProgram, emitter, procedureReference.Emit(irProgram, emitter), indent);
        }
    }

    partial class ProcedureReference
    {
        public string GetStandardIdentifier(IRProgram irProgram) => $"{IScopeSymbol.GetAbsolouteName(ProcedureDeclaration)}{string.Join(string.Empty, typeArguments.Values.ToList().ConvertAll((typearg) => $"_{typearg.GetStandardIdentifier(irProgram)}"))}" + (IsAnonymous ? "_anon_capture" : string.Empty);

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

        private void EmitCFunctionHeader(IRProgram irProgram, StringBuilder emitter)
        {
#pragma warning disable CS8604 // Parameters not null when compilation begins
            emitter.Append($"{ReturnType.GetCName(irProgram)} {GetStandardIdentifier(irProgram)}(");
            if (IsAnonymous)
            {
                if (ProcedureDeclaration.CapturedVariables.Count > 0)
                    emitter.Append($"{GetClosureCaptureCType(irProgram)}* _nhp_captured");
#pragma warning disable CS8602 // Parameters initialized by compilation
                ProcedureDeclaration.Parameters.ForEach((parameter) =>
                {
                    emitter.Append(", ");
                    emitter.Append($"{parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {parameter.GetStandardIdentifier(irProgram)}");
                });
                emitter.Append(") ");
#pragma warning restore CS8602 
            }
            else
            {
                if (ProcedureDeclaration.CapturedVariables.Count > 0)
                    emitter.Append(string.Join(", ", (ProcedureDeclaration.Parameters.Concat(ProcedureDeclaration.CapturedVariables)).Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) + " " + parameter.GetStandardIdentifier(irProgram))));
                else
                    emitter.Append(string.Join(", ", ProcedureDeclaration.Parameters.Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram) + " " + parameter.GetStandardIdentifier(irProgram))));
                emitter.Append(")");
            }
#pragma warning restore CS8604 
        }

        public void EmitCaptureCFunctionHeader(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.Append($"{anonProcedureType.GetCName(irProgram)} capture_{GetStandardIdentifier(irProgram)}(");
            emitter.Append(string.Join(", ", ProcedureDeclaration.CapturedVariables.ConvertAll((capturedVar) => $"{capturedVar.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {capturedVar.GetStandardIdentifier(irProgram)}")));
            emitter.Append(")");
        }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
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

        public Dictionary<TypeParameter, IType> Emit(IRProgram irProgram, StringBuilder emitter)
        {
            EmitCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(" {");
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {variable.GetStandardIdentifier(irProgram)} = _nhp_captured->{variable.GetStandardIdentifier(irProgram)};");
            if(ReturnType is not NothingType)
                emitter.AppendLine($"\t{ReturnType} _nhp_toret;");
            return typeArguments;
        }

        public void EmitCaptureContextCStruct(IRProgram irProgram, StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)}_captured {{");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_t _nhp_this_anon;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_destructor_t _nhp_destructor;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_copier_t _nhp_copier;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier(irProgram)}_record_copier_t _nhp_record_copier;");
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)} {variable.GetStandardIdentifier(irProgram)};");
            emitter.AppendLine("\tint _nhp_freeing;");
            emitter.AppendLine("};");
        }

        public void EmitAnonymizer(IRProgram irProgram, StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            EmitCaptureCFunctionHeader(irProgram, emitter);
            emitter.AppendLine(" {");

            if(ProcedureDeclaration.CapturedVariables.Count > 0)
            {
                emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* closure = malloc(sizeof({GetStandardIdentifier(irProgram)}_captured_t));");
                emitter.AppendLine($"\tclosure->_nhp_this_anon = ({anonProcedureType.GetStandardIdentifier(irProgram)}_t)(&{GetStandardIdentifier(irProgram)});");
                emitter.AppendLine($"\tclosure->_nhp_destructor = ({anonProcedureType.GetStandardIdentifier(irProgram)}_destructor_t)(&free{GetStandardIdentifier(irProgram)});");
                emitter.AppendLine($"\tclosure->_nhp_copier = ({anonProcedureType.GetStandardIdentifier(irProgram)}_copier_t)(&copy{GetStandardIdentifier(irProgram)});");
                emitter.AppendLine($"\tclosure->_nhp_record_copier = ({anonProcedureType.GetStandardIdentifier(irProgram)}_record_copier_t)(&record_copy{GetStandardIdentifier(irProgram)});");
                emitter.AppendLine("\tclosure->_nhp_freeing = 0;");

                foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
                {
                    emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier(irProgram)} = ");
                    capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(irProgram, emitter, capturedVariable.GetStandardIdentifier(irProgram));
                    emitter.AppendLine(";");
                }
            }
            else
            {
                emitter.AppendLine($"\t{anonProcedureType.GetCName(irProgram)} closure = malloc(sizeof({anonProcedureType.GetStandardIdentifier(irProgram)}));");
                emitter.AppendLine($"\t*closure = ({anonProcedureType.GetStandardIdentifier(irProgram)})(&{GetStandardIdentifier(irProgram)});");
            }
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName(irProgram)})closure;");
            emitter.AppendLine("}");
        }

        public void EmitAnonDestructor(IRProgram irProgram, StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"void free{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_free_anon) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_free = ({GetClosureCaptureCType(irProgram)}*)to_free_anon;");

            emitter.AppendLine("\tif(to_free->_nhp_freeing)");
            emitter.AppendLine("\t\treturn;");
            emitter.AppendLine("\tto_free->_nhp_freeing = 1;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables) 
            {
                emitter.Append('\t');
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitFreeValue(irProgram, emitter, $"to_free->{capturedVariable.GetStandardIdentifier(irProgram)}");
            }
            
            emitter.AppendLine("\tfree(to_free);");
            emitter.AppendLine("}");
        }

        public void EmitAnonCopier(IRProgram irProgram, StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName(irProgram)} copy{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_copy_anon) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_copy = ({GetClosureCaptureCType(irProgram)}*)to_copy_anon;");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* closure = malloc(sizeof({GetStandardIdentifier(irProgram)}_captured_t));");
            emitter.AppendLine("\tclosure->_nhp_this_anon = to_copy->_nhp_this_anon;");
            emitter.AppendLine("\tclosure->_nhp_destructor = to_copy->_nhp_destructor;");
            emitter.AppendLine("\tclosure->_nhp_copier = to_copy->_nhp_copier;");
            emitter.AppendLine("\tclosure->_nhp_record_copier = to_copy->_nhp_record_copier;");
            emitter.AppendLine("\tclosure->_nhp_freeing = 0;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier(irProgram)} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(irProgram, emitter, $"to_copy->{capturedVariable.GetStandardIdentifier(irProgram)}");
                emitter.AppendLine(";");
            }
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName(irProgram)})closure;");
            emitter.AppendLine("}");
        }

        public void EmitAnonRecordCopier(IRProgram irProgram, StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName(irProgram)} record_copy{GetStandardIdentifier(irProgram)}({anonProcedureType.GetCName(irProgram)} to_copy_anon, void* record) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* to_copy = ({GetClosureCaptureCType(irProgram)}*)to_copy_anon;");
            emitter.AppendLine($"\t{GetClosureCaptureCType(irProgram)}* closure = malloc(sizeof({GetStandardIdentifier(irProgram)}_captured_t));");
            emitter.AppendLine("\tclosure->_nhp_this_anon = to_copy->_nhp_this_anon;");
            emitter.AppendLine("\tclosure->_nhp_destructor = to_copy->_nhp_destructor;");
            emitter.AppendLine("\tclosure->_nhp_copier = to_copy->_nhp_copier;");
            emitter.AppendLine("\tclosure->_nhp_record_copier = to_copy->_nhp_record_copier;");
            emitter.AppendLine("\tclosure->_nhp_freeing = 0;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier(irProgram)} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(irProgram, emitter, capturedVariable.IsRecordSelf ? $"({capturedVariable.Type.SubstituteWithTypearg(typeArguments).GetCName(irProgram)})record" : $"to_copy->{capturedVariable.GetStandardIdentifier(irProgram)}");
                emitter.AppendLine(";");
            }
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

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);

            if (ToReturn.Type is not NothingType)
            {
                emitter.Append("_nhp_toret = ");

                if (ToReturn.RequiresDisposal(typeargs))
                    ToReturn.Emit(irProgram, emitter, typeargs);
                else
                {
                    StringBuilder valueBuilder = new StringBuilder();
                    ToReturn.Emit(irProgram, valueBuilder, typeargs);
                    ToReturn.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString());
                }

                emitter.AppendLine(";");
            }

            List<Variable> toFree = parentCodeBlock.GetCurrentLocals();
            foreach (Variable variable in toFree)
                if (variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    CodeBlock.CIndent(emitter, indent);
                    variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, variable.GetStandardIdentifier(irProgram));
                }

            CodeBlock.CIndent(emitter, indent);
            if (ToReturn.Type is not NothingType)
                emitter.AppendLine("return _nhp_toret;");
            else
                emitter.Append("return;");
        }
    }

    partial class ForeignCProcedureDeclaration
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent) { }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class LinkedProcedureCall
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => Procedure.SubstituteWithTypearg(typeargs).ReturnType.RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            foreach (IRValue argument in Arguments)
                argument.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            for(int i = 0; i < Arguments.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");
                IRValue.EmitMemorySafe(Arguments[i], irProgram, emitter, typeargs);
            }
            for(int i = 0; i < Procedure.ProcedureDeclaration.CapturedVariables.Count; i++)
            {
                if (i > 0 || Arguments.Count > 0)
                    emitter.Append(", ");
                VariableReference variableReference = new VariableReference(Procedure.ProcedureDeclaration.CapturedVariables[i], ErrorReportedElement);
                variableReference.Emit(irProgram, emitter, typeargs);
            }
            emitter.Append(')');
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs);
            emitter.AppendLine(";");
        }
    }

    partial class AnonymousProcedureCall
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => ProcedureType.ReturnType.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ProcedureValue.ScopeForUsedTypes(typeargs, irBuilder);
            foreach (IRValue argument in Arguments)
                argument.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            IRValue.EmitMemorySafe(ProcedureValue, irProgram, emitter, typeargs);
            emitter.Append("->_nhp_this_anon(");
            IRValue.EmitMemorySafe(ProcedureValue, irProgram, emitter, typeargs);
            foreach (IRValue argument in Arguments)
            {
                emitter.Append(", ");
                IRValue.EmitMemorySafe(argument, irProgram, emitter, typeargs);
            }
            emitter.Append(')');
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);

            if (ProcedureType.ReturnType.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                StringBuilder valueEmitter = new StringBuilder();
                Emit(irProgram, valueEmitter, typeargs);
                ProcedureType.ReturnType.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, valueEmitter.ToString());
            }
            else
                Emit(irProgram, emitter, typeargs);

            emitter.AppendLine(";");
        }
    }

    partial class AnonymizeProcedure
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"capture_{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}({string.Join(", ", Procedure.SubstituteWithTypearg(typeargs).ProcedureDeclaration.CapturedVariables.ConvertAll((capturedVar) => (capturedVar.IsRecordSelf) ? "_nhp_self" :capturedVar.GetStandardIdentifier(irProgram)))})");
        }
    }
}