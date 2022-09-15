using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

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
                emitter.AppendLine($"void{recordType.GetCName()} init_record(");
 

                emitter.AppendLine($"void free_record{recordType.GetStandardIdentifier()}({recordType.GetCName()} record);");
                emitter.AppendLine($"{recordType.GetCName()} copy_record{recordType.GetStandardIdentifier()}({recordType.GetCName()} record);");
            }
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!recordTypeOverloads.ContainsKey(this))
                return;

            foreach (RecordType recordType in recordTypeOverloads[this])
            {

            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class RecordType
    {
        public string GetStandardIdentifier() => $"_nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetCName()))}_";

        public string GetCName() => $"{GetStandardIdentifier()}_t*";
        public string GetCHeapSizer() => $"sizeof({GetStandardIdentifier()}_t";

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => emitter.AppendLine($"free_record{GetStandardIdentifier()}({valueCSource});");
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append($"copy_record{GetStandardIdentifier()}({valueCSource})");

        public void EmitGetProperty(StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}->{property.Name}");

        public void ScopeForUsedTypes()
        {
            if (RecordDeclaration.DeclareUsedRecordType(this)) 
            {
                foreach (var property in properties.Value)
                {
                    property.Type.ScopeForUsedTypes();
                    if (property.DefaultValue != null)
                        property.DefaultValue.ScopeForUsedTypes(new Dictionary<TypeParameter, IType>());
                }
            }
        }

        public void EmitCStruct(StringBuilder emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier() + " {");
            foreach (var property in properties.Value)
                emitter.Append($"\t{GetCName()} {property.Name};");
            emitter.AppendLine("}");
        }
    }
}
