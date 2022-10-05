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

            typeDependencyTree.Add(enumType, new HashSet<IType>(enumType.GetOptions(), new ITypeComparer()));
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
                if (usedEnum.RequiresDisposal)
                {
                    emitter.AppendLine($"void free_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} _nhp_enum);");
                    emitter.AppendLine($"{usedEnum.GetCName(irProgram)} copy_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} _nhp_enum);");
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
    partial class EnumType
    {
        public bool RequiresDisposal => !options.Value.TrueForAll((option) => !option.RequiresDisposal);

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource)
        {
            if(RequiresDisposal)
                emitter.AppendLine($"free_enum{GetStandardIdentifier(irProgram)}({valueCSource});");
        }

        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource)
        {
            if (RequiresDisposal)
                emitter.Append($"copy_enum{GetStandardIdentifier(irProgram)}({valueCSource})");
            else
                emitter.Append(valueCSource);
        }

        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource)
        {
            if (RequiresDisposal)
                emitter.Append($"move_enum{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource})");
            else
                emitter.Append($"({destC} = {valueCSource})");
        }

        public void EmitGetProperty(IRProgram irProgram, StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"get_{property.Name}{GetStandardIdentifier(irProgram)}({valueCSource})");

        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => EmitCopyValue(irProgram, emitter, valueCSource);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(irProgram, emitter, valueCSource);

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
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            emitter.AppendLine($"\t{GetStandardIdentifier(irProgram)}_options_t option;");

            emitter.AppendLine($"\tunion {GetStandardIdentifier(irProgram)}_data {{");
            foreach (IType option in options.Value)
                if(option is not NothingType)
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
                emitter.Append(GetCEnumOptionForType(irProgram, options.Value[i]));
            }
            emitter.AppendLine();
            emitter.Append("} ");
            emitter.AppendLine($"{GetStandardIdentifier(irProgram)}_options_t;");
        }

        public void EmitMarshallerHeaders(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (IType option in options.Value)
                if (option is not NothingType)
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_{option.GetStandardIdentifier(irProgram)}({option.GetCName(irProgram)} option);");
                else
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_nothing();");
        }

        public void EmitPropertyGetHeaders(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (Property property in globalSupportedProperties[this].Values)
                emitter.AppendLine($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum);");
        }

        public void EmitMarshallers(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (IType option in options.Value)
            {
                if (option is not NothingType)
                {
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_{option.GetStandardIdentifier(irProgram)}({option.GetCName(irProgram)} option) {{");
                    emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_enum;");
                    emitter.AppendLine($"\tmarshalled_enum.data.{option.GetStandardIdentifier(irProgram)}_set = option;");
                }
                else
                {
                    emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}_with_nothing() {{");
                    emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_enum;");
                }
                emitter.AppendLine($"\tmarshalled_enum.option = {GetCEnumOptionForType(irProgram, option)};");
                emitter.AppendLine("\treturn marshalled_enum;");
                emitter.AppendLine("}");
            }
        }

        public void EmitPropertyGetters(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (Property property in globalSupportedProperties[this].Values)
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
                    option.EmitFreeValue(irProgram, emitter, $"_nhp_enum.data.{option.GetStandardIdentifier(irProgram)}_set");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} copy_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} _nhp_enum) {{");

            emitter.AppendLine($"\t{GetCName(irProgram)} copied_enum;");
            emitter.AppendLine("\tcopied_enum.option = _nhp_enum.option;");

            emitter.AppendLine("\tswitch(_nhp_enum.option) {");
            foreach (IType option in options.Value)
                if (option is not NothingType)
                {
                    emitter.AppendLine($"\tcase {GetCEnumOptionForType(irProgram, option)}:");
                    emitter.Append("\t\t");
                    emitter.Append($"copied_enum.data.{option.GetStandardIdentifier(irProgram)}_set = ");
                    option.EmitCopyValue(irProgram, emitter, $"_nhp_enum.data.{option.GetStandardIdentifier(irProgram)}_set");
                    emitter.AppendLine(";");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\treturn copied_enum;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} move_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} temp_buffer = *dest;");
            emitter.AppendLine($"\t*dest = src;");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "temp_buffer");
            emitter.AppendLine("\treturn src;");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            EnumType realPrototype = (EnumType)TargetType.SubstituteWithTypearg(typeargs);

            if (Value.Type is not NothingType)
            {
                emitter.Append($"marshal{realPrototype.GetStandardIdentifier(irProgram)}_with_{Value.Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
                if(Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, emitter, typeargs);
                else
                {
                    StringBuilder valueBuilder = new();
                    Value.Emit(irProgram, valueBuilder, typeargs);
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString());
                }
                emitter.Append(')');
            }
            else
                emitter.Append($"marshal{realPrototype.GetStandardIdentifier(irProgram)}_with_nothing()");
        }
    }
}