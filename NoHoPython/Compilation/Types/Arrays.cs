using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private List<ArrayType> usedArrayTypes = new();

        public void ScopeForUsedArrayType(ArrayType toscope)
        {
            toscope.ElementType.ScopeForUsedTypes(this);
            foreach (ArrayType arrayType in usedArrayTypes)
                if (arrayType.IsCompatibleWith(toscope))
                    return;
            usedArrayTypes.Add(toscope);

            typeDependencyTree.Add(toscope, new HashSet<IType>(new ITypeComparer()));
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<ArrayType> usedArrayTypes;

        public void EmitArrayTypeTypedefs(StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                emitter.AppendLine($"typedef struct {arrayType.GetStandardIdentifier(this)} {arrayType.GetCName(this)};");
        }

        public void EmitArrayTypeCStructs(StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                arrayType.EmitCStruct(this, emitter);
        }

        public void ForwardDeclareArrayTypes(StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
            {
                emitter.AppendLine($"{arrayType.GetCName(this)} marshal{arrayType.GetStandardIdentifier(this)}(const {arrayType.ElementType.GetCName(this)}* buffer, int length);");
                emitter.AppendLine($"{arrayType.GetCName(this)} marshal_proto{arrayType.GetStandardIdentifier(this)}(int length, {arrayType.ElementType.GetCName(this)} proto);");
                emitter.AppendLine($"{arrayType.GetCName(this)} move{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)}* src, {arrayType.GetCName(this)} dest);");

                if (arrayType.ElementType.RequiresDisposal)
                {
                    emitter.AppendLine($"void free{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)} to_free);");
                    emitter.AppendLine($"{arrayType.GetCName(this)} copy{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)} to_copy);");
                }
            }
        }

        public void EmitArrayTypeMarshallers(StringBuilder emitter)
        {
            if (DoBoundsChecking)
            {
                emitter.AppendLine("int _nhp_bounds_check(int index, int max_length, const char* src_loc, const char* assertion_src) {");
                emitter.AppendLine("\tif(index < 0 || index >= max_length) {");
                emitter.AppendLine("\t\tprintf(\"Index out of bounds, %s. Index was %i, alloced length was %i.\\n\\t\", src_loc, index, max_length);");
                emitter.AppendLine("\t\tputs(assertion_src);");
                emitter.AppendLine("\t\tabort();");
                emitter.AppendLine("\t}");
                emitter.AppendLine("\treturn index;");
                emitter.AppendLine("}");
            }

            foreach (ArrayType arrayType in usedArrayTypes)
            {
                arrayType.EmitMarshaller(this, emitter);
                arrayType.EmitDestructor(this, emitter);
                arrayType.EmitMover(this, emitter);
                arrayType.EmitCopier(this, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ArrayType
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => true;

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource)
        {
            if (ElementType.RequiresDisposal)
                emitter.AppendLine($"free{GetStandardIdentifier(irProgram)}({valueCSource});");
            else
                emitter.AppendLine($"free({valueCSource}.buffer);");
        }

        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource)
        {
            if (ElementType.RequiresDisposal)
                emitter.Append($"copy{GetStandardIdentifier(irProgram)}({valueCSource})");
            else
                emitter.Append($"marshal{GetStandardIdentifier(irProgram)}({valueCSource}.buffer, {valueCSource}.length)");
        }

        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => EmitCopyValue(irProgram, emitter, valueCSource);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(irProgram, emitter, valueCSource);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) => irBuilder.ScopeForUsedArrayType(this);

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_array_{ElementType.GetStandardIdentifier(irProgram)}";

        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)} {{");
            emitter.AppendLine($"\t{ElementType.GetCName(irProgram)}* buffer;");
            emitter.AppendLine("\tint length;");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}(const {ElementType.GetCName(irProgram)}* buffer, int length) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = malloc(length * sizeof({ElementType.GetCName(irProgram)}));");
            emitter.AppendLine($"\tmemcpy(to_alloc.buffer, buffer, length * sizeof({ElementType.GetCName(irProgram)}));");
            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");

            emitter.AppendLine($"{GetCName(irProgram)} marshal_proto{GetStandardIdentifier(irProgram)}(int length, {ElementType.GetCName(irProgram)} proto) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = malloc(length * sizeof({ElementType.GetCName(irProgram)}));");

            emitter.AppendLine($"\tfor(int i = 0; i < length; i++)");
            emitter.Append("\t\tto_alloc.buffer[i] = ");
            ElementType.EmitCopyValue(irProgram, emitter, "proto");
            emitter.AppendLine(";");

            if (ElementType.RequiresDisposal)
            {
                emitter.Append('\t');
                ElementType.EmitFreeValue(irProgram, emitter, "proto");
            }

            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StringBuilder emitter)
        {
            if (!ElementType.RequiresDisposal)
                return;

            emitter.AppendLine($"void free{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_free) {{");
            emitter.AppendLine("\tfor(int i = 0; i < to_free.length; i++) {{");
            emitter.Append("\t\t");
            ElementType.EmitFreeValue(irProgram, emitter, "to_free.buffer[i]");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\tfree(to_free.buffer);");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StringBuilder emitter)
        {
            if (!ElementType.RequiresDisposal)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} copy{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_copy) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied;");
            emitter.AppendLine("\tcopied.length = to_copy.length;");
            emitter.AppendLine($"\tcopied.buffer = malloc(to_copy.length * sizeof({ElementType.GetCName(irProgram)}));");

            emitter.AppendLine("\tfor(int i = 0; i < to_copy.length; i++)");
            emitter.Append("\t\tcopied.buffer[i] = ");
            ElementType.EmitCopyValue(irProgram, emitter, "to_copy.buffer[i]");
            emitter.AppendLine(";");

            emitter.AppendLine("\treturn copied;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} move{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "(*dest)");
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }
    }
}
