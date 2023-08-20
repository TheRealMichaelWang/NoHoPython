using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<ArrayType> usedArrayTypes = new(new ITypeComparer());
        private HashSet<IType> bufferTypes = new(new ITypeComparer());

        public void ScopeForUsedArrayType(ArrayType toscope)
        {
            if (usedArrayTypes.Contains(toscope))
                return;

            usedArrayTypes.Add(toscope);
            ScopeForUsedBufferType(toscope.ElementType);
        }

        public void ScopeForUsedBufferType(IType bufferType)
        {
            if (bufferTypes.Contains(bufferType))
                return;

            bufferTypes.Add(bufferType);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<ArrayType> usedArrayTypes;
        private List<IType> bufferTypes;

        public void EmitArrayTypeTypedefs(StatementEmitter emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                emitter.AppendLine($"typedef struct {arrayType.GetStandardIdentifier(this)} {arrayType.GetCName(this)};");
        }

        public void EmitArrayTypeCStructs(StatementEmitter emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                arrayType.EmitCStruct(this, emitter);
        }

        public void ForwardDeclareArrayTypes(StatementEmitter emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
            {
                emitter.Append($"{arrayType.GetCName(this)} marshal{arrayType.GetStandardIdentifier(this)}({arrayType.ElementType.GetCName(this)}* buffer, int length");
                if (arrayType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* responsible_destroyer");
                emitter.AppendLine(");");

                emitter.Append($"{arrayType.GetCName(this)} marshal_proto{arrayType.GetStandardIdentifier(this)}(int length, {arrayType.ElementType.GetCName(this)} proto");
                if (arrayType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* responsible_destroyer");
                emitter.AppendLine(");");

                if (!EmitExpressionStatements)
                    emitter.AppendLine($"{arrayType.GetCName(this)} move{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)}* src, {arrayType.GetCName(this)} dest, void* child_agent);");

                if (arrayType.ElementType.RequiresDisposal)
                {
                    emitter.AppendLine($"{arrayType.GetCName(this)} copy{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)} to_copy");
                    if (arrayType.MustSetResponsibleDestroyer)
                        emitter.Append(", void* responsible_destroyer");
                    emitter.Append(");");
                }
            }
        }

        public void EmitArrayTypeMarshallers(StatementEmitter emitter, bool doCallStack)
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
                arrayType.EmitMover(this, emitter);
                arrayType.EmitCopier(this, emitter);
            }
        }

        private void EmitBufferCopiers(StatementEmitter emitter)
        {
            foreach(IType elementType in bufferTypes)
            {
                if (elementType.RequiresDisposal)
                {
                    emitter.Append($"{elementType.GetCName(this)}* buffer_copy_{elementType.GetStandardIdentifier(this)}({elementType.GetCName(this)}* src, int len");
                    if (elementType.MustSetResponsibleDestroyer)
                        emitter.Append(", void* responsible_destroyer");
                    emitter.AppendLine(") {");

                    emitter.AppendLine($"\t{elementType.GetCName(this)}* buf = {MemoryAnalyzer.Allocate($"sizeof({elementType.GetCName(this)}) * len")};");
                    emitter.AppendLine("\tfor(int i = 0; i < len; i++) {");
                    emitter.Append("\t\tbuf[i] = ");
                    elementType.EmitCopyValue(this, emitter, "src[i]", "responsible_destroyer");
                    emitter.AppendLine(";");
                    emitter.AppendLine("\t}");
                    emitter.AppendLine("\treturn buf;");
                    emitter.AppendLine("}");
                }
                
                emitter.Append($"{elementType.GetCName(this)}* buffer_alloc_{elementType.GetStandardIdentifier(this)}({elementType.GetCName(this)} proto, int count");
                if(elementType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* responsible_destroyer");
                emitter.AppendLine(") {");
                emitter.AppendLine($"\t{elementType.GetCName(this)}* buf = {MemoryAnalyzer.Allocate($"count * sizeof({elementType.GetCName(this)})")};");
                emitter.AppendLine("\tfor(int i = 0; i < count; i++) {");
                emitter.Append("\t\tbuf[i] = ");
                elementType.EmitCopyValue(this, emitter, "proto", "responsible_destroyer");
                emitter.AppendLine(";");
                emitter.AppendLine("\t}");

                if (elementType.RequiresDisposal)
                {
                    emitter.Append('\t');
                    elementType.EmitFreeValue(this, emitter, "proto", "NULL");
                    emitter.AppendLine();
                }

                emitter.AppendLine("\treturn buf;");
                emitter.AppendLine("}");
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
        public bool MustSetResponsibleDestroyer => ElementType.MustSetResponsibleDestroyer;
        public bool IsTypeDependency => true;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => true;

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent)
        {
            if(ElementType.RequiresDisposal)
            {
                emitter.Append($"for(int free_i = 0; free_i < {valueCSource}.length; free_i++) {{ ");
                ElementType.EmitFreeValue(irProgram, emitter, $"{valueCSource}.buffer[free_i]", childAgent);
                emitter.Append("}; ");
            }

            emitter.Append($"{irProgram.MemoryAnalyzer.Dealloc($"{valueCSource}.buffer", $"{valueCSource}.length * sizeof({ElementType.GetCName(irProgram)})")};");
        }

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (ElementType.RequiresDisposal)
                emitter.Append($"copy{GetStandardIdentifier(irProgram)}({valueCSource}");
            else
                emitter.Append($"marshal{GetStandardIdentifier(irProgram)}({valueCSource}.buffer, {valueCSource}.length");

            if (MustSetResponsibleDestroyer)
                emitter.Append($", {responsibleDestroyer}");

            emitter.Append(')');
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (irProgram.EmitExpressionStatements)
                IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
            else
                emitter.Append($"move{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource}, {childAgent})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.ScopeForUsedTypes(irBuilder);
            irBuilder.ScopeForUsedArrayType(this);
        }

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_array_{ElementType.GetStandardIdentifier(irProgram)}";

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)} {{");
            emitter.AppendLine($"\t{ElementType.GetCName(irProgram)}* buffer;");
            emitter.AppendLine("\tint length;");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} marshal{GetStandardIdentifier(irProgram)}({ElementType.GetCName(irProgram)}* buffer, int length");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
            emitter.AppendLine($"\tto_alloc.buffer = {irProgram.MemoryAnalyzer.Allocate($"length * sizeof({ElementType.GetCName(irProgram)})")};");
            emitter.AppendLine($"\tmemcpy(to_alloc.buffer, buffer, length * sizeof({ElementType.GetCName(irProgram)}));");
            emitter.AppendLine("\tto_alloc.length = length;");
            
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");

            if (ElementType.RequiresDisposal)
            {
                emitter.Append($"{GetCName(irProgram)} marshal_foreign{GetStandardIdentifier(irProgram)}({ElementType.GetCName(irProgram)}* buffer, int length");

                if (MustSetResponsibleDestroyer)
                    emitter.Append(", void* responsible_destroyer");

                emitter.AppendLine(") {");
                emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");

                emitter.Append($"\tto_alloc.buffer = buffer_copy_{ElementType.GetStandardIdentifier(irProgram)}(buffer, length");
                if (MustSetResponsibleDestroyer)
                    emitter.Append(", responsible_destroyer");
                emitter.AppendLine(");");

                emitter.AppendLine("\tto_alloc.length = length;");
                emitter.AppendLine("\treturn to_alloc;");
                emitter.AppendLine("}");
            }

            emitter.Append($"{GetCName(irProgram)} marshal_proto{GetStandardIdentifier(irProgram)}(int length, {ElementType.GetCName(irProgram)} proto");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_alloc;");
            
            emitter.Append($"\tto_alloc.buffer = buffer_alloc_{ElementType.GetStandardIdentifier(irProgram)}(proto, length");
            if (MustSetResponsibleDestroyer)
                emitter.Append(", responsible_destroyer");
            emitter.AppendLine(");");

            emitter.AppendLine("\tto_alloc.length = length;");
            emitter.AppendLine("\treturn to_alloc;");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!ElementType.RequiresDisposal)
                return;

            emitter.Append($"{GetCName(irProgram)} copy{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_copy");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied;");
            
            emitter.Append($"\tcopied.buffer = buffer_copy_{ElementType.GetStandardIdentifier(irProgram)}(to_copy.buffer, to_copy.length");
            if(MustSetResponsibleDestroyer)
                emitter.Append(", responsible_destroyer");
            emitter.AppendLine(");");

            emitter.AppendLine("\tcopied.length = to_copy.length;");
            emitter.AppendLine("\treturn copied;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src, void* child_agent) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "(*dest)", "child_agent");
            emitter.AppendLine();
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }
    }

    partial class MemorySpan
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => ElementType.MustSetResponsibleDestroyer;
        public bool IsTypeDependency => true;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => ElementType.TypeParameterAffectsCodegen(effectInfo);

        public string GetCName(IRProgram irProgram) => $"{ElementType.GetCName(irProgram)}*";

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_{Identifier}";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent)
        {
            if (ElementType.RequiresDisposal)
            {
                emitter.Append($"for(int free_i = 0; free_i < {Length}; free_i++) {{ ");
                ElementType.EmitFreeValue(irProgram, emitter, $"{valueCSource}[free_i]", childAgent);
                emitter.Append("}; ");
            }

            emitter.Append($"{irProgram.MemoryAnalyzer.Dealloc($"{valueCSource}", $"{Length} * sizeof({ElementType.GetCName(irProgram)})")};");
        }

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (ElementType.RequiresDisposal)
            {
                emitter.Append($"buffer_copy_{ElementType.GetStandardIdentifier(irProgram)}({valueCSource}, {Length}");
                if (ElementType.MustSetResponsibleDestroyer)
                    emitter.Append($", {responsibleDestroyer}");
                emitter.Append(')');
            }
            else
            {
                string size = $"{Length} * sizeof({ElementType.GetCName(irProgram)})";
                emitter.Append($"memcpy({irProgram.MemoryAnalyzer.Allocate(size)}, {valueCSource}, {size})");
            }
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent) => IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.ScopeForUsedTypes(irBuilder);
            irBuilder.ScopeForUsedBufferType(ElementType);
        }

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter) { }
    }
}
