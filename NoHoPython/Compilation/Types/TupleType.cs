using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<TupleType> usedTupleTypes = new(new ITypeComparer());

        public bool DeclareUsedTupleType(TupleType tupleType)
        {
            if (usedTupleTypes.Contains(tupleType))
                return false;

            usedTupleTypes.Add(tupleType);

            DeclareTypeDependencies(tupleType, tupleType.ValueTypes.Keys.ToArray());

            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<TupleType> usedTupleTypes;

        private void EmitTupleTypeTypedefs(Emitter emitter)
        {
            foreach(TupleType usedTupleType in usedTupleTypes)
                emitter.AppendLine($"typedef struct {usedTupleType.GetStandardIdentifier(this)} {usedTupleType.GetCName(this)};");
        }

        private void EmitTupleCStructs(Emitter emitter)
        {
            foreach(TupleType usedTupleType in usedTupleTypes)
                usedTupleType.EmitCStruct(this, emitter);
        }

        private void ForwardDeclareTupleTypes(Emitter emitter)
        {
            foreach(TupleType usedTupleType in usedTupleTypes)
            {
                if (!usedTupleType.RequiresDisposal)
                    continue;

                usedTupleType.EmitCopierHeader(this, emitter);
                emitter.AppendLine(";");
            }
        }

        private void EmitTupleTypeMarshallers(Emitter emitter)
        {
            foreach (TupleType usedTupleType in usedTupleTypes)
            {
                if (!usedTupleType.RequiresDisposal)
                    continue;

                usedTupleType.EmitCopier(this, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class TupleType
    {
        partial class TupleProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
            {
                Debug.Assert(propertyContainer is TupleType);

                IType realPropertyType = Type.SubstituteWithTypearg(typeargs);
                Debug.Assert(realPropertyType is not TypeParameterReference);

                value(emitter);
                emitter.Append($".{realPropertyType.Identifier}{TypeNumber}");
                return false;
            }
        }

        public bool IsNativeCType => true;
        public bool RequiresDisposal => ValueTypes.Keys.Any((type) => type.RequiresDisposal);
        public bool MustSetResponsibleDestroyer => ValueTypes.Keys.Any((type) => type.MustSetResponsibleDestroyer);
        public bool IsTypeDependency => true;

        public string? GetInvalidState() => null;
        public Emitter.SetPromise? IsInvalid(Emitter emitter) => null;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo)
        {
            if (effectInfo.ContainsKey(this))
                return effectInfo[this];
            
            effectInfo.Add(this, false);
            return effectInfo[this] = ValueTypes.Keys.Any((type) => type.TypeParameterAffectsCodegen(effectInfo));
        }

        public string GetStandardIdentifier(IRProgram irProgram) => $"nhp_tuple_{Identifier}";
        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            if (RequiresDisposal)
            {
                int indirection = emitter.AppendStartBlock();
                emitter.Append($"{GetCName(irProgram)} to_free{indirection} = ");
                valuePromise(emitter);
                emitter.AppendLine(";");

                foreach (KeyValuePair<IType, int> valuePair in ValueTypes)
                {
                    if (valuePair.Key.RequiresDisposal)
                        for (int i = 0; i < valuePair.Value; i++)
                            valuePair.Key.EmitFreeValue(irProgram, emitter, (e) => e.Append($"to_free{indirection}.{valuePair.Key.Identifier}{i}"), childAgent);
                }
                emitter.AppendEndBlock();
            }
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
        {
            if (RequiresDisposal)
            {
                emitter.Append($"copy_{GetStandardIdentifier(irProgram)}(");
                valueCSource(emitter);
                if (MustSetResponsibleDestroyer)
                {
                    emitter.Append(", ");
                    responsibleDestroyer(emitter);
                }
                emitter.Append(')');
            }    
            else
                valueCSource(emitter);
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer, null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord, null);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedTupleType(this))
            {
                foreach (IType type in ValueTypes.Keys)
                    type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine($"struct nhp_tuple_{Identifier} {{");
            foreach(KeyValuePair<IType, int> valuePair in ValueTypes)
            {
                for (int i = 0; i < valuePair.Value; i++)
                    emitter.AppendLine($"\t{valuePair.Key.GetCName(irProgram)} {valuePair.Key.Identifier}{i};");
            }
            emitter.AppendLine("};");
        }

        public void EmitCopierHeader(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_copy");
            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");
            emitter.Append(')');
        }

        public void EmitCopier(IRProgram irProgram, Emitter emitter)
        {
            EmitCopierHeader(irProgram, emitter);
            emitter.AppendLine(" {");
            emitter.AppendLine($"\t{GetCName(irProgram)} to_ret;");
            foreach (KeyValuePair<IType, int> valuePair in ValueTypes)
            {
                for (int i = 0; i < valuePair.Value; i++)
                {
                    emitter.Append($"\tto_ret.{valuePair.Key.Identifier}{i} = ");
                    valuePair.Key.EmitCopyValue(irProgram, emitter, (e) => e.Append($"to_copy.{valuePair.Key.Identifier}{i}"), (e) => e.Append("responsible_destroyer"), null);
                    emitter.AppendLine(";");
                }
            }
            emitter.AppendLine("\treturn to_ret;");
            emitter.AppendLine("}");
        }
    }
}