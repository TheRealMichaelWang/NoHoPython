using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
        public bool RequiresDisposal => true;

        private static List<EnumType> usedEnumTypes = new List<EnumType>();
        private static Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads = new Dictionary<EnumDeclaration, List<EnumType>>();

        public static bool DeclareUsedEnumType(EnumType enumType)
        {
            foreach (EnumType usedEnum in usedEnumTypes)
                if (enumType.IsCompatibleWith(usedEnum))
                    return false;

            usedEnumTypes.Add(enumType);
            if (!enumTypeOverloads.ContainsKey(enumType.EnumDeclaration))
                enumTypeOverloads.Add(enumType.EnumDeclaration, new List<EnumType>());
            enumTypeOverloads[enumType.EnumDeclaration].Add(enumType);

            return true;
        }

        public static void ForwardDeclareInterfaceTypes(StringBuilder emitter)
        {
            foreach (EnumType usedEnum in usedEnumTypes)
            {
                emitter.AppendLine($"typedef struct {usedEnum.GetStandardIdentifier()} {usedEnum.GetCName()};");
                usedEnum.EmitOptionsCEnum(emitter);
            }
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        public void ForwardDeclareType(StringBuilder emitter)
        {
            if (!enumTypeOverloads.ContainsKey(this))
                return;

            foreach (EnumType enumType in enumTypeOverloads[this])
                enumType.EmitCStruct(emitter);
        }

        public void ForwardDeclare(StringBuilder emitter)
        {
            if (!enumTypeOverloads.ContainsKey(this))
                return;

            foreach(EnumType usedEnum in enumTypeOverloads[this])
            {
                usedEnum.EmitMarshallerHeaders(emitter);
                emitter.AppendLine($"void free_enum{usedEnum.GetStandardIdentifier()}({usedEnum.GetCName()} enum);");
                emitter.AppendLine($"{usedEnum.GetCName()} copy_enum{usedEnum.GetStandardIdentifier()}({usedEnum.GetCName()} enum);"); 
                emitter.AppendLine($"{usedEnum.GetCName()} move_enum{usedEnum.GetStandardIdentifier()}({usedEnum.GetCName()}* dest, {usedEnum.GetCName()} src);");
            }
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!enumTypeOverloads.ContainsKey(this))
                return;

            foreach (EnumType enumType in enumTypeOverloads[this])
            {
                enumType.EmitMarshallers(emitter);
                enumType.EmitDestructor(emitter);
                enumType.EmitCopier(emitter);
                enumType.EmitMover(emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class EnumType
    {
        public bool RequiresDisposal => true;

        public string GetStandardIdentifier() => $"_nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier()))}_";

        public string GetCName() => $"{GetStandardIdentifier()}_t";

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => emitter.AppendLine($"free_enum{GetStandardIdentifier()}({valueCSource});");
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append($"copy_enum{GetStandardIdentifier()}({valueCSource})");
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move_enum{GetStandardIdentifier()}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => EmitCopyValue(emitter, valueCSource);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(emitter, valueCSource);

        public string GetCEnumOptionForType(IType type) => $"{GetStandardIdentifier()}OPTION_{type.GetCName()}";

        public void ScopeForUsedTypes()
        {
            if (IntermediateRepresentation.Statements.EnumDeclaration.DeclareUsedEnumType(this))
            {
                foreach (IType options in options.Value)
                    options.ScopeForUsedTypes();
            }
        }

        public void EmitCStruct(StringBuilder emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier() + " {");
            emitter.AppendLine("\t{GetStandardIdentifier()}_options_t option;");

            emitter.AppendLine($"\tunion {GetStandardIdentifier()}_data {{");
            foreach (IType option in options.Value)
                emitter.AppendLine($"\t\t{option.GetCName()} {option.GetCName()}_set;");
            emitter.AppendLine("\t} data;");
            emitter.AppendLine("};");
        }

        public void EmitOptionsCEnum(StringBuilder emitter)
        {
            emitter.AppendLine($"typedef enum {GetStandardIdentifier()}_options {{");
            for(int i = 0; i < options.Value.Count; i++)
            {
                if (i > 0)
                    emitter.AppendLine(",");
                emitter.Append(GetCEnumOptionForType(options.Value[i]));
            }
            emitter.AppendLine();
            emitter.Append("} ");
            emitter.AppendLine($"{GetStandardIdentifier()}_options_t");
        }

        public void EmitMarshallerHeaders(StringBuilder emitter)
        {
            foreach (IType option in options.Value)
                emitter.AppendLine($"{GetCName()} marshal_enum{GetStandardIdentifier()}_with_{option.GetCName()}({option.GetCName()} option);");
        }

        public void EmitMarshallers(StringBuilder emitter)
        {
            foreach (IType option in options.Value)
            {
                emitter.AppendLine($"{GetCName()} marshal_enum{GetStandardIdentifier()}_with_{option.GetCName()}({option.GetCName()} option) {{");
                emitter.AppendLine($"\t{GetCName()} marshalled_enum;");
                emitter.AppendLine($"\tmarshalled_enum.option = {GetCEnumOptionForType(option)};");
                emitter.AppendLine($"\tmarshalled_enum.data.{option.GetCName()}_set = option;");
                emitter.AppendLine("\treturn marshalled_enum;");
                emitter.AppendLine("}");
            }
        }

        public void EmitDestructor(StringBuilder emitter)
        {
            emitter.AppendLine($"void free_enum{GetStandardIdentifier()}({GetCName()} enum) {{");
            emitter.AppendLine("\tswitch(enum.option) {");
            foreach(IType option in options.Value)
            {
                emitter.AppendLine($"\tcase {GetCEnumOptionForType(option)}:");
                emitter.Append("\t\t");
                option.EmitFreeValue(emitter, $"enum.{option.GetCName()}_set");
                emitter.AppendLine("\t\tbreak;");
            }
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void EmitCopier(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} copy_enum{GetStandardIdentifier()}({GetCName()} enum) {{");

            emitter.AppendLine($"\t{GetCName()} copied_enum;");
            emitter.AppendLine("\tcopied_enum.option = enum.option;");

            emitter.AppendLine("\tswitch(enum.option) {");
            foreach (IType option in options.Value)
            {
                emitter.AppendLine($"\tcase {GetCEnumOptionForType(option)}:");
                emitter.Append("\t\t");
                emitter.AppendLine($"copied_enum.{option.GetCName()}_set = ");
                option.EmitCopyValue(emitter, $"enum.{option.GetCName()}_set");
                emitter.AppendLine(";");
                emitter.AppendLine("\t\tbreak;");
            }
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn copied_enum;");
            emitter.AppendLine("}");
        }

        public void EmitMover(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} move_enum{GetStandardIdentifier()}({GetCName()}* dest, {GetCName()} src) {{");
            emitter.AppendLine($"\t{GetCName()} temp_buffer = *dest;");
            emitter.AppendLine($"\t*dest = src;");
            emitter.Append('\t');
            EmitFreeValue(emitter, "temp_buffer");
            emitter.AppendLine(";");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoEnum
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Value.ScopeForUsedTypes(typeargs);
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            EnumType realPrototype = (EnumType)TargetType.SubstituteWithTypearg(typeargs);

            emitter.Append($"marshal_enum{realPrototype.GetStandardIdentifier()}_with_{Value.Type.SubstituteWithTypearg(typeargs).GetCName()}(");
            Value.Emit(emitter, typeargs);
            emitter.Append(')');
        }
    }
}