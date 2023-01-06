using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

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
        private List<EnumType> usedEnumTypes;
        public readonly Dictionary<EnumDeclaration, List<EnumType>> EnumTypeOverloads;

        public void ForwardDeclareEnumTypes(StringBuilder emitter)
        {
            foreach (EnumType usedEnum in usedEnumTypes)
            {
                if(!usedEnum.EnumDeclaration.IsEmpty)
                    emitter.AppendLine($"typedef struct {usedEnum.GetStandardIdentifier(this)} {usedEnum.GetCName(this)};");
                usedEnum.EmitOptionsCEnum(this, emitter);
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
#pragma warning disable CS8602 // Options already linked
        public bool IsEmpty => options.TrueForAll((option) => option.IsEmpty);
#pragma warning restore CS8602 

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this))
                return;

            foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
                enumType.EmitCStruct(irProgram, emitter);
        }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this))
                return;

            foreach(EnumType usedEnum in irProgram.EnumTypeOverloads[this])
            {
                usedEnum.EmitMarshallerHeaders(irProgram, emitter);
                usedEnum.EmitPropertyGetHeaders(irProgram, emitter);
                if(!usedEnum.EnumDeclaration.IsEmpty)
                    emitter.AppendLine($"{usedEnum.GetCName(irProgram)} change_resp_owner{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} to_mutate, void* responsible_destroyer);");
                if (usedEnum.RequiresDisposal)
                {
                    emitter.AppendLine($"void free_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} _nhp_enum);");
                    emitter.AppendLine($"{usedEnum.GetCName(irProgram)} copy_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} _nhp_enum, void* responsible_destroyer);");
                    if (!irProgram.EmitExpressionStatements)
                        emitter.AppendLine($"{usedEnum.GetCName(irProgram)} move_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)}* dest, {usedEnum.GetCName(irProgram)} src);");
                }
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this))
                return;

            foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
            {
                enumType.EmitMarshallers(irProgram, emitter);
                enumType.EmitPropertyGetters(irProgram, emitter);
                enumType.EmitResponsibleDestroyerMutator(irProgram, emitter);
                if (enumType.RequiresDisposal)
                {
                    enumType.EmitDestructor(irProgram, emitter);
                    enumType.EmitCopier(irProgram, emitter);
                    enumType.EmitMover(irProgram, emitter);
                }
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

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_empty_option_{Name}";
        public string GetCName(IRProgram irProgram) => throw new CannotCompileEmptyTypeError(null);

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => throw new CannotCompileEmptyTypeError(null);
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => throw new CannotCompileEmptyTypeError(null);
        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }

    partial class EnumType
    {
        public bool RequiresDisposal => !options.Value.TrueForAll((option) => !option.RequiresDisposal);
        public bool MustSetResponsibleDestroyer => !options.Value.TrueForAll((option) => !option.MustSetResponsibleDestroyer);

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

        public string GetCName(IRProgram irProgram) => EnumDeclaration.IsEmpty ? $"{GetStandardIdentifier(irProgram)}_options_t" : $"{GetStandardIdentifier(irProgram)}_t";

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string childAgent)
        {
            if(RequiresDisposal)
                emitter.AppendLine($"free_enum{GetStandardIdentifier(irProgram)}({valueCSource});");
        }

        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer)
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

        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource)
        {
            if (RequiresDisposal)
            {
                if (irProgram.EmitExpressionStatements)
                    IType.EmitMoveExpressionStatement(this, irProgram, emitter, destC, valueCSource);
                else
                    emitter.Append($"move_enum{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource})");
            }
            else
                emitter.Append($"({destC} = {valueCSource})");
        }

        public void EmitGetProperty(IRProgram irProgram, StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"get_{property.Name}{GetStandardIdentifier(irProgram)}({valueCSource})");

        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);
        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => emitter.Append(EnumDeclaration.IsEmpty ? valueCSource : $"change_resp_owner{GetStandardIdentifier(irProgram)}({valueCSource}, {newResponsibleDestroyer})");

        public string GetCEnumOptionForType(IRProgram irProgram, IType type) => $"{GetStandardIdentifier(irProgram)}OPTION_{type.GetStandardIdentifier(irProgram)}";

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedEnumType(this))
            {
                foreach (IType options in options.Value)
                    options.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this) || EnumDeclaration.IsEmpty)
                return;

            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            emitter.AppendLine($"\t{GetStandardIdentifier(irProgram)}_options_t option;");

            emitter.AppendLine($"\tunion {GetStandardIdentifier(irProgram)}_data {{");
            foreach (IType option in options.Value)
                if(!option.IsEmpty)
                    emitter.AppendLine($"\t\t{option.GetCName(irProgram)} {option.GetStandardIdentifier(irProgram)}_set;");
            emitter.AppendLine("\t} data;");

            emitter.AppendLine("};");
        }

        public void EmitOptionsCEnum(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"typedef enum {GetStandardIdentifier(irProgram)}_options {{");
            for(int i = 0; i < options.Value.Count; i++)
            {
                if (i > 0)
                    emitter.AppendLine(",");
                emitter.Append('\t');
                emitter.Append(GetCEnumOptionForType(irProgram, options.Value[i]));
            }
            emitter.AppendLine();
            emitter.Append("} ");
            emitter.AppendLine($"{GetStandardIdentifier(irProgram)}_options_t;");
        }

        public void EmitMarshallerHeaders(IRProgram irProgram, StringBuilder emitter)
        {
            if (EnumDeclaration.IsEmpty)
                return;

            foreach (IType option in options.Value)
            {
                if(option.IsEmpty)
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_{option.GetStandardIdentifier(irProgram)}();");
                else
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_{option.GetStandardIdentifier(irProgram)}({option.GetCName(irProgram)} option);");
            }
        }

        public void EmitPropertyGetHeaders(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (Property property in globalSupportedProperties[this].Value.Values)
                emitter.AppendLine($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum);");
        }

        public void EmitMarshallers(IRProgram irProgram, StringBuilder emitter)
        {
            if (EnumDeclaration.IsEmpty)
                return;

            foreach (IType option in options.Value)
            {
                if (option.IsEmpty)
                {
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_{option.GetStandardIdentifier(irProgram)}() {{");
                    emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_enum;");
                }
                else
                {
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_{option.GetStandardIdentifier(irProgram)}({option.GetCName(irProgram)} option) {{");
                    emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_enum;");
                    emitter.AppendLine($"\tmarshalled_enum.data.{option.GetStandardIdentifier(irProgram)}_set = option;");
                }
                emitter.AppendLine($"\tmarshalled_enum.option = {GetCEnumOptionForType(irProgram, option)};");
                emitter.AppendLine("\treturn marshalled_enum;");
                emitter.AppendLine("}");
            }
        }

        public void EmitPropertyGetters(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (Property property in globalSupportedProperties[this].Value.Values)
            {
                emitter.AppendLine($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum) {{");
                emitter.AppendLine("\tswitch(_nhp_enum.option) {");
                foreach(IType option in options.Value)
                {
                    emitter.AppendLine($"\tcase {GetCEnumOptionForType(irProgram, option)}:");
                    emitter.Append("\t\treturn ");
                    IPropertyContainer propertyContainer = (IPropertyContainer)option;
                    propertyContainer.EmitGetProperty(irProgram, emitter, $"_nhp_enum.data.{option.GetStandardIdentifier(irProgram)}_set", property);
                    emitter.AppendLine(";");
                }
                emitter.AppendLine("\t}");
                emitter.AppendLine("}");
            }
        }

        public void EmitDestructor(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"void free_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum) {{");
            emitter.AppendLine("\tswitch(_nhp_enum.option) {");
            foreach(IType option in options.Value)
                if (option.RequiresDisposal)
                {
                    emitter.AppendLine($"\tcase {GetCEnumOptionForType(irProgram, option)}:");
                    emitter.Append("\t\t");
                    option.EmitFreeValue(irProgram, emitter, $"_nhp_enum.data.{option.GetStandardIdentifier(irProgram)}_set", "NULL");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_enum;");
            emitter.AppendLine("\tcopied_enum.option = _nhp_enum.option;");

            emitter.AppendLine("\tswitch(_nhp_enum.option) {");
            foreach (IType option in options.Value)
                if (!option.IsEmpty)
                {
                    emitter.AppendLine($"\tcase {GetCEnumOptionForType(irProgram, option)}:");
                    emitter.Append($"\t\tcopied_enum.data.{option.GetStandardIdentifier(irProgram)}_set = ");
                    option.EmitCopyValue(irProgram, emitter, $"_nhp_enum.data.{option.GetStandardIdentifier(irProgram)}_set", "responsible_destroyer");
                    emitter.AppendLine(";");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\treturn copied_enum;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StringBuilder emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "*dest", "NULL");
            emitter.AppendLine($"\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }

        public void EmitResponsibleDestroyerMutator(IRProgram irProgram, StringBuilder emitter)
        {
            if (!MustSetResponsibleDestroyer)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} change_resp_owner{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_mutate, void* responsible_destroyer) {{");
           
            emitter.AppendLine("\tswitch(to_mutate.option) {");
            foreach (IType option in options.Value)
                if (!option.IsEmpty)
                {
                    emitter.AppendLine($"\tcase {GetCEnumOptionForType(irProgram, option)}:");
                    emitter.Append($"\t\tto_mutate.data.{option.GetStandardIdentifier(irProgram)}_set = ");
                    option.EmitMutateResponsibleDestroyer(irProgram, emitter, $"to_mutate.data.{option.GetStandardIdentifier(irProgram)}_set", "responsible_destroyer");
                    emitter.AppendLine(";");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn to_mutate;");
            emitter.AppendLine("}");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoEnum
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => TargetType.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            EnumType realPrototype = (EnumType)TargetType.SubstituteWithTypearg(typeargs);

            if (realPrototype.EnumDeclaration.IsEmpty)
                emitter.Append(realPrototype.GetCEnumOptionForType(irProgram, Value.Type));
            else if(Value.Type.IsEmpty)
                emitter.Append($"marshal{realPrototype.GetStandardIdentifier(irProgram)}_with_{Value.Type.GetStandardIdentifier(irProgram)}()");
            else
            {
                emitter.Append($"marshal{realPrototype.GetStandardIdentifier(irProgram)}_with_{Value.Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, emitter, typeargs, responsibleDestroyer);
                else
                {
                    StringBuilder valueBuilder = new();
                    Value.Emit(irProgram, valueBuilder, typeargs, "NULL");
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), responsibleDestroyer);
                }
                emitter.Append(')');
            }
        }
    }

    partial class UnwrapEnumValue
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            EnumValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if (!irProgram.EmitExpressionStatements)
                throw new InvalidOperationException();

            irProgram.ExpressionDepth++;

            EnumType enumType = (EnumType)EnumValue.Type.SubstituteWithTypearg(typeargs);

            emitter.Append($"({{{enumType.GetCName(irProgram)} enum{irProgram.ExpressionDepth} = ");
            EnumValue.Emit(irProgram, emitter, typeargs, "NULL");
            
            emitter.Append($"; if(enum{irProgram.ExpressionDepth}.option != {enumType.GetCEnumOptionForType(irProgram, Type.SubstituteWithTypearg(typeargs))}) {{");
            if (irProgram.DoCallStack)
            {
                CallStackReporting.EmitErrorLoc(emitter, ErrorReportedElement);
                CallStackReporting.EmitPrintStackTrace(emitter);
                emitter.Append("puts(\"Unwrapping Error: ");
                ErrorReportedElement.EmitSrcAsCString(emitter, false);
                emitter.Append(" failed.\");");
            }
            else
            {
                emitter.Append("puts(\"Failed to unwrap enum from value, ");
                CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString(), false, false);
                emitter.Append(".\\n\\t");
                ErrorReportedElement.EmitSrcAsCString(emitter, false);
                emitter.Append("\");");
            }
            emitter.Append("abort();}");

            Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, $"enum{irProgram.ExpressionDepth}.data.{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_set", responsibleDestroyer);
            emitter.Append(";})");

            irProgram.ExpressionDepth--;
        }
    }

    partial class CheckEnumOption
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Option.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            EnumValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
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