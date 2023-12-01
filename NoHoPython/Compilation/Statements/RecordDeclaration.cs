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

            DeclareTypeDependencies(recordType, recordType.GetProperties().ConvertAll((prop) => prop.Type).ToArray());

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

        public void ForwardDeclareRecordTypes(Emitter emitter)
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
        public bool EmitMultipleCStructs => properties.Any((property) => property.Type.TypeParameterAffectsCodegen(new(new ITypeComparer())));
#pragma warning restore CS8604 // Possible null reference argument.

        partial class RecordProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => OptimizeMessageReciever;

            public bool OptimizeMessageReciever => RecordDeclaration.messageReceiverNames.Contains(Name);
            public bool HasDefaultValue => RecordDeclaration.defaultValues.ContainsKey(Name);

            public IRValue? DefaultValue { get; private set; }

            public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
            {
                Type.ScopeForUsedTypes(irBuilder);
                if(!OptimizeMessageReciever && HasDefaultValue)
                {
                    DefaultValue = RecordDeclaration.defaultValues[Name].SubstituteWithTypearg(RecordType.TypeargMap);
                    DefaultValue.ScopeForUsedTypes(new(), irBuilder);
                }
            }

            public override void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
            {
                if (!OptimizeMessageReciever)
                    return;

                DefaultValue = RecordDeclaration.defaultValues[Name].SubstituteWithTypearg(RecordType.TypeargMap);
                if(!optimizedMessageRecieverCall)
                    DefaultValue.ScopeForUsedTypes(typeargs, irBuilder);
            }

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer)
            {
                if (OptimizeMessageReciever)
                {
#pragma warning disable CS8600 //DefaultValue != null is a precondition for OptimizeMessageReciever
#pragma warning disable CS8602
                    AnonymizeProcedure anonymizeProcedure = (AnonymizeProcedure)DefaultValue;
                    anonymizeProcedure.EmitForPropertyGet(irProgram, emitter, typeargs, value, responsibleDestroyer);
#pragma warning restore CS8602
#pragma warning restore CS8600
                    return true;
                }
                else
                {
                    value(emitter);
                    emitter.Append($"->{Name}");
                    return false;
                }
            }
        }

        public static void EmitRecordMaskProto(Emitter emitter)
        {
            emitter.AppendLine("typedef struct nhp_std_record_mask nhp_std_record_mask_t;");
            emitter.AppendLine("struct nhp_std_record_mask {");
            emitter.AppendLine("\tint nhp_ref_count;");
            emitter.AppendLine("\tint nhp_master_count;");
            emitter.AppendLine("\tint nhp_lock;");
            emitter.AppendLine($"\t{RecordType.StandardRecordMask} parent_record;");
            emitter.AppendLine("};");
            emitter.AppendLine("typedef void (*nhp_custom_destructor)(void* to_destroy);");
        }

        public static void EmitRecordChildFinder(Emitter emitter)
        {
            emitter.AppendLine("static int nhp_record_has_child(nhp_std_record_mask_t* parent, nhp_std_record_mask_t* child) {");
            emitter.AppendLine("\twhile(child != NULL) {");
            emitter.AppendLine("\t\tif(parent == child)");
            emitter.AppendLine("\t\t\treturn 1;");
            emitter.AppendLine("\t\tchild = child->parent_record;");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\treturn 0;");
            emitter.AppendLine("}");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, Emitter emitter)
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

        public void ForwardDeclare(IRProgram irProgram, Emitter emitter)
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
                }
            }
            else
            {
                RecordType genericRecordType = irProgram.RecordTypeOverloads[this].First();

                emitter.Append($"void free_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record, {RecordType.StandardRecordMask} child_agent");
                if (Destructor != null)
                    emitter.Append(", nhp_custom_destructor destructor");
                emitter.AppendLine(");");

                emitter.AppendLine($"{genericRecordType.GetCName(irProgram)} borrow_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record, void* responsible_destroyer);");

                if (!genericRecordType.HasCopier)
                    emitter.AppendLine($"{genericRecordType.GetCName(irProgram)} copy_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record, {RecordType.StandardRecordMask} parent_record);");

                foreach(RecordType recordType in irProgram.RecordTypeOverloads[this])
                {
                    recordType.EmitConstructorCHeader(irProgram, emitter);
                    emitter.AppendLine(";");
                }
            }
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (!irProgram.RecordTypeOverloads.ContainsKey(this))
                return;

            if (EmitMultipleCStructs)
            {
                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                {
                    recordType.EmitConstructor(irProgram, primaryEmitter);
                    recordType.EmitDestructor(irProgram, primaryEmitter);
                    recordType.EmitCopier(irProgram, primaryEmitter);
                    recordType.EmitBorrower(irProgram, primaryEmitter);
                }
            }
            else
            {
                RecordType genericRecordType = irProgram.RecordTypeOverloads[this].First();
                genericRecordType.EmitDestructor(irProgram, primaryEmitter);
                genericRecordType.EmitCopier(irProgram, primaryEmitter);
                genericRecordType.EmitBorrower(irProgram, primaryEmitter);

                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                    recordType.EmitConstructor(irProgram, primaryEmitter);
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

        public static string StandardRecordMask => "nhp_std_record_mask_t*";

        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => true;
        public bool IsTypeDependency => false;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo)
        {
            if (effectInfo.ContainsKey(this))
                return effectInfo[this];

            effectInfo.Add(this, false);
            return effectInfo[this] = properties.Value.Any((property) => property.Type.TypeParameterAffectsCodegen(effectInfo));
        }

        public string GetStandardIdentifier(IRProgram irProgram) => RecordPrototype.EmitMultipleCStructs ? GetOriginalStandardIdentifer(irProgram) : $"nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}";
        public string GetOriginalStandardIdentifer(IRProgram irProgram) => $"nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}{string.Join(string.Empty, TypeArguments.ConvertAll((typearg) => $"_{typearg.GetStandardIdentifier(irProgram)}"))}";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t*";
        public string GetCHeapSizer(IRProgram irProgram) => $"sizeof({GetStandardIdentifier(irProgram)}_t)";

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            emitter.Append($"free_record{GetStandardIdentifier(irProgram)}(");
            valuePromise(emitter);
            emitter.Append($", ({StandardRecordMask})");
            childAgent(emitter);

            if (destructorCall == null) 
            {
                ITypeComparer comparer = new();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                destructorCall = irProgram.RecordTypeOverloads[RecordPrototype].Find((type) => comparer.Equals(type, this)).destructorCall;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            if (!RecordPrototype.EmitMultipleCStructs && destructorCall != null)
                emitter.Append($", (nhp_custom_destructor)&{destructorCall.GetStandardIdentifier(irProgram)}");
            emitter.AppendLine(");");
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter primaryEmitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)
        {
            if (copierCall == null)
            {
                ITypeComparer comparer = new();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                copierCall = irProgram.RecordTypeOverloads[RecordPrototype].Find((type) => comparer.Equals(type, this)).copierCall;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            primaryEmitter.Append(copierCall != null ? copierCall.GetStandardIdentifier(irProgram) : $"copy_record{GetStandardIdentifier(irProgram)}");
            primaryEmitter.Append('(');
            valueCSource(primaryEmitter);
            primaryEmitter.Append(", ");
            if (copierCall == null)
                primaryEmitter.Append($"({StandardRecordMask})");
            responsibleDestroyer(primaryEmitter);
            primaryEmitter.Append(')');
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) 
        { 
            emitter.Append($"borrow_record{GetStandardIdentifier(irProgram)}(");
            valueCSource(emitter);
            emitter.Append($", ({StandardRecordMask})");
            responsibleDestroyer(emitter);
            emitter.Append(')');
        }

        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord);

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
            properties.Value.ForEach((property) => property.ScopeForUsedTypes(irBuilder));
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
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
        
        public void EmitCStructImmediatley(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine($"struct {GetStandardIdentifier(irProgram)} {{");
            emitter.AppendLine("\tint nhp_ref_count;");
            emitter.AppendLine("\tint nhp_master_count;");
            emitter.AppendLine("\tint nhp_lock;");
            emitter.AppendLine($"\t{StandardRecordMask} parent_record;");

            foreach (var property in properties.Value)
            {
                if(!property.OptimizeMessageReciever)
                    emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            }

            emitter.AppendLine("};");
        }

        public void EmitConstructorCHeader(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} construct_{GetOriginalStandardIdentifer(irProgram)}(");
            for (int i = 0; i < constructorParameterTypes.Value.Count; i++)
                emitter.Append($"{constructorParameterTypes.Value[i].GetCName(irProgram)} param{i}, ");
            emitter.Append($"{StandardRecordMask} parent_record)");
        }

        public void EmitConstructor(IRProgram irProgram, Emitter emitter)
        {
            EmitConstructorCHeader(irProgram, emitter);
            emitter.AppendLine(" {"); 
            
            emitter.AppendLine($"\t{GetCName(irProgram)} nhp_self = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            emitter.AppendLine("\tnhp_self->nhp_ref_count = 0;");
            emitter.AppendLine("\tnhp_self->nhp_master_count = 0;");
            emitter.AppendLine("\tnhp_self->nhp_lock = 0;");
            emitter.AppendLine("\tnhp_self->parent_record = parent_record;");
            
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                if (recordProperty.HasDefaultValue && !recordProperty.OptimizeMessageReciever)
                {
#pragma warning disable CS8602 //recordProperty.HasDefaultValue guarentees DefaultValue is not null 
                    recordProperty.DefaultValue.Emit(irProgram, emitter, typeargMap.Value, (valuePromise) =>
                    {
                        emitter.Append($"\tnhp_self->{recordProperty.Name} = ");
                        if (recordProperty.DefaultValue.RequiresDisposal(irProgram, new(), false))
                            valuePromise(emitter);
                        else
                            recordProperty.Type.EmitCopyValue(irProgram, emitter, valuePromise, (e) => e.Append("nhp_self"));
                        emitter.AppendLine(';');
                    }, (e) => e.Append("nhp_self"), false);
#pragma warning restore CS8602 
                }

            emitter.Append($"\t{constructorCall.GetStandardIdentifier(irProgram)}(");
            for (int i = 0; i < constructorParameterTypes.Value.Count; i++)
                emitter.Append($"param{i}, ");
            emitter.AppendLine("nhp_self);");
            
            emitter.AppendLine("\treturn nhp_self;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"void free_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, {StandardRecordMask} child_agent");
            if (!RecordPrototype.EmitMultipleCStructs && destructorCall != null)
                emitter.Append(", nhp_custom_destructor destructor");
            emitter.AppendStartBlock(")");

            emitter.AppendLine($"\tif(record->nhp_lock || nhp_record_has_child(({StandardRecordMask})record, child_agent))");
            emitter.AppendLine("\t\treturn;");

            emitter.AppendLine("if(record->nhp_ref_count) {");
            emitter.AppendLine($"\tif(nhp_record_has_child(child_agent, ({StandardRecordMask})record)) {{");
            emitter.AppendLine("\t\tif(record->nhp_master_count == 0)");
            emitter.AppendLine("\t\t\trecord->parent_record = NULL;");
            emitter.AppendLine("\t\telse");
            emitter.AppendLine("\t\t\trecord->nhp_master_count--;");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\trecord->nhp_ref_count--;");
            emitter.AppendLine("\treturn;");
            emitter.AppendLine("}");
            
            emitter.AppendLine("record->nhp_lock = 1;");
            if (destructorCall != null)
                emitter.AppendLine($"{destructorCall.GetStandardIdentifier(irProgram)}(record);");
            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                if (recordProperty.Type.RequiresDisposal && !recordProperty.OptimizeMessageReciever)
                    recordProperty.Type.EmitFreeValue(irProgram, emitter, (e) => e.Append($"record->{recordProperty.Name}"), (e) => e.Append("record"));

            emitter.AppendLine($"{irProgram.MemoryAnalyzer.Dealloc("record", GetCHeapSizer(irProgram))}");
            emitter.AppendEndBlock();
        }

        public void EmitCopier(IRProgram irProgram, Emitter emitter)
        {
            if (copierCall != null)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} copy_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, {StandardRecordMask} parent_record) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_record = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            emitter.AppendLine("\tcopied_record->nhp_ref_count = 0;");
            emitter.AppendLine("\tcopied_record->nhp_master_count = 0;");
            emitter.AppendLine("\tcopied_record->nhp_lock = 0;");
            emitter.AppendLine("\tcopied_record->parent_record = parent_record;");

            foreach (RecordDeclaration.RecordProperty property in properties.Value)
            {
                if (!property.OptimizeMessageReciever)
                {
                    emitter.Append($"\tcopied_record->{property.Name} = ");
                    property.Type.EmitRecordCopyValue(irProgram, emitter, (e) => e.Append($"record->{property.Name}"), (e) => e.Append("copied_record"));
                    emitter.AppendLine(";");
                }
            }

            emitter.AppendLine("\treturn copied_record;");
            emitter.AppendLine("}");
        }

        public void EmitBorrower(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} borrow_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record, void* responsible_destroyer) {{");

            emitter.AppendLine($"\tif(!nhp_record_has_child(({StandardRecordMask})record, responsible_destroyer)) {{");
            emitter.AppendLine("\t\trecord->nhp_ref_count++;");
            emitter.AppendLine($"\t\tif(nhp_record_has_child(({StandardRecordMask})responsible_destroyer, ({StandardRecordMask})record))");
            emitter.AppendLine("\t\t\trecord->nhp_master_count++;");
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\treturn record;");
            emitter.AppendLine("}");
        }
    }
}