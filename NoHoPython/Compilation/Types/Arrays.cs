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

        public void EmitArrayTypeTypedefs(Emitter emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                emitter.AppendLine($"typedef struct {arrayType.GetStandardIdentifier(this)} {arrayType.GetCName(this)};");
        }

        public void EmitArrayTypeCStructs(Emitter emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
                arrayType.EmitCStruct(this, emitter);
        }

        public void ForwardDeclareArrayTypes(Emitter emitter)
        {
            foreach (ArrayType arrayType in usedArrayTypes)
            {
                emitter.Append($"{arrayType.GetCName(this)} marshal_proto{arrayType.GetStandardIdentifier(this)}(int length, {arrayType.ElementType.GetCName(this)} proto");
                if (arrayType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* responsible_destroyer");
                emitter.AppendLine(");");

                if (arrayType.ElementType.RequiresDisposal)
                {
                    emitter.AppendLine($"{arrayType.GetCName(this)} copy{arrayType.GetStandardIdentifier(this)}({arrayType.GetCName(this)} to_copy");
                    if (arrayType.MustSetResponsibleDestroyer)
                        emitter.Append(", void* responsible_destroyer");
                    emitter.Append(");");
                }
            }
        }

        public void EmitArrayTypeMarshallers(Emitter emitter)
        {
            if (DoBoundsChecking)
            {
                emitter.AppendLine("int nhp_bounds_check(int index, int max_length, const char* src_loc, const char* access_src) {");
                emitter.AppendLine("\tif(index < 0 || index >= max_length) {");

                if (DoCallStack)
                {
                    CallStackReporting.EmitErrorLoc(emitter, "src_loc", "access_src");
                    CallStackReporting.EmitPrintStackTrace(emitter);
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
                arrayType.EmitCopier(this, emitter);
            }
        }

        private void EmitBufferCopiers(Emitter emitter)
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
                    elementType.EmitCopyValue(this, emitter, (e) => e.Append("src[i]"), (e) => e.Append("responsible_destroyer"));
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
                elementType.EmitCopyValue(this, emitter, (e) => e.Append("proto"), (e) => e.Append("responsible_destroyer"));
                emitter.AppendLine(";");
                emitter.AppendLine("\t}");

                if (elementType.RequiresDisposal)
                {
                    emitter.Append('\t');
                    elementType.EmitFreeValue(this, emitter, (e) => e.Append("proto"), Emitter.NullPromise);
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

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            int indirection = emitter.AppendStartBlock();
            emitter.Append($"{GetCName(irProgram)} to_free{indirection} = ");
            valuePromise(emitter);
            emitter.AppendLine(";");
            if(ElementType.RequiresDisposal)
            {
                emitter.AppendStartBlock($"for(int i = 0; i < to_free{indirection}.length; i++)");
                ElementType.EmitFreeValue(irProgram, emitter, (e) => e.Append($"to_free{indirection}.buffer[i]"), childAgent);
                emitter.AppendEndBlock();
            }
            emitter.AppendLine($"{irProgram.MemoryAnalyzer.Dealloc($"to_free{indirection}.buffer", $"to_free{indirection}.length * sizeof({ElementType.GetCName(irProgram)})")}");
            emitter.AppendEndBlock();
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter primaryEmitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)
        {
            primaryEmitter.Append($"copy{GetStandardIdentifier(irProgram)}(");
            valueCSource(primaryEmitter);

            if (MustSetResponsibleDestroyer)
                primaryEmitter.Append($", {responsibleDestroyer}");

            primaryEmitter.Append(')');
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.ScopeForUsedTypes(irBuilder);
            irBuilder.ScopeForUsedArrayType(this);
        }

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public string GetStandardIdentifier(IRProgram irProgram) => $"nhp_array_{ElementType.GetStandardIdentifier(irProgram)}";

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)} {{");
            emitter.AppendLine($"\t{ElementType.GetCName(irProgram)}* buffer;");
            emitter.AppendLine("\tint length;");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, Emitter emitter)
        {
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

        public void EmitCopier(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_copy");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied;");

            if (ElementType.RequiresDisposal)
            {
                emitter.Append($"\tcopied.buffer = buffer_copy_{ElementType.GetStandardIdentifier(irProgram)}(to_copy.buffer, to_copy.length");
                if (MustSetResponsibleDestroyer)
                    emitter.Append(", responsible_destroyer");
                emitter.AppendLine(");");
            }
            else
            {
                string sizeSrc = $"to_copy.length * sizeof({ElementType.GetCName(irProgram)})";
                emitter.AppendLine($"\tcopied.buffer = memcpy({irProgram.MemoryAnalyzer.Allocate(sizeSrc)}, to_copy.buffer, {sizeSrc});");
            }

            emitter.AppendLine("\tcopied.length = to_copy.length;");
            emitter.AppendLine("\treturn copied;");
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

        public string GetStandardIdentifier(IRProgram irProgram) => $"nhp_{Identifier}";

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            if (ElementType.RequiresDisposal)
            {
                int indirection = emitter.AppendStartBlock();
                emitter.Append($"\t{GetCName(irProgram)} to_free{indirection} = ");
                valuePromise(emitter);
                emitter.AppendLine(";");
                emitter.AppendStartBlock($"for(int i = 0; i < to_free{indirection}.length; i++)");
                ElementType.EmitFreeValue(irProgram, emitter, (e) => e.Append($"to_free{indirection}.buffer[i]"), childAgent);
                emitter.AppendEndBlock();
                emitter.Append($"{irProgram.MemoryAnalyzer.Dealloc($"to_free{indirection}.buffer", $"to_free{indirection}.length * sizeof({ElementType.GetCName(irProgram)})")};");
                emitter.AppendEndBlock();
            }
            else
                using(Emitter valueSrcBuffer = new Emitter())
                {
                    valuePromise(valueSrcBuffer);
                    emitter.Append($"{irProgram.MemoryAnalyzer.Dealloc(valueSrcBuffer.GetBuffered(), $"{Length} * sizeof({ElementType.GetCName(irProgram)})")}");
                }
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter primaryEmitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)
        {
            if (ElementType.RequiresDisposal)
            {
                primaryEmitter.Append($"buffer_copy_{ElementType.GetStandardIdentifier(irProgram)}(");
                valueCSource(primaryEmitter);
                primaryEmitter.Append($", {Length}");
                if (ElementType.MustSetResponsibleDestroyer)
                {
                    primaryEmitter.Append(", ");
                    responsibleDestroyer(primaryEmitter);
                }
                primaryEmitter.Append(')');
            }
            else
            {
                string sizeSrc = $"{Length} * sizeof({ElementType.GetCName(irProgram)})";
                primaryEmitter.Append($"memcpy({irProgram.MemoryAnalyzer.Allocate(sizeSrc)}, ");
                valueCSource(primaryEmitter);
                primaryEmitter.Append($", {sizeSrc})");
            }
        }
         
        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.ScopeForUsedTypes(irBuilder);
            irBuilder.ScopeForUsedBufferType(ElementType);
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter) { }
    }
}
