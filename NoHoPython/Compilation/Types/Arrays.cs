using NoHoPython.Compilation;
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

        public void ForwardDeclareArrayTypes(IRProgram irProgram, StringBuilder emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
            {
                emitter.AppendLine($"{arrayType.GetCName(this)} marshal{arrayType.GetStandardIdentifier(this)}(const {arrayType.ElementType.GetCName(this)}* buffer, int length, void* responsible_destroyer);");
                emitter.AppendLine($"{arrayType.GetCName(this)} marshal_proto{arrayType.GetStandardIdentifier(this)}(int length, {arrayType.ElementType.GetCName(this)} proto, void* responsible_destroyer);");
                emitter.AppendLine($"{arrayType.GetCName(irProgram)} change_resp_owner{arrayType.GetStandardIdentifier(irProgram)}({arrayType.GetCName(irProgram)} array, void* responsible_destroyer);");

                if (!irProgram.EmitExpressionStatements)
                    emitter.AppendLine($"{arrayType.GetCName(this)} move{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)}* src, {arrayType.GetCName(this)} dest);");

                if (arrayType.ElementType.RequiresDisposal)
                {
                    emitter.AppendLine($"void free{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)} to_free);");
                    emitter.AppendLine($"{arrayType.GetCName(this)} copy{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)} to_copy, void* responsible_destroyer);");
                }
            }
        }

        public void EmitArrayTypeMarshallers(StringBuilder emitter, bool doCallStack)
        {
            if (DoBoundsChecking)
            {
                emitter.AppendLine("int _nhp_bounds_check(int index, int max_length, const char* src_loc, const char* access_src) {");
                emitter.AppendLine("\tif(index < 0 || index >= max_length) {");

                if (doCallStack)
                {
                    CallStackReporting.EmitErrorLoc(emitter, "src_loc", "access_src", 2);
                    CallStackReporting.EmitPrintStackTrace(emitter, 2);
                    emitter.AppendLine("\t\tprintf(\"IndexError: Index was %i, length was %i.\\n\", index, max_length);");
                }
                else
                {
                    emitter.AppendLine("\t\tprintf(\"Index out of bounds, %s. Index was %i, alloced length was %i.\\n\\t\", src_loc, index, max_length);");
                    emitter.AppendLine("\t\tputs(access_src);");
                }

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
                arrayType.EmitResponsibleDestroyerMutator(this, emitter);
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
                emitter.AppendLine($"{irProgram.MemoryAnalyzer.Dealloc($"{valueCSource}.buffer", $"{valueCSource}.length * sizeof({ElementType.GetCName(irProgram)})")};");
        }

        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer)
        {
            if (ElementType.RequiresDisposal)
                emitter.Append($"copy{GetStandardIdentifier(irProgram)}({valueCSource}, {responsibleDestroyer})");
            else
                emitter.Append($"marshal{GetStandardIdentifier(irProgram)}({valueCSource}.buffer, {valueCSource}.length, {responsibleDestroyer})");
        }

        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource)
        {
            if (irProgram.EmitExpressionStatements)
                IType.EmitMoveExpressionStatement(this, irProgram, emitter, destC, valueCSource);
            else
                emitter.Append($"move{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, $"{recordCSource}->_nhp_responsible_destroyer");

        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => emitter.Append($"change_resp_owner{GetStandardIdentifier(irProgram)}({valueCSource}, {newResponsibleDestroyer})");
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
            emitter.AppendLine("\tvoid* responsible_destroyer;");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}(const {ElementType.GetCName(irProgram)}* buffer, int length, void* responsible_destroyer) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = {irProgram.MemoryAnalyzer.Allocate($"length * sizeof({ElementType.GetCName(irProgram)})")};");
            emitter.AppendLine($"\tmemcpy(to_alloc.buffer, buffer, length * sizeof({ElementType.GetCName(irProgram)}));");
            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\tto_alloc.responsible_destroyer = responsible_destroyer;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");

            if (ElementType.RequiresDisposal)
            {
                emitter.AppendLine($"{GetCName(irProgram)} marshal_foreign{GetStandardIdentifier(irProgram)}(const {ElementType.GetCName(irProgram)}* buffer, int length, void* responsible_destroyer) {{");
                emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
                emitter.AppendLine($"\tto_alloc.buffer = {irProgram.MemoryAnalyzer.Allocate($"length * sizeof({ElementType.GetCName(irProgram)})")};");
                emitter.AppendLine("\tfor(int i = 0; i < length; i++) {");
                emitter.AppendLine("\t\tto_alloc.buffer[i] = ");
                ElementType.EmitCopyValue(irProgram, emitter, "buffer[i]", "responsible_destroyer");
                emitter.AppendLine(";");
                emitter.AppendLine("\t}");
                emitter.AppendLine("\tto_alloc.length = length;");
                emitter.AppendLine("\tto_alloc.responsible_destroyer = responsible_destroyer;");
                emitter.AppendLine("\treturn to_alloc;");
                emitter.AppendLine("}");
            }

            emitter.AppendLine($"{GetCName(irProgram)} marshal_proto{GetStandardIdentifier(irProgram)}(int length, {ElementType.GetCName(irProgram)} proto, void* responsible_destroyer) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = {irProgram.MemoryAnalyzer.Allocate($"length * sizeof({ElementType.GetCName(irProgram)})")};");

            emitter.AppendLine($"\tfor(int i = 0; i < length; i++)");
            emitter.Append("\t\tto_alloc.buffer[i] = ");
            ElementType.EmitCopyValue(irProgram, emitter, "proto", "responsible_destroyer");
            emitter.AppendLine(";");

            if (ElementType.RequiresDisposal)
            {
                emitter.Append('\t');
                ElementType.EmitFreeValue(irProgram, emitter, "proto");
            }

            emitter.AppendLine("\tto_alloc.responsible_destroyer = responsible_destroyer;");
            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StringBuilder emitter)
        {
            if (!ElementType.RequiresDisposal)
                return;

            emitter.AppendLine($"void free{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_free) {{");
            emitter.AppendLine("\tfor(int i = 0; i < to_free.length; i++) {");
            emitter.Append("\t\t");
            ElementType.EmitFreeValue(irProgram, emitter, "to_free.buffer[i]");
            emitter.AppendLine("\t}");
            emitter.AppendLine($"\t{irProgram.MemoryAnalyzer.Dealloc("to_free.buffer", $"to_free.length * sizeof({ElementType.GetCName(irProgram)})")};");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StringBuilder emitter)
        {
            if (!ElementType.RequiresDisposal)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} copy{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_copy, void* responsible_destroyer) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied;");
            emitter.AppendLine("\tcopied.length = to_copy.length;");
            emitter.AppendLine("\tcopied.responsible_destroyer = responsible_destroyer;");
            emitter.AppendLine($"\tcopied.buffer = {irProgram.MemoryAnalyzer.Allocate($"to_copy.length * sizeof({ElementType.GetCName(irProgram)})")};");

            emitter.AppendLine("\tfor(int i = 0; i < to_copy.length; i++) {");
            emitter.Append("\t\tcopied.buffer[i] = ");
            ElementType.EmitCopyValue(irProgram, emitter, "to_copy.buffer[i]", "responsible_destroyer");
            emitter.AppendLine(";");
            emitter.AppendLine("\t}");

            emitter.AppendLine("\treturn copied;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StringBuilder emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "(*dest)");
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }

        public void EmitResponsibleDestroyerMutator(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} change_resp_owner{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} array, void* responsible_destroyer) {{");
            emitter.AppendLine("\tarray.responsible_destroyer = responsible_destroyer;");
            emitter.AppendLine("\tfor(int i = 0; i < array.length; i++) {");
            emitter.Append("\t\tarray.buffer[i] = ");
            ElementType.EmitMutateResponsibleDestroyer(irProgram, emitter, "array.buffer[i]", "responsible_destroyer");
            emitter.AppendLine(";");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn array;");
            emitter.AppendLine("}");
        }
    }
}
