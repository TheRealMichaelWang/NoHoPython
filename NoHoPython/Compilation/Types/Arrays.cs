using System.Text;

namespace NoHoPython.Typing
{
    partial class ArrayType
    {
        private static List<ArrayType> usedArrayTypes = new List<ArrayType>();

        public static void ForwardDeclareArrayTypes(StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                emitter.AppendLine($"typedef struct {arrayType.GetStandardIdentifier()} {arrayType.GetCName()};");
        }

        public static void EmitCStructs(StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                arrayType.EmitCStruct(emitter);
        }

        public static void ForwardDeclare(StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
            {
                emitter.AppendLine($"{arrayType.GetCName()} marshal{arrayType.GetStandardIdentifier()}(const {arrayType.ElementType.GetCName()}* buffer, int length);");
                emitter.AppendLine($"{arrayType.GetCName()} marshal_proto{arrayType.GetStandardIdentifier()}(int length, {arrayType.ElementType.GetCName()} proto);");
                emitter.AppendLine($"{arrayType.GetCName()} copy{arrayType.GetStandardIdentifier()}({arrayType.GetCName()} to_copy);");
                emitter.AppendLine($"{arrayType.GetCName()} move{arrayType.GetStandardIdentifier()}({arrayType.GetCName()}* src, {arrayType.GetCName()} dest);");

                if (arrayType.ElementType.RequiresDisposal)
                    emitter.AppendLine($"void free{arrayType.GetStandardIdentifier()}({arrayType.GetCName()} to_free);");
            }
        }

        public static void EmitMarshallers(StringBuilder emitter)
        {
            emitter.AppendLine("int _nhp_bounds_check(int index, int max_length) {");
            emitter.AppendLine("\tif(index < 0 || index >= max_length) {");
            emitter.AppendLine("\t\tprintf(\"Index out of bounds. Index was %i, alloced length was %i.\", index, max_length);");
            emitter.AppendLine("\t\tabort();");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn index;");
            emitter.AppendLine("}");

            foreach (ArrayType arrayType in usedArrayTypes)
            {
                arrayType.EmitMarshaller(emitter);
                arrayType.EmitDestructor(emitter);
                arrayType.EmitMover(emitter);
                arrayType.EmitCopier(emitter);
            }
        }

        public bool RequiresDisposal => true;

        public void EmitFreeValue(StringBuilder emitter, string valueCSource)
        {
            if (ElementType.RequiresDisposal)
                emitter.AppendLine($"free{GetStandardIdentifier()}({valueCSource});");
            else
                emitter.AppendLine($"free({valueCSource}.buffer);");
        }

        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append($"copy{GetStandardIdentifier()}({valueCSource})");
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move{GetStandardIdentifier()}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => EmitCopyValue(emitter, valueCSource);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(emitter, valueCSource);

        public void ScopeForUsedTypes()
        {
            ElementType.ScopeForUsedTypes();
            foreach (ArrayType arrayType in usedArrayTypes)
                if (arrayType.IsCompatibleWith(this))
                    return;
            usedArrayTypes.Add(this);
        }

        public string GetCName() => $"{GetStandardIdentifier()}_t";

        public string GetStandardIdentifier() => $"_nhp_array_{ElementType.GetStandardIdentifier()}";

        public void EmitCStruct(StringBuilder emitter)
        {
            emitter.AppendLine($"struct {GetStandardIdentifier()} {{");
            emitter.AppendLine($"\t{ElementType.GetCName()}* buffer;");
            emitter.AppendLine("\tint length;");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} marshal{GetStandardIdentifier()}(const {ElementType.GetCName()}* buffer, int length) {{");
            emitter.AppendLine($"\t{GetCName()} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = malloc(length * sizeof({ElementType.GetCName()}));");
            emitter.AppendLine($"\tmemcpy(to_alloc.buffer, buffer, length * sizeof({ElementType.GetCName()}));");
            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");

            emitter.AppendLine($"{GetCName()} marshal_proto{GetStandardIdentifier()}(int length, {ElementType.GetCName()} proto) {{");
            emitter.AppendLine($"\t{GetCName()} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = malloc(length * sizeof({ElementType.GetCName()}));");

            emitter.AppendLine($"\tfor(int i = 0; i < to_alloc.length; i++)");
            emitter.Append("\t\tto_alloc.buffer[i] = ");
            ElementType.EmitCopyValue(emitter, "proto");
            emitter.AppendLine(";");

            if (ElementType.RequiresDisposal)
            {
                emitter.Append('\t');
                ElementType.EmitFreeValue(emitter, "proto");
            }

            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(StringBuilder emitter)
        {
            if (!ElementType.RequiresDisposal)
                return;

            emitter.AppendLine($"void free{GetStandardIdentifier()}({GetCName()} to_free) {{");
            emitter.AppendLine("\tfor(int i = 0; i < to_free.length; i++) {{");
            emitter.Append("\t\t");
            ElementType.EmitFreeValue(emitter, "to_free.buffer[i]");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\tfree(to_free.buffer);");
            emitter.AppendLine("}");
        }

        public void EmitCopier(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} copy{GetStandardIdentifier()}({GetCName()} to_copy) {{");
            emitter.AppendLine($"\t{GetCName()} copied;");
            emitter.AppendLine($"\tcopied.buffer = malloc(to_copy.length * sizeof({ElementType.GetCName()}));");

            emitter.AppendLine("\tfor(int i = 0; i < to_copy.length; i++)");
            emitter.Append("\t\tcopied.buffer[i] = ");
            ElementType.EmitCopyValue(emitter, "to_copy.buffer[i]");
            emitter.AppendLine(";");

            emitter.AppendLine("\treturn copied;");
            emitter.AppendLine("}");
        }

        public void EmitMover(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} move{GetStandardIdentifier()}({GetCName()}* dest, {GetCName()} src) {{");
            emitter.Append('\t');
            EmitFreeValue(emitter, "(*dest)");
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }
    }
}
