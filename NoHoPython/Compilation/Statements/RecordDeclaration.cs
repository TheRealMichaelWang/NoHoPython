using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;

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
                recordTypeOverloads.Add(recordType.RecordPrototype, new());
            recordTypeOverloads[recordType.RecordPrototype].Add(recordType);

            typeDependencyTree.Add(recordType, new HashSet<IType>(recordType.GetProperties().ConvertAll((prop) => prop.Type).Where((type) => type is not RecordType), new ITypeComparer()));

            return true;
        }

        public RecordType? FindSimilarRecordType(RecordType current)
        {
            RecordType toret;
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            if (usedRecordTypes.TryGetValue(current, out toret))
                return toret;
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            return null;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public readonly Dictionary<RecordDeclaration, List<RecordType>> RecordTypeOverloads;

        public void ForwardDeclareRecordTypes(StatementEmitter emitter)
        {
            foreach(RecordDeclaration recordDeclaration in RecordTypeOverloads.Keys)
            {
                if(recordDeclaration.EmitMultipleCStructs)
                {
                    foreach(RecordType recordType in RecordTypeOverloads[recordDeclaration])
                        emitter.AppendLine($"typedef struct {recordType.GetStandardIdentifier(this)} {recordType.GetStandardIdentifier(this)}_t;");
                }
                else
                    emitter.AppendLine($"typedef struct {RecordTypeOverloads[recordDeclaration].First().GetStandardIdentifier(this)} {RecordTypeOverloads[recordDeclaration].First().GetStandardIdentifier(this)}_t;");
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class RecordDeclaration
    {
#pragma warning disable CS8604 // Possible null reference argument.
        public bool EmitMultipleCStructs => properties.Any((property) => property.Type.TypeParameterAffectsCodegen);
#pragma warning restore CS8604 // Possible null reference argument.

        partial class RecordProperty
        {
            public override bool RequiresDisposal => OptimizeMessageReciever;

            public bool OptimizeMessageReciever => Type is ProcedureType && HasDefaultValue && RecordDeclaration.defaultValues[Name] is AnonymizeProcedure && IsReadOnly;
            public bool HasDefaultValue => RecordDeclaration.defaultValues.ContainsKey(Name);

            public IRValue? DefaultValue { get; private set; }

            public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder, Dictionary<TypeParameter, IType> typeargs)
            {
                Type.ScopeForUsedTypes(irBuilder);
                if(!OptimizeMessageReciever && HasDefaultValue)
                {
                    DefaultValue = RecordDeclaration.defaultValues[Name].SubstituteWithTypearg(typeargs);
                    DefaultValue.ScopeForUsedTypes(new(), irBuilder);
                }
            }

            public override void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
            {
                if (!OptimizeMessageReciever)
                    return;

                DefaultValue = RecordDeclaration.defaultValues[Name].SubstituteWithTypearg(typeargs);
                if(!optimizedMessageRecieverCall)
                    DefaultValue.ScopeForUsedTypes(new(), irBuilder);
            }

            public override bool EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource, string responsibleDestroyer)
            {
                if (OptimizeMessageReciever)
                {
#pragma warning disable CS8600 //DefaultValue != null is a precondition for OptimizeMessageReciever
#pragma warning disable CS8602
                    AnonymizeProcedure anonymizeProcedure = (AnonymizeProcedure)DefaultValue;
                    anonymizeProcedure.EmitForPropertyGet(irProgram, emitter, typeargs, valueCSource, responsibleDestroyer);
#pragma warning restore CS8602
#pragma warning restore CS8600
                    return true;
                }
                else
                {
                    emitter.Append($"{valueCSource}->{Name}");
                    return false;
                }
            }
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
            emitter.AppendLine("typedef void (*_nhp_custom_destructor)(void* to_destroy);");
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

            if (EmitMultipleCStructs)
            {
                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                    recordType.EmitCStruct(irProgram, emitter);
            }
            else
            {
                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                {
                    if (recordType == irProgram.RecordTypeOverloads[this].First())
                    {
                        if (!irProgram.DeclareCompiledType(emitter, recordType))
                            return;

                        recordType.EmitCStructImmediatley(irProgram, emitter);
                    }
                    else
                        irProgram.DeclareCompiledType(emitter, recordType);
                }
            }
        }

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            if (EmitMultipleCStructs)
            {
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
            else
            {
                RecordType genericRecordType = irProgram.RecordTypeOverloads[this].First();

                emitter.AppendLine($"void free_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record, {RecordType.StandardRecordMask} child_agent");
                if (Destructor != null)
                    emitter.Append(", _nhp_custom_destructor destructor");
                emitter.AppendLine(");");

                emitter.AppendLine($"{genericRecordType.GetCName(irProgram)} borrow_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record, void* responsible_destroyer);");

                if (!genericRecordType.HasCopier)
                    emitter.AppendLine($"{genericRecordType.GetCName(irProgram)} copy_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record, {RecordType.StandardRecordMask} parent_record);");
                if (!irProgram.EmitExpressionStatements)
                    emitter.AppendLine($"{genericRecordType.GetCName(irProgram)} move_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)}* dest, {genericRecordType.GetCName(irProgram)} src, void* child_agent);");

                foreach(RecordType recordType in irProgram.RecordTypeOverloads[this])
                {
                    recordType.EmitConstructorCHeader(irProgram, emitter);
                    emitter.AppendLine(";");
                }
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            if (EmitMultipleCStructs)
            {
                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                {
                    recordType.EmitConstructor(irProgram, emitter);
                    recordType.EmitDestructor(irProgram, emitter);
                    recordType.EmitCopier(irProgram, emitter);
                    recordType.EmitMover(irProgram, emitter);
                    recordType.EmitBorrower(irProgram, emitter);
                }
            }
            else
            {
                RecordType genericRecordType = irProgram.RecordTypeOverloads[this].First();
                genericRecordType.EmitDestructor(irProgram, emitter);
                genericRecordType.EmitCopier(irProgram, emitter);
                genericRecordType.EmitMover(irProgram, emitter);
                genericRecordType.EmitBorrower(irProgram, emitter);

                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                    recordType.EmitConstructor(irProgram, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class RecordType
    {
        public bool HasCopier => copierCall != null;

        private ProcedureReference constructorCall;
        private ProcedureReference? destructorCall = null;
        private ProcedureReference? copierCall = null;

        public Dictionary<TypeParameter, IType> TypeargMap => typeargMap.Value;

        public static string StandardRecordMask => "_nhp_std_record_mask_t*";

        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => true;
        public bool TypeParameterAffectsCodegen => properties.Value.Any((property) => property.Type.TypeParameterAffectsCodegen);

        public string GetStandardIdentifier(IRProgram irProgram) => RecordPrototype.EmitMultipleCStructs ? GetOriginalStandardIdentifer(irProgram) : $"_nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}";
        public string GetOriginalStandardIdentifer(IRProgram irProgram) => $"_nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t*";
        public string GetCHeapSizer(IRProgram irProgram) => $"sizeof({GetStandardIdentifier(irProgram)}_t)";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent)
        {
            emitter.Append($"free_record{GetStandardIdentifier(irProgram)}({valueCSource}, ({StandardRecordMask}){childAgent}");
            if (destructorCall == null) 
            {
                ITypeComparer comparer = new();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                destructorCall = irProgram.RecordTypeOverloads[RecordPrototype].Find((type) => comparer.Equals(type, this)).destructorCall;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            if (!RecordPrototype.EmitMultipleCStructs && destructorCall != null)
                emitter.Append($", (_nhp_custom_destructor)&{destructorCall.GetStandardIdentifier(irProgram)}");
            emitter.Append(");");
        }

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (copierCall == null)
            {
                ITypeComparer comparer = new();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                copierCall = irProgram.RecordTypeOverloads[RecordPrototype].Find((type) => comparer.Equals(type, this)).copierCall;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            if (copierCall != null)
                emitter.Append($"{copierCall.GetStandardIdentifier(irProgram)}({valueCSource}, {responsibleDestroyer})");
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

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (!irBuilder.DeclareUsedRecordType(this)) 
                return;

            constructorCall = new ProcedureReference(typeargMap.Value, RecordPrototype.Constructor, false, RecordPrototype.Constructor.ErrorReportedElement);
            if (RecordPrototype.Destructor != null)
                destructorCall = new ProcedureReference(typeargMap.Value, RecordPrototype.Destructor, false, RecordPrototype.Destructor.ErrorReportedElement);
            if(RecordPrototype.Copier != null)
                copierCall = new ProcedureReference(typeargMap.Value, RecordPrototype.Copier, false, RecordPrototype.Copier.ErrorReportedElement);

            constructorCall = constructorCall.ScopeForUsedTypes(irBuilder);
            destructorCall = destructorCall?.ScopeForUsedTypes(irBuilder);
            copierCall = copierCall?.ScopeForUsedTypes(irBuilder);
            properties.Value.ForEach((property) => property.ScopeForUsedTypes(irBuilder, typeargMap.Value));
        }

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            if (!RecordPrototype.EmitMultipleCStructs)
            {
                RecordPrototype.ForwardDeclareType(irProgram, emitter);
                return;
            }

            EmitCStructImmediatley(irProgram, emitter);
        }
        
        public void EmitCStructImmediatley(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)} {{");
            emitter.AppendLine("\tint _nhp_ref_count;");
            emitter.AppendLine("\tint _nhp_master_count;");
            emitter.AppendLine("\tint _nhp_lock;");
            emitter.AppendLine($"\t{StandardRecordMask} parent_record;");

            foreach (var property in properties.Value)
            {
                if(!property.OptimizeMessageReciever)
                    emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            }

            emitter.AppendLine("};");
        }

        public void EmitConstructorCHeader(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} construct_{GetOriginalStandardIdentifer(irProgram)}(");
            for (int i = 0; i < constructorParameterTypes.Value.Count; i++)
                emitter.Append($"{constructorParameterTypes.Value[i].GetCName(irProgram)} param{i}, ");
            emitter.Append($"{StandardRecordMask} parent_record)");
        }

        public void EmitConstructor(IRProgram irProgram, StatementEmitter emitter)
        {
            EmitConstructorCHeader(irProgram, emitter);
            emitter.AppendLine(" {"); 
            
            emitter.AppendLine($"\t{GetCName(irProgram)} _nhp_self = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            emitter.AppendLine("\t_nhp_self->_nhp_ref_count = 0;");
            emitter.AppendLine("\t_nhp_self->_nhp_master_count = 0;");
            emitter.AppendLine("\t_nhp_self->_nhp_lock = 0;");
            emitter.AppendLine("\t_nhp_self->parent_record = parent_record;");
            
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                if (recordProperty.HasDefaultValue && !recordProperty.OptimizeMessageReciever)
                {
                    emitter.Append($"\t_nhp_self->{recordProperty.Name} = ");
#pragma warning disable CS8602 //recordProperty.HasDefaultValue guarentees DefaultValue is not null 
                    if (recordProperty.DefaultValue.RequiresDisposal(new()))
                        recordProperty.DefaultValue.Emit(irProgram, emitter, new(), "_nhp_self");
                    else
                        recordProperty.Type.EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(recordProperty.DefaultValue, irProgram, new(), "NULL"), "_nhp_self");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    emitter.AppendLine(";");
                }

            emitter.Append($"\t{constructorCall.GetStandardIdentifier(irProgram)}(");
            for (int i = 0; i < constructorParameterTypes.Value.Count; i++)
                emitter.Append($"param{i}, ");
            emitter.AppendLine("_nhp_self);");
            
            emitter.AppendLine("\treturn _nhp_self;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"void free_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, {StandardRecordMask} child_agent");
            if (!RecordPrototype.EmitMultipleCStructs && destructorCall != null)
                emitter.Append(", _nhp_custom_destructor destructor");
            emitter.AppendLine(") {");

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
            if (destructorCall != null)
                emitter.AppendLine("\tdestructor(record);");
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
            {
                if (recordProperty.Type.RequiresDisposal && !recordProperty.OptimizeMessageReciever)
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
            if (copierCall != null)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} copy_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, {StandardRecordMask} parent_record) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_record = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            emitter.AppendLine("\tcopied_record->_nhp_ref_count = 0;");
            emitter.AppendLine("\tcopied_record->_nhp_master_count = 0;");
            emitter.AppendLine("\tcopied_record->_nhp_lock = 0;");
            emitter.AppendLine("\tcopied_record->parent_record = parent_record;");

            foreach (RecordDeclaration.RecordProperty property in properties.Value)
            {
                if (!property.OptimizeMessageReciever)
                {
                    emitter.Append($"\tcopied_record->{property.Name} = ");
                    property.Type.EmitRecordCopyValue(irProgram, emitter, $"record->{property.Name}", "copied_record");
                    emitter.AppendLine(";");
                }
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