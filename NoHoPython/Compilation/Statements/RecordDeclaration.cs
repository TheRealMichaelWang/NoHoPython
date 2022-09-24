using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Typing
{
    partial class RecordType
    {
        public bool RequiresDisposal => true;

        public string GetStandardIdentifier() => $"_nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier()))}_";

        public string GetCName() => $"{GetStandardIdentifier()}_t*";
        public string GetCHeapSizer() => $"sizeof({GetStandardIdentifier()}_t)";

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => emitter.AppendLine($"free_record{GetStandardIdentifier()}({valueCSource});");
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append($"copy_record{GetStandardIdentifier()}({valueCSource})");
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move_record(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => emitter.Append($"borrow_record{GetStandardIdentifier()}({valueCSource})");
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(emitter, valueCSource);

        public void EmitGetProperty(StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}->{property.Name}");

        public void ScopeForUsedTypes()
        {
            if (RecordDeclaration.DeclareUsedRecordType(this)) 
                foreach (var property in properties.Value)
                {
                    property.Type.ScopeForUsedTypes();
                    if (property.DefaultValue != null)
                        property.DefaultValue.ScopeForUsedTypes(new Dictionary<TypeParameter, IType>());
                }
        }

        public void EmitCStruct(StringBuilder emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier() + " {");
            foreach (var property in properties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName()} {property.Name};");
            emitter.AppendLine("\tint ref_count;");
            emitter.AppendLine("\tint min_refs;");
            emitter.AppendLine("};");
        }

        public void EmitConstructorCHeader(StringBuilder emitter)
        {
            if (HasProperty("__init__") && FindProperty("__init__").Type is ProcedureType constructorType)
                emitter.AppendLine($"{GetCName()} construct_{GetStandardIdentifier()}({string.Join(", ", constructorType.ParameterTypes.ConvertAll((arg) => $"{arg.GetCName()} {arg.GetCName()}_param"))});");
            else
                emitter.AppendLine($"{GetCName()} construct_{GetStandardIdentifier()}();");
        }

        public void EmitConstructor(StringBuilder emitter)
        {
            void emitInitializer()
            {
                emitter.AppendLine($"\t{GetCName()} _nhp_self = malloc({GetCHeapSizer()});");
                emitter.AppendLine("\t_nhp_self->ref_count = 0;");
                foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                {
                    emitter.Append($"\t_nhp_self->{recordProperty.Name} = ");
                    
                    if(recordProperty.DefaultValue.RequiresDisposal(new Dictionary<TypeParameter, IType>()))
                        recordProperty.DefaultValue.Emit(emitter, new Dictionary<TypeParameter, IType>());
                    else
                    {
                        StringBuilder valueBuilder = new StringBuilder();
                        recordProperty.DefaultValue.Emit(valueBuilder, new Dictionary<TypeParameter, IType>());
                        recordProperty.Type.EmitCopyValue(emitter, valueBuilder.ToString());
                    }
                    
                    emitter.AppendLine(";");
                }
            }

            if (HasProperty("__init__") && FindProperty("__init__").Type is ProcedureType constructorType)
            {
                emitter.AppendLine($"{GetCName()} construct_{GetStandardIdentifier()}({string.Join(", ", constructorType.ParameterTypes.ConvertAll((arg) => $"{arg.GetCName()} {arg.GetStandardIdentifier()}_param"))}) {{");
                emitInitializer();

                emitter.Append("\t_nhp_self->__init__->_nhp_this_anon(_nhp_self->__init__");
                foreach(IType arg in constructorType.ParameterTypes)
                    emitter.Append($", {arg.GetStandardIdentifier()}_param");
                emitter.AppendLine(");");
            }
            else
            {
                emitter.AppendLine($"{GetCName()} construct_{GetStandardIdentifier()}() {{");
                emitInitializer();
            }
            emitter.AppendLine("\t_nhp_self->min_refs = _nhp_self->ref_count;");
            emitter.AppendLine("\treturn _nhp_self;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(StringBuilder emitter)
        {
            emitter.AppendLine($"void free_record{GetStandardIdentifier()}({GetCName()} record) {{");

            emitter.AppendLine("\tif(record->ref_count == record->min_refs) {");
            emitter.AppendLine("\t\trecord->ref_count--;");
            emitter.AppendLine("\t\treturn;");
            emitter.AppendLine("\t}");

            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
            {
                if (recordProperty.Type.RequiresDisposal)
                {
                    emitter.Append('\t');
                    recordProperty.Type.EmitFreeValue(emitter, $"record->{recordProperty.Name}");
                }
            }

            emitter.AppendLine("}");
        }

        public void EmitCopier(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} copy_record{GetStandardIdentifier()}({GetCName()} record) {{");
            emitter.AppendLine($"\t{GetCName()} copied_record = malloc({GetCHeapSizer()});");
            emitter.AppendLine("\tcopied_record->ref_count = 0;");

            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
            {
                emitter.Append($"\tcopied_record->{recordProperty.Name} = ");
                recordProperty.Type.EmitRecordCopyValue(emitter, $"record->{recordProperty.Name}", "copied_record");
                emitter.AppendLine(";");
            }

            emitter.AppendLine("\tcopied_record->min_refs = copied_record->ref_count;");
            emitter.AppendLine("\treturn copied_record;");
            emitter.AppendLine("}");
        }

        public void EmitMover(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} move_record{GetStandardIdentifier()}({GetCName()}* dest, {GetCName()} src) {{");
            emitter.Append('\t');
            EmitFreeValue(emitter, "*dest");
            emitter.AppendLine("\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }

        public void EmitBorrower(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} borrow_record{GetStandardIdentifier()}({GetCName()} record) {{");
            emitter.AppendLine("\trecord->ref_count++;");
            emitter.AppendLine("\treturn record;");
            emitter.AppendLine("}");
        }
    }
}


namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class RecordDeclaration
    {
        private static List<RecordType> usedRecordTypes = new List<RecordType>();
        private static Dictionary<RecordDeclaration, List<RecordType>> recordTypeOverloads = new Dictionary<RecordDeclaration, List<RecordType>>();

        public static bool DeclareUsedRecordType(RecordType recordType)
        {
            foreach (RecordType usedRecord in usedRecordTypes)
                if (recordType.IsCompatibleWith(usedRecord))
                    return false;
            
            usedRecordTypes.Add(recordType);
            if (!recordTypeOverloads.ContainsKey(recordType.RecordPrototype))
                recordTypeOverloads.Add(recordType.RecordPrototype, new List<RecordType>());
            recordTypeOverloads[recordType.RecordPrototype].Add(recordType);
            
            return true;
        }

        public static void ForwardDeclareRecordTypes(StringBuilder emitter)
        {
            foreach (RecordType recordType in usedRecordTypes)
                emitter.AppendLine($"typedef struct {recordType.GetStandardIdentifier()} {recordType.GetStandardIdentifier()}_t;");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        public void ForwardDeclareType(StringBuilder emitter) 
        {
            if (!recordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in recordTypeOverloads[this])
                recordType.EmitCStruct(emitter);
        }

        public void ForwardDeclare(StringBuilder emitter)
        {
            if (!recordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in recordTypeOverloads[this]) 
            {
                recordType.EmitConstructorCHeader(emitter);

                emitter.AppendLine($"void free_record{recordType.GetStandardIdentifier()}({recordType.GetCName()} record);");
                emitter.AppendLine($"{recordType.GetCName()} copy_record{recordType.GetStandardIdentifier()}({recordType.GetCName()} record);");
                emitter.AppendLine($"{recordType.GetCName()} move_record{recordType.GetStandardIdentifier()}({recordType.GetCName()}* dest, {recordType.GetCName()} src);"); 
                emitter.AppendLine($"{recordType.GetCName()} borrow_record{recordType.GetStandardIdentifier()}({recordType.GetCName()} record);");
            }
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!recordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in recordTypeOverloads[this])
            {
                recordType.EmitConstructor(emitter);
                recordType.EmitDestructor(emitter);
                recordType.EmitCopier(emitter);
                recordType.EmitMover(emitter);
                recordType.EmitBorrower(emitter);
            }
        }
    }
}