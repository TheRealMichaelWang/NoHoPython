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

        public static void EmitRecordMaskProto(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine("typedef struct nhp_trace_obj nhp_trace_obj_t;");
            emitter.AppendLine("typedef struct nhp_rc_obj nhp_rc_obj_t;");

            emitter.AppendLine("struct nhp_trace_obj {");
            emitter.AppendLine("\tunion nhp_std_rc_mask_parent_info { nhp_trace_obj_t* parent; nhp_trace_obj_t** parents; } parent_info;");
            emitter.AppendLine("\tint nhp_parent_count;");
            emitter.AppendLine("\tint nhp_lock;");
            emitter.AppendLine("};");
            emitter.AppendLine("typedef void (*nhp_custom_destructor)(void* to_destroy);");

            emitter.AppendLine("struct nhp_rc_obj {");
            emitter.AppendLine("\tint nhp_count;");
            emitter.AppendLine("};");

            emitter.AppendStartBlock("static nhp_rc_obj_t* nhp_rc_ref(nhp_rc_obj_t* obj)");
            emitter.AppendLine("obj->nhp_count++;");
            emitter.AppendLine("return obj;");
            emitter.AppendEndBlock();

            emitter.AppendStartBlock("static nhp_trace_obj_t* nhp_trace_add_parent(nhp_trace_obj_t* child, nhp_trace_obj_t* parent)");
            emitter.AppendLine("child->nhp_parent_count++;");
            emitter.AppendLine("if(child->nhp_parent_count == 1) { child->parent_info.parent = parent; return child; }");
            emitter.AppendStartBlock("if(child->nhp_parent_count == 2)");
            emitter.AppendLine("nhp_trace_obj_t* parent1 = child->parent_info.parent;");
            emitter.AppendLine($"child->parent_info.parents = {irProgram.MemoryAnalyzer.Allocate("2 * sizeof(nhp_trace_obj_t*)")};");
            emitter.AppendLine("child->parent_info.parents[0] = parent1;");
            emitter.AppendLine("child->parent_info.parents[1] = parent;");
            emitter.AppendLine("return child;");
            emitter.AppendEndBlock();
            emitter.AppendLine($"child->parent_info.parents = {irProgram.MemoryAnalyzer.Realloc("child->parent_info.parents", "(child->nhp_parent_count - 1) * sizeof(nhp_trace_obj_t*)", "child->nhp_parent_count * sizeof(nhp_trace_obj_t*)")};");
            emitter.AppendLine("child->parent_info.parents[child->nhp_parent_count - 1] = parent;");
            emitter.AppendLine("return child;");
            emitter.AppendEndBlock();

            emitter.AppendStartBlock("static int nhp_trace_del_parent(nhp_trace_obj_t* child, nhp_trace_obj_t* parent)");
            emitter.AppendStartBlock("if(child->nhp_parent_count == 1)");
            emitter.AppendLine("if(child->parent_info.parent != parent) { return 0; }");
            emitter.AppendLine("child->nhp_parent_count = 0;");
            emitter.AppendLine("return 1;");
            emitter.AppendEndBlock();
            emitter.AppendStartBlock("if(child->nhp_parent_count == 2)");
            emitter.AppendLine("int kept;");
            emitter.AppendLine("if(child->parent_info.parents[0] == parent) { kept = 1; }");
            emitter.AppendLine("else if(child->parent_info.parents[1] == parent) { kept = 0; }");
            emitter.AppendLine("else { return 0; }");
            emitter.AppendLine("child->nhp_parent_count = 1;");
            emitter.AppendLine("nhp_trace_obj_t** parents = child->parent_info.parents;");
            emitter.AppendLine("child->parent_info.parent = parents[kept];");
            emitter.AppendLine($"{irProgram.MemoryAnalyzer.Dealloc("parents", "2 * sizeof(nhp_trace_obj_t*)")};");
            emitter.AppendLine("return 1;");
            emitter.AppendEndBlock();
            emitter.AppendStartBlock("for(int i = 0; i < child->nhp_parent_count; i++)");
            emitter.AppendStartBlock("if(child->parent_info.parents[i] == parent)");
            emitter.AppendLine("memmove(&child->parent_info.parents[i], &child->parent_info.parents[i + 1], (child->nhp_parent_count - (i + 1)) * sizeof(nhp_trace_obj_t*));");
            emitter.AppendLine("child->nhp_parent_count = child->nhp_parent_count - 1;");
            emitter.AppendLine($"child->parent_info.parents = {irProgram.MemoryAnalyzer.Realloc("child->parent_info.parents", "(child->nhp_parent_count + 1) * sizeof(nhp_trace_obj_t*)", "child->nhp_parent_count * sizeof(nhp_trace_obj_t*)")};");
            emitter.AppendLine("return 1;");
            emitter.AppendEndBlock();
            emitter.AppendEndBlock();
            emitter.AppendLine("return 0;");
            emitter.AppendEndBlock();

            emitter.AppendStartBlock("static int nhp_trace_reachable(nhp_trace_obj_t* parent)");
            emitter.AppendLine("if(parent == NULL) { return 1; } //succesfully traced to top level");
            emitter.AppendLine("if(parent->nhp_lock) { return 0; }");
            emitter.AppendLine("if(parent->nhp_parent_count == 0) { return 0; }");
            emitter.AppendStartBlock("if(parent->nhp_parent_count == 1)");
            emitter.AppendLine("parent->nhp_lock = 1;");
            emitter.AppendLine("int res = nhp_trace_reachable(parent->parent_info.parent);");
            emitter.AppendLine("parent->nhp_lock = 0;");
            emitter.AppendLine("return res;");
            emitter.AppendEndBlock();
            emitter.AppendLine("parent->nhp_lock = 1;");
            emitter.AppendStartBlock("for(int i = 0; i < parent->nhp_parent_count; i++)");
            emitter.AppendLine("if(nhp_trace_reachable(parent->parent_info.parents[i])) { parent->nhp_lock = 0; return 1; }");
            emitter.AppendEndBlock();
            emitter.AppendLine("parent->nhp_lock = 0;");
            emitter.AppendLine("return 0;");
            emitter.AppendEndBlock();

            emitter.AppendStartBlock("static void nhp_trace_destroy(nhp_trace_obj_t* trace_unit)");
            emitter.AppendLine("if(trace_unit->nhp_parent_count <= 1) { return; }");
            emitter.AppendLine($"{irProgram.MemoryAnalyzer.Dealloc("trace_unit->parent_info.parents", "trace_unit->nhp_parent_count * sizeof(nhp_trace_obj_t*)")};");
            emitter.AppendLine("return;");
            emitter.AppendEndBlock();
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

            void AppendFunctionEnd(RecordType type, string name)
            {
                if (type.IsCircularDataStructure)
                    emitter.AppendLine($", nhp_trace_obj_t* {name});");
                else
                    emitter.AppendLine(");");
            }

            if (EmitMultipleCStructs)
            {
                foreach (RecordType recordType in irProgram.RecordTypeOverloads[this])
                {
                    recordType.EmitConstructorCHeader(irProgram, emitter);
                    emitter.AppendLine(";");

                    emitter.Append($"void free_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record");
                    AppendFunctionEnd(recordType, "child_agent");

                    if (!recordType.HasCopier && !PassByReference)
                    {
                        emitter.Append($"{recordType.GetCName(irProgram)} copy_record{recordType.GetStandardIdentifier(irProgram)}({recordType.GetCName(irProgram)} record");
                        AppendFunctionEnd(recordType, "parent_record");
                    }
                }
            }
            else
            {
                RecordType genericRecordType = irProgram.RecordTypeOverloads[this].First();

                emitter.Append($"void free_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record");
                if (Destructor != null)
                    emitter.Append(", nhp_custom_destructor destructor");
                AppendFunctionEnd(genericRecordType, "freeing_parent");

                if (!genericRecordType.HasCopier && !PassByReference)
                {
                    emitter.Append($"{genericRecordType.GetCName(irProgram)} copy_record{genericRecordType.GetStandardIdentifier(irProgram)}({genericRecordType.GetCName(irProgram)} record");
                    AppendFunctionEnd(genericRecordType, "parent_record");
                }

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
                }
            }
            else
            {
                RecordType genericRecordType = irProgram.RecordTypeOverloads[this].First();
                genericRecordType.EmitDestructor(irProgram, primaryEmitter);
                genericRecordType.EmitCopier(irProgram, primaryEmitter);

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

        public bool IsCircularDataStructure => properties.Value.Any(property => {
            if (property.Type is ProcedureType && !property.OptimizeMessageReciever)
                return true;
            if(property.Type is RecordType recordType)
            {
                if (recordType.IsCompatibleWith(this))
                    return false; //inconclusive

                if (recordType.IsCircularDataStructure)
                    return true;
            }
            if (RecordPrototype.PassByReference && property.Type.ContainsType(this))
                return true;
            return false;
        });

        public string GetStandardIdentifier(IRProgram irProgram) => RecordPrototype.EmitMultipleCStructs ? GetOriginalStandardIdentifer(irProgram) : $"nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}";
        public string GetOriginalStandardIdentifer(IRProgram irProgram) => $"nhp_record_{IScopeSymbol.GetAbsolouteName(RecordPrototype)}{string.Join(string.Empty, TypeArguments.ConvertAll((typearg) => $"_{typearg.GetStandardIdentifier(irProgram)}"))}";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t*";
        public string GetCHeapSizer(IRProgram irProgram) => $"sizeof({GetStandardIdentifier(irProgram)}_t)";

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            emitter.Append($"free_record{GetStandardIdentifier(irProgram)}(");
            valuePromise(emitter);
            if (IsCircularDataStructure)
            {
                emitter.Append(", (nhp_trace_obj_t*)");
                childAgent(emitter);
            }

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
            if (RecordPrototype.PassByReference)
            {
                EmitClosureBorrowValue(irProgram, primaryEmitter, valueCSource, responsibleDestroyer);
                return;
            }

            if (copierCall == null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                copierCall = irProgram.RecordTypeOverloads[RecordPrototype].Find((type) => IsCompatibleWith(type)).copierCall;
#pragma warning restore CS8602
            }
            primaryEmitter.Append(copierCall != null ? copierCall.GetStandardIdentifier(irProgram) : $"copy_record{GetStandardIdentifier(irProgram)}");
            primaryEmitter.Append('(');
            valueCSource(primaryEmitter);
            if(!IsCircularDataStructure && copierCall == null)
            {
                primaryEmitter.Append(')');
                return;
            }
            primaryEmitter.Append(", ");
            if (copierCall == null)
                primaryEmitter.Append($"(nhp_trace_obj_t*)");
            responsibleDestroyer(primaryEmitter);
            primaryEmitter.Append(')');
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) 
        {
            if (IsCircularDataStructure)
            {
                emitter.Append($"({GetCName(irProgram)})nhp_trace_add_parent((nhp_trace_obj_t*)");
                valueCSource(emitter);
                emitter.Append(", (nhp_trace_obj_t*)");
                responsibleDestroyer(emitter);
            }
            else
            {
                emitter.Append($"({GetCName(irProgram)})nhp_rc_ref((nhp_rc_obj_t*)");
                valueCSource(emitter);
            }
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

            if (IsCircularDataStructure)
                emitter.AppendLine("\tnhp_trace_obj_t trace_unit;");
            else
                emitter.AppendLine("\tnhp_rc_obj_t rc_unit;");

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
            {
                emitter.Append($"{constructorParameterTypes.Value[i].GetCName(irProgram)} param{i}");
                if (i != constructorParameterTypes.Value.Count - 1)
                    emitter.Append(", ");
            }
            if (IsCircularDataStructure)
            {
                if (constructorParameterTypes.Value.Count > 0)
                    emitter.Append(", ");
                emitter.Append("nhp_trace_obj_t* parent_record");
            }
            emitter.Append(')');
        }

        public void EmitConstructor(IRProgram irProgram, Emitter emitter)
        {
            EmitConstructorCHeader(irProgram, emitter);
            emitter.AppendStartBlock();
            
            emitter.AppendLine($"{GetCName(irProgram)} nhp_self = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");

            if (IsCircularDataStructure)
            {
                emitter.AppendLine("nhp_self->trace_unit.nhp_parent_count = 0;");
                emitter.AppendLine("nhp_self->trace_unit.nhp_lock = 0;");
                emitter.AppendLine($"nhp_trace_add_parent((nhp_trace_obj_t*)nhp_self, parent_record);");
            }
            else
                emitter.AppendLine("nhp_self->rc_unit.nhp_count = 0;");

            foreach (RecordDeclaration.RecordProperty recordProperty in properties.Value)
                if (recordProperty.HasDefaultValue && !recordProperty.OptimizeMessageReciever)
                {
#pragma warning disable CS8602 //recordProperty.HasDefaultValue guarentees DefaultValue is not null 
                    recordProperty.DefaultValue.Emit(irProgram, emitter, typeargMap.Value, (valuePromise) =>
                    {
                        emitter.Append($"nhp_self->{recordProperty.Name} = ");
                        if (recordProperty.DefaultValue.RequiresDisposal(irProgram, new(), false))
                            valuePromise(emitter);
                        else
                            recordProperty.Type.EmitCopyValue(irProgram, emitter, valuePromise, (e) => e.Append("nhp_self"));
                        emitter.AppendLine(';');
                    }, (e) => e.Append("nhp_self"), false);
#pragma warning restore CS8602 
                }

            emitter.Append($"{constructorCall.GetStandardIdentifier(irProgram)}(");
            for (int i = 0; i < constructorParameterTypes.Value.Count; i++)
                emitter.Append($"param{i}, ");
            emitter.AppendLine("nhp_self);");
            
            emitter.AppendLine("return nhp_self;");
            emitter.AppendEndBlock();
        }

        public void EmitDestructor(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"void free_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record");
            if (IsCircularDataStructure)
                emitter.Append(", nhp_trace_obj_t* freeing_parent");
            if (!RecordPrototype.EmitMultipleCStructs && destructorCall != null)
                emitter.Append(", nhp_custom_destructor destructor");
            emitter.AppendStartBlock(")");

            if (IsCircularDataStructure)
            {
                emitter.AppendLine($"if(!nhp_trace_del_parent((nhp_trace_obj_t*)record, freeing_parent)) {{ return; }}");
                emitter.AppendLine($"if(nhp_trace_reachable((nhp_trace_obj_t*)record)) {{ return; }}");
                emitter.AppendLine("if(record->trace_unit.nhp_lock) { return; } //lock for circular deletions");
                emitter.AppendLine("record->trace_unit.nhp_lock = 1;");
            }
            else
            {
                emitter.AppendStartBlock("if(record->rc_unit.nhp_count)");
                emitter.AppendLine("record->rc_unit.nhp_count--;");
                emitter.AppendLine("return;");
                emitter.AppendEndBlock();
            }

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
            if (copierCall != null || RecordPrototype.PassByReference)
                return;

            emitter.Append($"{GetCName(irProgram)} copy_record{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} record");
            if (IsCircularDataStructure)
                emitter.Append(", nhp_trace_obj_t* parent_record");
            emitter.AppendStartBlock(")");

            emitter.AppendLine($"{GetCName(irProgram)} copied_record = {irProgram.MemoryAnalyzer.Allocate(GetCHeapSizer(irProgram))};");
            if (IsCircularDataStructure)
            {
                emitter.AppendLine("copied_record->trace_unit.nhp_parent_count = 0;");
                emitter.AppendLine("copied_record->trace_unit.nhp_lock = 0;");
                emitter.AppendLine($"nhp_trace_add_parent((nhp_trace_obj_t*)copied_record, parent_record);");
            }
            else
                emitter.AppendLine("copied_record->rc_unit.nhp_count = 0;");

            foreach (RecordDeclaration.RecordProperty property in properties.Value)
            {
                if (!property.OptimizeMessageReciever)
                {
                    emitter.Append($"copied_record->{property.Name} = ");
                    property.Type.EmitRecordCopyValue(irProgram, emitter, (e) => e.Append($"record->{property.Name}"), (e) => e.Append("copied_record"));
                    emitter.AppendLine(";");
                }
            }

            emitter.AppendLine("return copied_record;");
            emitter.AppendEndBlock();
        }
    }
}