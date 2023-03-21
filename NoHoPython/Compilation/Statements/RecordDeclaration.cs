using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<RecordType> usedRecordTypes = new(new ITypeComparer());
        private Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads = new();

        public bool DeclareUsedRecordType(RecordType recordType)
        {
            if (usedRecordTypes.Contains(recordType))
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

        public void ForwardDeclareRecordTypes(StatementEmitter emitter)
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
        partial class RecordProperty
        {
            public override void EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource) => propertyContainer.EmitGetProperty(irProgram, emitter, valueCSource, Name);
        }

        public static void EmitRecordMaskProto(StatementEmitter emitter)
        {
            emitter.AppendLine("typedef struct _nhp_std_record_mask _nhp_std_record_mask_t;");
            emitter.AppendLine("struct _nhp_std_record_mask {");
            emitter.AppendLine("\tint _nhp_ref_count;");
            emitter.AppendLine("\tint _nhp_master_count;");
            emitter.AppendLine("\tint _nhp_lock;");
            emitter.AppendLine($"\t{RecordType.StandardRecordMask} parent_record;");
            emitter.AppendLine("} _nhp_std_record_mask;");
        }

        public static void EmitRecordChildFinder(StatementEmitter emitter)
        {
            emitter.AppendLine("static int _nhp_record_has_child(_nhp_std_record_mask_t* parent, _nhp_std_record_mask_t* child) {");
            emitter.AppendLine("\twhile(child != NULL) {");
            emitter.AppendLine("\t\tif(parent == child)");
            emitter.AppendLine("\t\t\treturn 1;");
            emitter.AppendLine("\t\tchild = child->parent_record;");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn 0;");
            emitter.AppendLine("}");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StatementEmitter emitter) 
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                recordType.EmitCStruct(irProgram, emitter);
        }

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in irProgram.RecordTypeOverloads[this]) 
            {
                recordType.EmitConstructorCHeader(irProgram, emitter);
                emitter.AppendLine(";");

                emitter.AppendLine($"void free_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record, {RecordType.StandardRecordMask} child_agent);");
                emitter.AppendLine($"{recordType.GetCName(irProgram)} borrow_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record, void* responsible_destroyer);");

                if (!recordType.HasCopier)
                    emitter.AppendLine($"{recordType.GetCName(irProgram)} copy_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record, {RecordType.StandardRecordMask} parent_record);");
                if (!irProgram.EmitExpressionStatements)
                    emitter.AppendLine($"{recordType.GetCName(irProgram)} move_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)}* dest, {recordType.GetCName(irProgram)} src, void* child_agent);");
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
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
        public bool MustSetResponsibleDestroyer => true;

        public bool HasDestructor => HasProperty("__del__");
        public bool HasCopier => HasProperty("__copy__");

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t*";
        public string GetCHeapSizer(IRProgram irProgram) => $"sizeof({GetStandardIdentifier(irProgram)}_t)";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) => emitter.Append($"free_record{GetStandardIdentifier(irProgram)}({valueCSource}, ({StandardRecordMask}){childAgent});");

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (HasCopier)
            {
                ProcedureType copierType = (ProcedureType)FindProperty("__copy__").Type;
                emitter.Append("((");
                emitter.Append(copierType.GetStandardIdentifier(irProgram));
                emitter.Append($"_t){valueCSource}->__copy__->_nhp_this_anon)({valueCSource}->__copy__, {responsibleDestroyer})");
            }
            else
                emitter.Append($"copy_record{GetStandardIdentifier(irProgram)}({valueCSource}, ({StandardRecordMask}){responsibleDestroyer})");
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (irProgram.EmitExpressionStatements)
                IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
            else
                emitter.Append($"move_record(&{destC}, {valueCSource}, {childAgent})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => emitter.Append($"borrow_record{GetStandardIdentifier(irProgram)}({valueCSource}, ({StandardRecordMask}){responsibleDestroyer})");
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void EmitGetProperty(IRProgram irProgram, IEmitter emitter, string valueCSource, string propertyIdentifier) => emitter.Append($"{valueCSource}->{propertyIdentifier}");

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

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            emitter.AppendLine("\tint _nhp_ref_count;");
            emitter.AppendLine("\tint _nhp_master_count;");
            emitter.AppendLine("\tint _nhp_lock;");
            emitter.AppendLine($"\t{StandardRecordMask} parent_record;");
            foreach (var property in properties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitConstructorCHeader(IRProgram irProgram, StatementEmitter emitter)
        {
            ProcedureType constructorType = (ProcedureType)FindProperty("__init__").Type;

            emitter.Append($"{GetCName(irProgram)} construct_{GetStandardIdentifier(irProgram)}(");
            for (int i = 0; i < constructorType.ParameterTypes.Count; i++)
                emitter.Append($"{constructorType.ParameterTypes[i].GetCName(irProgram)} param{i}, ");
            emitter.Append($"{StandardRecordMask} parent_record)");
        }

        public void EmitConstructor(IRProgram irProgram, StatementEmitter emitter)
        {
            ProcedureType constructorType = (ProcedureType)FindProperty("__init__").Type;
            EmitConstructorCHeader(irProgram, emitter);
            emitter.AppendLine(" {"); 
            
            emitter.AppendLine($"\t{GetCName(irProgram)} _nhp_self = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            emitter.AppendLine("\t_nhp_self->_nhp_ref_count = 0;");
            emitter.AppendLine("\t_nhp_self->_nhp_master_count = 0;");
            emitter.AppendLine("\t_nhp_self->_nhp_lock = 0;");
            emitter.AppendLine("\t_nhp_self->parent_record = parent_record;");
            
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                if (recordProperty.DefaultValue != null)
                {
                    emitter.Append($"\t_nhp_self->{recordProperty.Name} = ");
                    if (recordProperty.DefaultValue.RequiresDisposal(new Dictionary<TypeParameter, IType>()))
                        recordProperty.DefaultValue.Emit(irProgram, emitter, new Dictionary<TypeParameter, IType>(), "_nhp_self");
                    else
                        recordProperty.Type.EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(recordProperty.DefaultValue, irProgram, new(), "NULL"), "_nhp_self");
                    emitter.AppendLine(";");
                }

            emitter.Append($"\t(({constructorType.GetStandardIdentifier(irProgram)}_t)_nhp_self->__init__->_nhp_this_anon)(_nhp_self->__init__");
            for (int i = 0; i < constructorType.ParameterTypes.Count; i++)
                emitter.Append($", param{i}");
            emitter.AppendLine(");");
            
            emitter.AppendLine("\treturn _nhp_self;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine($"void free_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, {StandardRecordMask} child_agent) {{");

            emitter.AppendLine($"\tif(record->_nhp_lock || _nhp_record_has_child(({StandardRecordMask})record, child_agent))");
            emitter.AppendLine("\t\treturn;");

            emitter.AppendLine("\tif(record->_nhp_ref_count) {");
            emitter.AppendLine($"\t\tif(_nhp_record_has_child(child_agent, ({StandardRecordMask})record)) {{");
            emitter.AppendLine("\t\t\tif(record->_nhp_master_count == 0)");
            emitter.AppendLine("\t\t\t\trecord->parent_record = NULL;");
            emitter.AppendLine("\t\t\telse");
            emitter.AppendLine("\t\t\t\trecord->_nhp_master_count--;");
            emitter.AppendLine("\t\t}");
            emitter.AppendLine("\t\trecord->_nhp_ref_count--;");
            emitter.AppendLine("\t\treturn;");
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\trecord->_nhp_lock = 1;");
            if (HasDestructor)
            {
                ProcedureType destructorType = (ProcedureType)FindProperty("__del__").Type;
                emitter.AppendLine($"\t(({destructorType.GetStandardIdentifier(irProgram)}_t)record->__del__->_nhp_this_anon)(record->__del__);");
            }
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
            {
                if (recordProperty.Type.RequiresDisposal)
                {
                    emitter.Append('\t');
                    recordProperty.Type.EmitFreeValue(irProgram, emitter, $"record->{recordProperty.Name}", "record");
                    emitter.AppendLine();
                }
            }
            emitter.AppendLine($"\t{irProgram.MemoryAnalyzer.Dealloc("record", GetCHeapSizer(irProgram))};");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StatementEmitter emitter)
        {
            if (HasCopier)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} copy_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, {StandardRecordMask} parent_record) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_record = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            emitter.AppendLine("\tcopied_record->_nhp_ref_count = 0;");
            emitter.AppendLine("\tcopied_record->_nhp_master_count = 0;");
            emitter.AppendLine("\tcopied_record->_nhp_lock = 0;");
            emitter.AppendLine("\tcopied_record->parent_record = parent_record;");

            foreach (RecordDeclaration.RecordProperty property in properties.Value)
            {
                emitter.Append($"\tcopied_record->{property.Name} = ");
                property.Type.EmitRecordCopyValue(irProgram, emitter, $"record->{property.Name}", "copied_record");
                emitter.AppendLine(";");
            }

            emitter.AppendLine("\treturn copied_record;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src, void* child_agent) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "*dest", "child_agent");
            emitter.AppendLine();
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }

        public void EmitBorrower(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} borrow_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, void* responsible_destroyer) {{");

            emitter.AppendLine($"\tif(!_nhp_record_has_child(({StandardRecordMask})record, responsible_destroyer)) {{");
            emitter.AppendLine("\t\trecord->_nhp_ref_count++;");
            emitter.AppendLine($"\t\tif(_nhp_record_has_child(({StandardRecordMask})responsible_destroyer, ({StandardRecordMask})record))");
            emitter.AppendLine("\t\t\trecord->_nhp_master_count++;");
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\treturn record;");
            emitter.AppendLine("}");
        }
    }
}