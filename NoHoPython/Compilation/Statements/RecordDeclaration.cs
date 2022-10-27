using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private List<RecordType> usedRecordTypes = new();
        private Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads = new();

        public bool DeclareUsedRecordType(RecordType recordType)
        {
            foreach (RecordType usedRecord in usedRecordTypes)
                if (recordType.IsCompatibleWith(usedRecord))
                    return false;

            usedRecordTypes.Add(recordType);
            if (!recordTypeOverloads.ContainsKey(recordType.RecordPrototype))
                recordTypeOverloads.Add(recordType.RecordPrototype, new List<RecordType>());
            recordTypeOverloads[recordType.RecordPrototype].Add(recordType);

            typeDependencyTree.Add(recordType, new HashSet<IType>(recordType.GetProperties().ConvertAll((prop) => prop.Type).Where((type) => type is not RecordType), new ITypeComparer()));

            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<RecordType> usedRecordTypes;
        public readonly Dictionary<RecordDeclaration, List<RecordType>> RecordTypeOverloads;

        public void ForwardDeclareRecordTypes(StringBuilder emitter)
        {
            foreach (RecordType recordType in usedRecordTypes)
                emitter.AppendLine($"typedef struct {recordType.GetStandardIdentifier(this)} {recordType.GetStandardIdentifier(this)}_t;");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class RecordDeclaration
    {
        public static void EmitRecordMaskProto(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine("typedef struct _nhp_std_record_mask {");
            emitter.AppendLine("\tint _nhp_ref_count;");
            emitter.AppendLine("\tint _nhp_freeing;");
            emitter.AppendLine("\tvoid* _nhp_responsible_destroyer;");
            emitter.AppendLine("} _nhp_std_record_mask_t;");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) 
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                recordType.EmitCStruct(irProgram, emitter);
        }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in irProgram.RecordTypeOverloads[this]) 
            {
                recordType.EmitConstructorCHeader(irProgram, emitter);
                emitter.AppendLine(";");

                emitter.AppendLine($"void free_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record);");
                emitter.AppendLine($"{recordType.GetCName(irProgram)} borrow_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record, void* responsible_destroyer);");
                emitter.AppendLine($"{recordType.GetCName(irProgram)} change_resp_owner{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record, void* responsible_destroyer);");

                if (!recordType.HasCopier)
                    emitter.AppendLine($"{recordType.GetCName(irProgram)} copy_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record, void* responsible_destroyer);");
                if (!irProgram.EmitExpressionStatements)
                    emitter.AppendLine($"{recordType.GetCName(irProgram)} move_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)}* dest, {recordType.GetCName(irProgram)} src);");
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
            {
                recordType.EmitConstructor(irProgram, emitter);
                recordType.EmitDestructor(irProgram, emitter);
                recordType.EmitCopier(irProgram, emitter);
                recordType.EmitMover(irProgram, emitter);
                recordType.EmitBorrower(irProgram, emitter);
                recordType.EmitResponsibleDestroyerMutator(irProgram, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class RecordType
    {
        public static string StandardRecordMask => "_nhp_std_record_mask_t*";

        public bool RequiresDisposal => true;
        public bool HasDestructor => HasProperty("__del__");
        public bool HasCopier => HasProperty("__copy__");

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t*";
        public string GetCHeapSizer(IRProgram irProgram) => $"sizeof({GetStandardIdentifier(irProgram)}_t)";

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.AppendLine($"free_record{GetStandardIdentifier(irProgram)}({valueCSource});");

        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer)
        {
            if (HasCopier)
            {
                StringBuilder valueBuilder = new();
                valueBuilder.Append($"{valueCSource}->__copy__->_nhp_this_anon({valueCSource}->__copy__)");
                EmitMutateResponsibleDestroyer(irProgram, emitter, valueBuilder.ToString(), responsibleDestroyer);
            }
            else
                emitter.Append($"copy_record{GetStandardIdentifier(irProgram)}({valueCSource}, {responsibleDestroyer})");
        }

        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource)
        {
            if (irProgram.EmitExpressionStatements)
                IType.EmitMoveExpressionStatement(this, irProgram, emitter, destC, valueCSource);
            else
                emitter.Append($"move_record(&{destC}, {valueCSource})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => emitter.Append($"borrow_record{GetStandardIdentifier(irProgram)}({valueCSource}, {responsibleDestroyer})");
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, $"{recordCSource}->_nhp_responsible_destroyer");

        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => emitter.Append($"change_resp_owner{GetStandardIdentifier(irProgram)}({valueCSource}, {newResponsibleDestroyer})");

        public void EmitGetProperty(IRProgram irProgram, StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}->{property.Name}");

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedRecordType(this)) 
                foreach (var property in properties.Value)
                {
                    property.Type.ScopeForUsedTypes(irBuilder);
                    if (property.DefaultValue != null)
                        property.DefaultValue.ScopeForUsedTypes(new Dictionary<TypeParameter, IType>(), irBuilder);
                }
        }

        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            emitter.AppendLine("\tint _nhp_ref_count;");
            emitter.AppendLine("\tint _nhp_freeing;");
            emitter.AppendLine("\tvoid* _nhp_responsible_destroyer;");
            foreach (var property in properties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitConstructorCHeader(IRProgram irProgram, StringBuilder emitter)
        {
            ProcedureType constructorType = (ProcedureType)FindProperty("__init__").Type;
            emitter.Append($"{GetCName(irProgram)} construct_{GetStandardIdentifier(irProgram)}(");
            for (int i = 0; i < constructorType.ParameterTypes.Count; i++)
                emitter.Append($"{constructorType.ParameterTypes[i].GetCName(irProgram)} param{i}, ");
            emitter.Append("void* responsible_destroyer)");
        }

        public void EmitConstructor(IRProgram irProgram, StringBuilder emitter)
        {
            ProcedureType constructorType = (ProcedureType)FindProperty("__init__").Type;
            EmitConstructorCHeader(irProgram, emitter);
            emitter.AppendLine(" {"); 
            
            emitter.AppendLine($"\t{GetCName(irProgram)} _nhp_self = {irProgram.MemoryAnalyzer.Allocater}({GetCHeapSizer(irProgram)});");
            emitter.AppendLine("\t_nhp_self->_nhp_ref_count = 0;");
            emitter.AppendLine("\t_nhp_self->_nhp_freeing = 0;");
            emitter.AppendLine("\t_nhp_self->_nhp_responsible_destroyer = (responsible_destroyer ? responsible_destroyer : _nhp_self);");
            
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                if (recordProperty.DefaultValue != null)
                {
                    emitter.Append($"\t_nhp_self->{recordProperty.Name} = ");
                    if (recordProperty.DefaultValue.RequiresDisposal(new Dictionary<TypeParameter, IType>()))
                        recordProperty.DefaultValue.Emit(irProgram, emitter, new Dictionary<TypeParameter, IType>(), "_nhp_self->_nhp_responsible_destroyer");
                    else
                    {
                        StringBuilder valueBuilder = new StringBuilder();
                        recordProperty.DefaultValue.Emit(irProgram, valueBuilder, new Dictionary<TypeParameter, IType>(), "NULL");
                        recordProperty.Type.EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), "_nhp_self->_nhp_responsible_destroyer");
                    }
                    emitter.AppendLine(";");
                }

            emitter.Append("\t_nhp_self->__init__->_nhp_this_anon(_nhp_self->__init__");
            for (int i = 0; i < constructorType.ParameterTypes.Count; i++)
                emitter.Append($", param{i}");
            emitter.AppendLine(");");
            
            emitter.AppendLine("\treturn _nhp_self;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"void free_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record) {{");

            emitter.AppendLine("\tif(record->_nhp_freeing)");
            emitter.AppendLine("\t\treturn;");

            emitter.AppendLine("\tif(record->_nhp_ref_count) {");
            emitter.AppendLine("\t\trecord->_nhp_ref_count--;");
            emitter.AppendLine("\t\treturn;");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\trecord->_nhp_freeing = 1;");

            if (HasDestructor)
                emitter.AppendLine("\trecord->__del__->_nhp_this_anon(record->__del__);");

            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
            {
                if (recordProperty.Type.RequiresDisposal)
                {
                    emitter.Append('\t');
                    recordProperty.Type.EmitFreeValue(irProgram, emitter, $"record->{recordProperty.Name}");
                }
            }
            emitter.AppendLine($"\t{irProgram.MemoryAnalyzer.Disposer}(record);");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StringBuilder emitter)
        {
            if (HasCopier)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} copy_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, void* responsible_destroyer) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_record = {irProgram.MemoryAnalyzer.Allocater}({GetCHeapSizer(irProgram)});");
            emitter.AppendLine("\tcopied_record->_nhp_ref_count = 0;");
            emitter.AppendLine("\tcopied_record->_nhp_freeing = 0;");
            emitter.AppendLine("\tcopied_record->_nhp_responsible_destroyer = (responsible_destroyer ? responsible_destroyer : copied_record);");

            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
            {
                emitter.Append($"\tcopied_record->{recordProperty.Name} = ");
                recordProperty.Type.EmitRecordCopyValue(irProgram, emitter, $"record->{recordProperty.Name}", "copied_record");
                emitter.AppendLine(";");
            }

            emitter.AppendLine("\treturn copied_record;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StringBuilder emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "*dest");
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }

        public void EmitBorrower(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} borrow_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, void* responsible_destroyer) {{");
            emitter.AppendLine("\tif(record->_nhp_responsible_destroyer != responsible_destroyer) {");
            emitter.AppendLine("\t\trecord->_nhp_ref_count++;");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn record;");
            emitter.AppendLine("}");
        }

        public void EmitResponsibleDestroyerMutator(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} change_resp_owner{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, void* responsible_destroyer) {{");
            emitter.AppendLine("\trecord->_nhp_responsible_destroyer = (responsible_destroyer ? responsible_destroyer : record);");
            foreach (RecordDeclaration.RecordProperty property in properties.Value)
            {
                emitter.Append($"\trecord->{property.Name} = ");
                property.Type.EmitMutateResponsibleDestroyer(irProgram, emitter, $"record->{property.Name}", "record->_nhp_responsible_destroyer");
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn record;");
            emitter.AppendLine("}");
        }
    }
}