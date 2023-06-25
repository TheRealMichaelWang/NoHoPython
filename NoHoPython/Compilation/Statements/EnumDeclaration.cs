using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private List<EnumType> usedEnumTypes = new();
        private Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads = new();
        
        public bool DeclareUsedEnumType(EnumType enumType)
        {
            foreach (EnumType usedEnum in usedEnumTypes)
                if (enumType.IsCompatibleWith(usedEnum))
                    return false;

            usedEnumTypes.Add(enumType);
            if (!enumTypeOverloads.ContainsKey(enumType.EnumDeclaration))
                enumTypeOverloads.Add(enumType.EnumDeclaration, new List<EnumType>());
            enumTypeOverloads[enumType.EnumDeclaration].Add(enumType);

            typeDependencyTree.Add(enumType, new HashSet<IType>(enumType.GetOptions().Where((type) => type is not RecordType), new ITypeComparer()));
            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public readonly Dictionary<EnumDeclaration, List<EnumType>> EnumTypeOverloads;

        public void ForwardDeclareEnumTypes(StatementEmitter emitter)
        {
            foreach(EnumDeclaration enumDeclaration in EnumTypeOverloads.Keys)
            {
                enumDeclaration.EmitOptionsCEnum(this, emitter);

                if (enumDeclaration.EmitMultipleCStructs)
                {
                    foreach (EnumType enumType in EnumTypeOverloads[enumDeclaration])
                        emitter.AppendLine($"typedef struct {enumType.GetStandardIdentifier(this)} {enumType.GetCName(this)};");
                }
                else
                    emitter.AppendLine($"typedef struct {EnumTypeOverloads[enumDeclaration].First().GetStandardIdentifier(this)} {EnumTypeOverloads[enumDeclaration].First().GetCName(this)};");
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
#pragma warning disable CS8602 // Options already linked
#pragma warning disable CS8604
        public bool IsEmpty => options.TrueForAll((option) => option.IsEmpty);
        public bool EmitMultipleCStructs => options.Any((option) => option.TypeParameterAffectsCodegen(new(new ITypeComparer())));
#pragma warning restore CS8604
#pragma warning restore CS8602

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public string GetCEnumOptionForType(IRProgram irProgram, IType type) => $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_OPTION_{type.GetStandardIdentifier(irProgram)}";
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public string GetCEnumOptionForType(IRProgram irProgram, int optionNumber) => GetCEnumOptionForType(irProgram, options[optionNumber]);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public void ForwardDeclareType(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this))
                return;

            if (EmitMultipleCStructs)
            {
                foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
                    enumType.EmitCStruct(irProgram, emitter);
            }
            else
            {
                foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
                {
                    if (enumType == irProgram.EnumTypeOverloads[this].First())
                    {
                        if (!irProgram.DeclareCompiledType(emitter, enumType))
                            return;

                        enumType.EmitCStructImmediatley(irProgram, emitter);
                    }
                    else
                        irProgram.DeclareCompiledType(emitter, enumType);
                }
            }
        }

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this))
                return;

            foreach(EnumType usedEnum in irProgram.EnumTypeOverloads[this])
            {
                //usedEnum.EmitMarshallerHeaders(irProgram, emitter);
                usedEnum.EmitPropertyGetHeaders(irProgram, emitter);
                if (usedEnum.RequiresDisposal)
                {
                    emitter.AppendLine($"void free_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} _nhp_enum, void* child_agent);");
                    emitter.AppendLine($"{usedEnum.GetCName(irProgram)} copy_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} _nhp_enum, void* responsible_destroyer);");
                    if (!irProgram.EmitExpressionStatements)
                        emitter.AppendLine($"{usedEnum.GetCName(irProgram)} move_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)}* dest, {usedEnum.GetCName(irProgram)} src, void* child_agent);");
                }

                if (!EmitMultipleCStructs)
                    return;
            }
        }

        public void EmitOptionsCEnum(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine($"typedef enum _nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_options {{");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            for (int i = 0; i < options.Count; i++)
            {
                if (i > 0)
                    emitter.AppendLine(",");
                emitter.Append('\t');
                emitter.Append(GetCEnumOptionForType(irProgram, options[i]));
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            emitter.AppendLine();
            emitter.Append("} ");
            emitter.AppendLine($"_nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_options_t;");
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this))
                return;

            foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
            {
                //enumType.EmitMarshallers(irProgram, emitter);
                enumType.EmitPropertyGetters(irProgram, emitter);
                if (enumType.RequiresDisposal)
                {
                    enumType.EmitDestructor(irProgram, emitter);
                    enumType.EmitCopier(irProgram, emitter);
                    enumType.EmitMover(irProgram, emitter);
                }
                enumType.EmitOptionTypeNames(irProgram, emitter);

                if (!EmitMultipleCStructs)
                    return;
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class EmptyEnumOption
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => false;
        public bool MustSetResponsibleDestroyer => false;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => false;

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_empty_option_{Name}";
        public string GetCName(IRProgram irProgram) => throw new CannotCompileEmptyTypeError(null);

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string recordCSource) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }

    partial class EnumType
    {
        partial class EnumProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => EnumType.GetOptions().Any((option) => ((IPropertyContainer)option).FindProperty(Name).RequiresDisposal(typeargs));
            
            public bool IsUsed
            {
                get
                {
                    if(EnumType.EnumDeclaration.EmitMultipleCStructs)
                    {
                        if (typePropertyInUse.ContainsKey(EnumType))
                            return typePropertyInUse[EnumType].Contains(this);
                    }
                    else if(declarationPropertyInUse.ContainsKey(EnumType.EnumDeclaration))
                        return declarationPropertyInUse[EnumType.EnumDeclaration].Contains(this);
                    return false;
                }
            }

            public override void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
            {
                Debug.Assert(!optimizedMessageRecieverCall);

                EnumType enumType = (EnumType)EnumType.SubstituteWithTypearg(typeargs);

                foreach(IType type in globalSupportedOptions[enumType].Value.Keys)
                {
                    Property accessProperty = ((IPropertyContainer)type).FindProperty(Name);
                    accessProperty.ScopeForUse(false, typeargs, irBuilder);
                }

                if (!declarationPropertyInUse.ContainsKey(enumType.EnumDeclaration))
                    declarationPropertyInUse.Add(enumType.EnumDeclaration, new());
                declarationPropertyInUse[enumType.EnumDeclaration].Add(this);

                if (!typePropertyInUse.ContainsKey(enumType))
                    typePropertyInUse.Add(enumType, new());
                typePropertyInUse[enumType].Add(this);
            }

            public override bool EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource, string responsibleDestroyer)
            {
                Debug.Assert(propertyContainer is EnumType enumType && EnumType.IsCompatibleWith(enumType));

                emitter.Append($"get_{Name}{EnumType.GetStandardIdentifier(irProgram)}({valueCSource}");
                if (RequiresDisposal(typeargs))
                    emitter.Append($", {responsibleDestroyer}");
                emitter.Append(')');

                return false;
            }
        }

        private static Dictionary<EnumDeclaration, HashSet<EnumProperty>> declarationPropertyInUse = new();
        private static Dictionary<EnumType, HashSet<EnumProperty>> typePropertyInUse = new(new ITypeComparer());

        public bool RequiresDisposal => globalSupportedOptions[this].Value.Keys.Any((option) => option.RequiresDisposal);
        public bool MustSetResponsibleDestroyer => globalSupportedOptions[this].Value.Keys.Any((option) => option.MustSetResponsibleDestroyer);

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => globalSupportedOptions[this].Value.Keys.Any((option) => option.TypeParameterAffectsCodegen(effectInfo));

        public string GetStandardIdentifier(IRProgram irProgram) => EnumDeclaration.EmitMultipleCStructs ? $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}" : $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}";

        public string GetCName(IRProgram irProgram) => EnumDeclaration.IsEmpty ? $"{GetStandardIdentifier(irProgram)}_options_t" : $"{GetStandardIdentifier(irProgram)}_t";

        public string GetCEnumOptionForType(IRProgram irProgram, IType type) => EnumDeclaration.GetCEnumOptionForType(irProgram, globalSupportedOptions[this].Value[type]);

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent)
        {
            if(RequiresDisposal)
                emitter.Append($"free_enum{GetStandardIdentifier(irProgram)}({valueCSource}, {childAgent});");
        }

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (RequiresDisposal)
            {
                emitter.Append($"copy_enum{GetStandardIdentifier(irProgram)}({valueCSource}");
                if (MustSetResponsibleDestroyer)
                    emitter.Append($", {responsibleDestroyer}");
                emitter.Append(')');
            }
            else
                emitter.Append(valueCSource);
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (RequiresDisposal)
            {
                if (irProgram.EmitExpressionStatements)
                    IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
                else
                    emitter.Append($"move_enum{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource}, {childAgent})");
            }
            else
                emitter.Append($"({destC} = {valueCSource})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);


        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedEnumType(this))
            {
                foreach (IType options in globalSupportedOptions[this].Value.Keys)
                    options.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this) || EnumDeclaration.IsEmpty)
                return;

            if(!EnumDeclaration.EmitMultipleCStructs)
            {
                EnumDeclaration.ForwardDeclareType(irProgram, emitter);
                return;
            }

            EmitCStructImmediatley(irProgram, emitter);
        }

        public void EmitCStructImmediatley(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            emitter.AppendLine($"\t_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_options_t option;");

            emitter.AppendLine($"\tunion {GetStandardIdentifier(irProgram)}_data {{");
            foreach (IType option in globalSupportedOptions[this].Value.Keys)
            {
                if (!option.IsEmpty)
                    emitter.AppendLine($"\t\t{option.GetCName(irProgram)} {option.GetStandardIdentifier(irProgram)}_set;");
            }
            emitter.AppendLine("\t} data;");

            emitter.AppendLine("};");
        }

        public void EmitPropertyGetHeaders(IRProgram irProgram, StatementEmitter emitter)
        {
            foreach (EnumProperty property in globalSupportedProperties[this].Value.Values)
            {
                if (property.IsUsed)
                {
                    emitter.Append($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum");
                    if (property.RequiresDisposal(typeargMap.Value))
                        emitter.Append(", void* responsible_destroyer");
                    emitter.AppendLine(");");
                }
            }
        }

        public void EmitPropertyGetters(IRProgram irProgram, StatementEmitter emitter)
        {
            foreach (EnumProperty property in globalSupportedProperties[this].Value.Values)
            {
                if (property.IsUsed)
                {
                    emitter.Append($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum");
                    if (property.RequiresDisposal(typeargMap.Value))
                        emitter.Append(", void* responsible_destroyer");
                    emitter.AppendLine(") {");

                    emitter.AppendLine("\tswitch(_nhp_enum.option) {");
                    foreach (KeyValuePair<IType, int> option in globalSupportedOptions[this].Value)
                    {
                        emitter.AppendLine($"\tcase {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                        emitter.Append("\t\treturn ");

                        IPropertyContainer propertyContainer = (IPropertyContainer)option.Key;
                        Property accessProperty = propertyContainer.FindProperty(property.Name);

                        if (!accessProperty.RequiresDisposal(typeargMap.Value) && property.RequiresDisposal(typeargMap.Value))
                        {
                            BufferedEmitter propertyGet = new();
                            accessProperty.EmitGet(irProgram, propertyGet, typeargMap.Value, propertyContainer, $"_nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set", "NULL");
                            property.Type.EmitCopyValue(irProgram, emitter, propertyGet.ToString(), "responsible_destroyer");
                        }
                        else
                        {
                            accessProperty.EmitGet(irProgram, emitter, typeargMap.Value, propertyContainer, $"_nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set", "responsible_destroyer");
                        }
                        emitter.AppendLine(";");
                    }
                    emitter.AppendLine("\t}");
                    emitter.AppendLine("}");
                }
            }
        }

        public void EmitDestructor(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine($"void free_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum, void* child_agent) {{");
            emitter.AppendLine("\tswitch(_nhp_enum.option) {");

            foreach(KeyValuePair<IType, int> option in globalSupportedOptions[this].Value)
                if (option.Key.RequiresDisposal)
                {
                    emitter.AppendLine($"\tcase {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                    emitter.Append("\t\t");
                    option.Key.EmitFreeValue(irProgram, emitter, $"_nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set", "child_agent");
                    emitter.AppendLine();
                    emitter.AppendLine("\t\tbreak;");
                }

            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_enum;");
            emitter.AppendLine("\tcopied_enum.option = _nhp_enum.option;");

            emitter.AppendLine("\tswitch(_nhp_enum.option) {");
            foreach (KeyValuePair<IType, int> option in globalSupportedOptions[this].Value)
                if (!option.Key.IsEmpty)
                {
                    emitter.AppendLine($"\tcase {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                    emitter.Append($"\t\tcopied_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set = ");
                    option.Key.EmitCopyValue(irProgram, emitter, $"_nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set", "responsible_destroyer");
                    emitter.AppendLine(";");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\treturn copied_enum;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src, void* child_agent) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "*dest", "child_agent");
            emitter.AppendLine();
            emitter.AppendLine($"\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }

        public void EmitOptionTypeNames(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.NameRuntimeTypes)
                return;

            emitter.Append($"static const char* {GetStandardIdentifier(irProgram)}_typenames[] = {{");

            foreach (IType option in globalSupportedOptions[this].Value.Keys)
            {
                emitter.Append($"\t\"{option.TypeName}\"");
                if (option != globalSupportedOptions[this].Value.Keys.Last())
                    emitter.Append(',');
                emitter.AppendLine();
            }
            emitter.AppendLine("};");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoEnum
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => TargetType.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            EnumType realPrototype = (EnumType)TargetType.SubstituteWithTypearg(typeargs);

            if (realPrototype.EnumDeclaration.IsEmpty)
                emitter.Append(realPrototype.GetCEnumOptionForType(irProgram, Value.Type));
            else if(Value.Type.IsEmpty)
                emitter.Append($"(({realPrototype.GetCName(irProgram)}) {{.option = {realPrototype.GetCEnumOptionForType(irProgram, Value.Type.SubstituteWithTypearg(typeargs))}}})");
            else
            {
                emitter.Append($"(({realPrototype.GetCName(irProgram)}) {{.option = {realPrototype.GetCEnumOptionForType(irProgram, Value.Type.SubstituteWithTypearg(typeargs))}, .data = {{ .{Value.Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_set = ");

                if (Value.RequiresDisposal(typeargs, false))
                    Value.Emit(irProgram, emitter, typeargs, responsibleDestroyer, false);
                else
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), responsibleDestroyer);
                emitter.Append("}})");
            }
        }
    }

    partial class UnwrapEnumValue
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            EnumValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (!irProgram.EmitExpressionStatements)
                throw new CannotEmitDestructorError(this);

            irProgram.ExpressionDepth++;

            EnumType enumType = (EnumType)EnumValue.Type.SubstituteWithTypearg(typeargs);

            emitter.Append($"({{{enumType.GetCName(irProgram)} enum{irProgram.ExpressionDepth} = ");
            EnumValue.Emit(irProgram, emitter, typeargs, "NULL", true);
            
            emitter.Append($"; if(enum{irProgram.ExpressionDepth}.option != {enumType.GetCEnumOptionForType(irProgram, Type.SubstituteWithTypearg(typeargs))}) {{");
            if (irProgram.DoCallStack)
            {
                CallStackReporting.EmitErrorLoc(emitter, ErrorReportedElement);
                CallStackReporting.EmitPrintStackTrace(emitter);
                if (irProgram.NameRuntimeTypes)
                {
                    emitter.Append("printf(\"Unwrapping Error: Expected ");
                    ErrorReportedElement.EmitSrcAsCString(emitter, true, false);
                    emitter.Append($"to be {Type.SubstituteWithTypearg(typeargs).TypeName}, but got %s instead.\\n\", {enumType.GetStandardIdentifier(irProgram)}_typenames[(int)enum{irProgram.ExpressionDepth}.option]);");
                }
                else
                {
                    emitter.Append("puts(\"Unwrapping Error: ");
                    ErrorReportedElement.EmitSrcAsCString(emitter, false, false);
                    emitter.Append(" failed.\");");
                }
            }
            else
            {
                if (irProgram.NameRuntimeTypes)
                {
                    emitter.Append($"printf(\"Failed to unwrap {Type.SubstituteWithTypearg(typeargs).TypeName} from ");
                    CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString(), true, false);
                    emitter.Append($", got %s instead.\\n\", {enumType.GetStandardIdentifier(irProgram)}_typenames[(int)enum{irProgram.ExpressionDepth}.option]); ");
                }
                else
                {
                    emitter.Append("puts(\"Failed to unwrap enum from value, ");
                    CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString(), false, false);
                    emitter.Append(".\\n\\t");
                    ErrorReportedElement.EmitSrcAsCString(emitter, true, false);
                    emitter.Append("\");");
                }
            }
            emitter.Append($"abort();}} {Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} res{irProgram.ExpressionDepth} = ");
            

            Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, $"enum{irProgram.ExpressionDepth}.data.{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_set", responsibleDestroyer);
            emitter.Append(";");

            if (EnumValue.RequiresDisposal(typeargs, true))
                EnumValue.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"enum{irProgram.ExpressionDepth}", "NULL");

            irProgram.ExpressionDepth--;
        }
    }

    partial class CheckEnumOption
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Option.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            EnumValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            EnumType enumType = (EnumType)EnumValue.Type.SubstituteWithTypearg(typeargs);

            emitter.Append('(');
            IRValue.EmitMemorySafe(EnumValue, irProgram, emitter, typeargs);
            emitter.Append(".option == ");
            emitter.Append(enumType.GetCEnumOptionForType(irProgram, Option.SubstituteWithTypearg(typeargs)));
            emitter.Append(')');
        }
    }
}