using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public static void EmitMemorySafe(IRValue value, StringBuilder emitter, Dictionary<TypeParameter, IType> typeArgs)
        {
            if (value.RequiresDisposal(typeArgs))
                throw new CannotEmitDestructorException(value);
            value.Emit(emitter, typeArgs);
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ProcedureType
    {
        public bool RequiresDisposal => true;

        private static List<Tuple<ProcedureType, string>> uniqueProcedureTypes = new();

        public static void EmitTypedefs(StringBuilder emitter)
        {
            static void EmitProcTypedef(StringBuilder emitter, Tuple<ProcedureType, string> procedureInfo)
            {
                static void emitType(StringBuilder emitter, IType type)
                {
                    if (type is RecordType ||
                       type is EnumType ||
                       type is InterfaceType ||
                       type is ProcedureType)
                        emitter.Append("void*");
                    else if (type is TypeParameterReference)
                        throw new InvalidOperationException();
                    else
                        emitter.Append(type.GetCName());
                }

                emitter.Append("typedef ");
                emitType(emitter, procedureInfo.Item1.ReturnType);
                emitter.Append(" (*");
                emitter.Append(procedureInfo.Item2);
                emitter.Append("_t)(void* _nhp_capture");

                for (int i = 0; i < procedureInfo.Item1.ParameterTypes.Count; i++)
                {
                    emitter.Append(", ");
                    emitter.Append(procedureInfo.Item1.ParameterTypes[i].GetCName());
                    emitter.Append($" param{i}");
                }
                emitter.AppendLine(");");

                emitter.AppendLine($"typedef struct {procedureInfo.Item2}_info {procedureInfo.Item2}_info_t;");

                emitter.AppendLine($"typedef void (*{procedureInfo.Item2}_destructor_t)({procedureInfo.Item1.GetCName()} to_free);");
                emitter.AppendLine($"typedef {procedureInfo.Item1.GetCName()} (*{procedureInfo.Item2}_copier_t)({procedureInfo.Item1.GetCName()} to_copy);");
                emitter.AppendLine($"typedef {procedureInfo.Item1.GetCName()} (*{procedureInfo.Item2}_record_copier_t)({procedureInfo.Item1.GetCName()} to_copy, void* record);");

                emitter.AppendLine($"struct {procedureInfo.Item2}_info {{");
                emitter.AppendLine($"\t{procedureInfo.Item2}_t _nhp_this_anon;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_destructor_t _nhp_destructor;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_copier_t _nhp_copier;");
                emitter.AppendLine($"\t{procedureInfo.Item2}_record_copier_t _nhp_record_copier;");
                emitter.AppendLine("};");
            }

            foreach (var uniqueProc in uniqueProcedureTypes)
            {
                EmitProcTypedef(emitter, uniqueProc);
                emitter.AppendLine();
            }
        }

        public static void ForwardDeclareProcedureTypes(StringBuilder emitter)
        {
            foreach(var uniqueProcedure in uniqueProcedureTypes)
            {
                emitter.AppendLine($"{uniqueProcedure.Item1.GetCName()} move{uniqueProcedure.Item2}({uniqueProcedure.Item1.GetCName()}* dest, {uniqueProcedure.Item1.GetCName()} src);");
            }
        }

        public static void EmitMovers(StringBuilder emitter)
        {
            foreach(var uniqueProcedure in uniqueProcedureTypes)
            {
                emitter.AppendLine($"{uniqueProcedure.Item1.GetCName()} move{uniqueProcedure.Item2}({uniqueProcedure.Item1.GetCName()}* dest, {uniqueProcedure.Item1.GetCName()} src) {{");
                emitter.Append('\t');
                uniqueProcedure.Item1.EmitFreeValue(emitter, "*dest");
                emitter.AppendLine("\t*dest = src;");
                emitter.AppendLine("\treturn src;");
                emitter.AppendLine("}");
            }
        }

        public string GetStandardIdentifier()
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
                if (IsCompatibleWith(uniqueProcedure.Item1))
                    return uniqueProcedure.Item2;

            string name = $"_nhp_anonProcTypeNo{uniqueProcedureTypes.Count}";
            uniqueProcedureTypes.Add(new(this, name));
            return name;
        }

        public string GetCName() => $"{GetStandardIdentifier()}_info_t*";

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => emitter.AppendLine($"({valueCSource})->_nhp_destructor({valueCSource});"); 
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append($"({valueCSource})->_nhp_copier({valueCSource})");
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move{GetStandardIdentifier()}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => EmitCopyValue(emitter, valueCSource);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => emitter.Append($"({valueCSource})->_nhp_record_copier({valueCSource}, {recordCSource})");

        public void ScopeForUsedTypes()
        {
            GetStandardIdentifier();
            foreach (IType type in ParameterTypes)
                type.ScopeForUsedTypes();
            ReturnType.ScopeForUsedTypes();
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class ProcedureDeclaration
    {
        private static List<ProcedureReference> usedProcedureReferences = new List<ProcedureReference>();
        private static Dictionary<ProcedureDeclaration, List<ProcedureReference>> procedureOverloads = new Dictionary<ProcedureDeclaration, List<ProcedureReference>>();

        public static bool DeclareUsedProcedureReference(ProcedureReference procedureReference)
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

        public static void ScopeForAllSecondaryProcedures()
        {
            for (int i = 0; i < usedProcedureReferences.Count; i++)
                usedProcedureReferences[i].ScopeForUsedTypes();
        }

        public static void EmitCapturedContecies(StringBuilder emitter)
        {
            foreach (ProcedureReference procedureReference in usedProcedureReferences)
            {
                procedureReference.EmitCaptureContextCStruct(emitter);
                procedureReference.EmitAnonDestructor(emitter);
                procedureReference.EmitAnonCopier(emitter);
                procedureReference.EmitAnonRecordCopier(emitter);
                procedureReference.EmitAnonymizer(emitter);
            }
        }

        public void ScopeAsCompileHead()
        {
            if (!IsCompileHead)
                throw new InvalidOperationException();

            DeclareUsedProcedureReference(new ProcedureReference(this, false, ErrorReportedElement));
        }

        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        private bool scoping = false;
        public void SubstitutedScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            if (scoping)
                return;

            scoping = true;
#pragma warning disable CS8602 // Parameters not null when compilation begins
            foreach (Variable variable in Parameters)
                variable.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            foreach (Variable capturedItem in CapturedVariables)
                capturedItem.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
#pragma warning restore CS8602
            base.ScopeForUsedTypes(typeargs);
            scoping = false;
        }

        public override void ForwardDeclare(StringBuilder emitter)
        {
            if (!procedureOverloads.ContainsKey(this))
                return;

            foreach (ProcedureReference procedureReference in procedureOverloads[this])
                procedureReference.ForwardDeclare(emitter);
            base.ForwardDeclare(emitter);
        }

        public override void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!procedureOverloads.ContainsKey(this))
                return;
            
            foreach (ProcedureReference procedureReference in procedureOverloads[this])
                base.EmitNoOpen(emitter, procedureReference.Emit(emitter), indent);
        }
    }

    partial class ProcedureReference
    {
        public string GetStandardIdentifier() => $"{IScopeSymbol.GetAbsolouteName(ProcedureDeclaration)}{string.Join(string.Empty, typeArguments.Values.ToList().ConvertAll((typearg) => $"_{typearg.GetStandardIdentifier()}"))}" + (IsAnonymous ? "_anon_capture" : string.Empty);

        public string GetClosureCaptureCType() => $"{GetStandardIdentifier()}_captured_t";

        private ProcedureType anonProcedureType;

        public void ScopeForUsedTypes()
        {
            ReturnType.ScopeForUsedTypes();
            foreach (IType type in ParameterTypes)
                type.ScopeForUsedTypes();
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                variable.Type.SubstituteWithTypearg(typeArguments).ScopeForUsedTypes();
            ProcedureDeclaration.SubstitutedScopeForUsedTypes(typeArguments);
            if (IsAnonymous)
                anonProcedureType.ScopeForUsedTypes();
            
            ProcedureDeclaration.DeclareUsedProcedureReference(this);
        }

        private void EmitCFunctionHeader(StringBuilder emitter)
        {
#pragma warning disable CS8604 // Parameters not null when compilation begins
            emitter.Append($"{ReturnType.GetCName()} {GetStandardIdentifier()}(");
            if (IsAnonymous)
            {
                if (ProcedureDeclaration.CapturedVariables.Count > 0)
                    emitter.Append($"{GetClosureCaptureCType()}* _nhp_captured");
#pragma warning disable CS8602 // Parameters initialized by compilation
                ProcedureDeclaration.Parameters.ForEach((parameter) =>
                {
                    emitter.Append(", ");
                    emitter.Append($"{parameter.Type.SubstituteWithTypearg(typeArguments).GetCName()} {parameter.GetStandardIdentifier()}");
                });
                emitter.Append(") ");
#pragma warning restore CS8602 
            }
            else
            {
                if (ProcedureDeclaration.CapturedVariables.Count > 0)
                    emitter.Append(string.Join(", ", (ProcedureDeclaration.Parameters.Concat(ProcedureDeclaration.CapturedVariables)).Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName() + " " + parameter.GetStandardIdentifier())));
                else
                    emitter.Append(string.Join(", ", ProcedureDeclaration.Parameters.Select((parameter) => parameter.Type.SubstituteWithTypearg(typeArguments).GetCName() + " " + parameter.GetStandardIdentifier())));
                emitter.Append(")");
            }
#pragma warning restore CS8604 
        }

        public void EmitCaptureCFunctionHeader(StringBuilder emitter)
        {
            emitter.Append($"{anonProcedureType.GetCName()} capture_{GetStandardIdentifier()}(");
            emitter.Append(string.Join(", ", ProcedureDeclaration.CapturedVariables.ConvertAll((capturedVar) => $"{capturedVar.Type.SubstituteWithTypearg(typeArguments).GetCName()} {capturedVar.GetStandardIdentifier()}")));
            emitter.Append(")");
        }

        public void ForwardDeclare(StringBuilder emitter)
        {
            if(IsAnonymous)
                emitter.AppendLine($"typedef struct {GetStandardIdentifier()}_captured {GetClosureCaptureCType()};");
            EmitCFunctionHeader(emitter);
            emitter.AppendLine(";");
            if (IsAnonymous)
            {
                EmitCaptureCFunctionHeader(emitter);
                emitter.AppendLine(";");
            }
        }

        public Dictionary<TypeParameter, IType> Emit(StringBuilder emitter)
        {
            EmitCFunctionHeader(emitter);
            emitter.AppendLine(" {");
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName()} {variable.GetStandardIdentifier()} = _nhp_captured->{variable.GetStandardIdentifier()};");
            if(ReturnType is not NothingType)
                emitter.AppendLine($"\t{ReturnType} _nhp_toret;");
            return typeArguments;
        }

        public void EmitCaptureContextCStruct(StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"struct {GetStandardIdentifier()}_captured {{");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier()}_t _nhp_this_anon;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier()}_destructor_t _nhp_destructor;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier()}_copier_t _nhp_copier;");
            emitter.AppendLine($"\t{anonProcedureType.GetStandardIdentifier()}_record_copier_t _nhp_record_copier;");
            foreach (Variable variable in ProcedureDeclaration.CapturedVariables)
                emitter.AppendLine($"\t{variable.Type.SubstituteWithTypearg(typeArguments).GetCName()} {variable.GetStandardIdentifier()};");
            emitter.AppendLine("};");
        }

        public void EmitAnonymizer(StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            EmitCaptureCFunctionHeader(emitter);
            emitter.AppendLine(" {");

            if(ProcedureDeclaration.CapturedVariables.Count > 0)
            {
                emitter.AppendLine($"\t{GetClosureCaptureCType()}* closure = malloc(sizeof({GetStandardIdentifier()}_captured_t));");
                emitter.AppendLine($"\tclosure->_nhp_this_anon = ({anonProcedureType.GetStandardIdentifier()}_t)(&{GetStandardIdentifier()});");
                emitter.AppendLine($"\tclosure->_nhp_destructor = ({anonProcedureType.GetStandardIdentifier()}_destructor_t)(&free{GetStandardIdentifier()});");
                emitter.AppendLine($"\tclosure->_nhp_copier = ({anonProcedureType.GetStandardIdentifier()}_copier_t)(&copy{GetStandardIdentifier()});");
                emitter.AppendLine($"\tclosure->_nhp_record_copier = ({anonProcedureType.GetStandardIdentifier()}_record_copier_t)(&record_copy{GetStandardIdentifier()});");

                foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
                {
                    emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier()} = ");
                    capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(emitter, capturedVariable.GetStandardIdentifier());
                    emitter.AppendLine(";");
                }
            }
            else
            {
                emitter.AppendLine($"\t{anonProcedureType.GetCName()} closure = malloc(sizeof({anonProcedureType.GetStandardIdentifier()}));");
                emitter.AppendLine($"\t*closure = ({anonProcedureType.GetStandardIdentifier()})(&{GetStandardIdentifier()});");
            }
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName()})closure;");
            emitter.AppendLine("}");
        }

        public void EmitAnonDestructor(StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"void free{GetStandardIdentifier()}({anonProcedureType.GetCName()} to_free_anon) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType()}* to_free = ({GetClosureCaptureCType()}*)to_free_anon;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables) 
            {
                emitter.Append('\t');
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitFreeValue(emitter, $"to_free->{capturedVariable.GetStandardIdentifier()}");
            }
            
            emitter.AppendLine("\tfree(to_free);");
            emitter.AppendLine("}");
        }

        public void EmitAnonCopier(StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName()} copy{GetStandardIdentifier()}({anonProcedureType.GetCName()} to_copy_anon) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType()}* to_copy = ({GetClosureCaptureCType()}*)to_copy_anon;");
            emitter.AppendLine($"\t{GetClosureCaptureCType()}* closure = malloc(sizeof({GetStandardIdentifier()}_captured_t));");
            emitter.AppendLine("\tclosure->_nhp_this_anon = to_copy->_nhp_this_anon;");
            emitter.AppendLine("\tclosure->_nhp_destructor = to_copy->_nhp_destructor;");
            emitter.AppendLine("\tclosure->_nhp_copier = to_copy->_nhp_copier;");
            emitter.AppendLine("\tclosure->_nhp_record_copier = to_copy->_nhp_record_copier;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier()} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(emitter, $"to_copy->{capturedVariable.GetStandardIdentifier()}");
                emitter.AppendLine(";");
            }
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName()})closure;");
            emitter.AppendLine("}");
        }

        public void EmitAnonRecordCopier(StringBuilder emitter)
        {
            if (!IsAnonymous)
                return;

            emitter.AppendLine($"{anonProcedureType.GetCName()} record_copy{GetStandardIdentifier()}({anonProcedureType.GetCName()} to_copy_anon, void* record) {{");
            emitter.AppendLine($"\t{GetClosureCaptureCType()}* to_copy = ({GetClosureCaptureCType()}*)to_copy_anon;");
            emitter.AppendLine($"\t{GetClosureCaptureCType()}* closure = malloc(sizeof({GetStandardIdentifier()}_captured_t));");
            emitter.AppendLine("\tclosure->_nhp_this_anon = to_copy->_nhp_this_anon;");
            emitter.AppendLine("\tclosure->_nhp_destructor = to_copy->_nhp_destructor;");
            emitter.AppendLine("\tclosure->_nhp_copier = to_copy->_nhp_copier;");
            emitter.AppendLine("\tclosure->_nhp_record_copier = to_copy->_nhp_record_copier;");

            foreach (Variable capturedVariable in ProcedureDeclaration.CapturedVariables)
            {
                emitter.Append($"\tclosure->{capturedVariable.GetStandardIdentifier()} = ");
                capturedVariable.Type.SubstituteWithTypearg(typeArguments).EmitClosureBorrowValue(emitter, capturedVariable.IsRecordSelf ? $"({capturedVariable.Type.SubstituteWithTypearg(typeArguments).GetCName()})record" : $"to_copy->{capturedVariable.GetStandardIdentifier()}");
                emitter.AppendLine(";");
            }
            emitter.AppendLine($"\treturn ({anonProcedureType.GetCName()})closure;");
            emitter.AppendLine("}");
        }
    }

    partial class ReturnStatement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            ToReturn.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            ToReturn.ScopeForUsedTypes(typeargs);
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);

            if (ToReturn.Type is not NothingType)
            {
                emitter.Append("_nhp_toret = ");

                if (ToReturn.RequiresDisposal(typeargs))
                    ToReturn.Emit(emitter, typeargs);
                else
                {
                    StringBuilder valueBuilder = new StringBuilder();
                    ToReturn.Emit(valueBuilder, typeargs);
                    ToReturn.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(emitter, valueBuilder.ToString());
                }

                emitter.AppendLine(";");
            }

            List<Variable> toFree = parentCodeBlock.GetCurrentLocals();
            foreach (Variable variable in toFree)
                if (variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    CodeBlock.CIndent(emitter, indent);
                    variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(emitter, variable.GetStandardIdentifier());
                }

            CodeBlock.CIndent(emitter, indent);
            if (ToReturn.Type is not NothingType)
                emitter.AppendLine("return _nhp_toret;");
            else
                emitter.Append("return;");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class LinkedProcedureCall
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => Procedure.SubstituteWithTypearg(typeargs).ReturnType.RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            foreach (IRValue argument in Arguments)
                argument.ScopeForUsedTypes(typeargs);
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier()}(");
            for(int i = 0; i < Arguments.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");
                IRValue.EmitMemorySafe(Arguments[i], emitter, typeargs);
            }
            for(int i = 0; i < Procedure.ProcedureDeclaration.CapturedVariables.Count; i++)
            {
                if (i > 0 || Arguments.Count > 0)
                    emitter.Append(", ");
                VariableReference variableReference = new VariableReference(Procedure.ProcedureDeclaration.CapturedVariables[i], ErrorReportedElement);
                variableReference.Emit(emitter, typeargs);
            }
            emitter.Append(')');
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(emitter, typeargs);
            emitter.AppendLine(";");
        }
    }

    partial class AnonymousProcedureCall
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => ProcedureType.ReturnType.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            ProcedureValue.ScopeForUsedTypes(typeargs);
            foreach (IRValue argument in Arguments)
                argument.ScopeForUsedTypes(typeargs);
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            IRValue.EmitMemorySafe(ProcedureValue, emitter, typeargs);
            emitter.Append("->_nhp_this_anon(");
            IRValue.EmitMemorySafe(ProcedureValue, emitter, typeargs);
            foreach (IRValue argument in Arguments)
            {
                emitter.Append(", ");
                IRValue.EmitMemorySafe(argument, emitter, typeargs);
            }
            emitter.Append(')');
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);

            if (ProcedureType.ReturnType.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                StringBuilder valueEmitter = new StringBuilder();
                Emit(valueEmitter, typeargs);
                ProcedureType.ReturnType.SubstituteWithTypearg(typeargs).EmitFreeValue(emitter, valueEmitter.ToString());
            }
            else
                Emit(emitter, typeargs);

            emitter.AppendLine(";");
        }
    }

    partial class AnonymizeProcedure
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Procedure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"capture_{Procedure.SubstituteWithTypearg(typeargs).GetStandardIdentifier()}({string.Join(", ", Procedure.SubstituteWithTypearg(typeargs).ProcedureDeclaration.CapturedVariables.ConvertAll((capturedVar) => (capturedVar.IsRecordSelf) ? "_nhp_self" :capturedVar.GetStandardIdentifier()))})");
        }
    }
}