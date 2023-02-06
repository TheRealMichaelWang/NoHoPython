using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

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
            typeDependencyTree.Add(tupleType, new HashSet<IType>(tupleType.ValueTypes.Keys.Where((type) => type is not RecordType)));

            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<TupleType> usedTupleTypes;

        private void EmitTupleTypeTypedefs(StatementEmitter emitter)
        {
            foreach(TupleType usedTupleType in usedTupleTypes)
                emitter.AppendLine($"typedef struct {usedTupleType.GetStandardIdentifier(this)} {usedTupleType.GetCName(this)};");
        }

        private void EmitTupleCStructs(StatementEmitter emitter)
        {
            foreach(TupleType usedTupleType in usedTupleTypes)
                usedTupleType.EmitCStruct(this, emitter);
        }

        private void ForwardDeclareTupleTypes(StatementEmitter emitter)
        {
            foreach(TupleType usedTupleType in usedTupleTypes)
            {
                usedTupleType.EmitDestructorHeader(this, emitter);
                emitter.AppendLine(";");
                usedTupleType.EmitMoverHeader(this, emitter);
                emitter.AppendLine(";");
            }
        }

        private void EmitTupleTypeMarshallers(StatementEmitter emitter)
        {
            foreach (TupleType usedTupleType in usedTupleTypes)
            {
                usedTupleType.EmitDestructor(this, emitter);
                usedTupleType.EmitMover(this, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class TupleType
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => ValueTypes.Keys.Any((type) => type.RequiresDisposal);
        public bool MustSetResponsibleDestroyer => ValueTypes.Keys.Any((type) => type.MustSetResponsibleDestroyer);

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_tuple_{Identifier}";
        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent)
        {
            if (RequiresDisposal)
            {
                emitter.Append($"free_{GetStandardIdentifier(irProgram)}({valueCSource}");
                if (MustSetResponsibleDestroyer)
                    emitter.Append($", {childAgent}");
                emitter.Append(");");
            }
        }

        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if (RequiresDisposal)
            {
                emitter.Append($"({GetCName(irProgram)}){{");

                bool emitCommaSeparator = false;
                foreach (KeyValuePair<IType, int> valuePair in ValueTypes)
                {
                    for (int i = 0; i < valuePair.Value; i++)
                    {
                        if (emitCommaSeparator)
                            emitter.Append(", ");
                        else
                            emitCommaSeparator = true;

                        emitter.Append($".{valuePair.Key.Identifier}{i} = ");
                        valuePair.Key.EmitCopyValue(irProgram, emitter, $"{valueCSource}.{valuePair.Key.Identifier}{i}", responsibleDestroyer);
                    }
                }
                emitter.Append('}');
            }
            else
                emitter.Append(valueCSource);
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (RequiresDisposal)
            {
                if (irProgram.EmitExpressionStatements)
                    IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
                else
                {
                    emitter.Append($"move{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource}");
                    if (MustSetResponsibleDestroyer)
                        emitter.Append($", {childAgent})");
                }
            }
            else
                emitter.Append($"({destC} = {valueCSource})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, IEmitter emitter, string valueCSource, string newResponsibleDestroyer)
        {
            if (MustSetResponsibleDestroyer)
            {
                emitter.Append($"({GetCName(irProgram)}){{");

                bool emitCommaSeparator = false;
                foreach (KeyValuePair<IType, int> valuePair in ValueTypes)
                {
                    for (int i = 0; i < valuePair.Value; i++)
                    {
                        if (emitCommaSeparator)
                            emitter.Append(", ");
                        else
                            emitCommaSeparator = true;

                        emitter.Append($".{valuePair.Key.Identifier}{i} = ");
                        valuePair.Key.EmitMutateResponsibleDestroyer(irProgram, emitter, $"{valueCSource}.{valuePair.Key.Identifier}{i}", newResponsibleDestroyer);
                    }
                }
                emitter.Append('}');
            }
            else
                emitter.Append(valueCSource);
        }

        public void EmitGetProperty(IRProgram irProgram, IEmitter emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}.{property.Name}");

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedTupleType(this))
            {
                foreach (IType type in ValueTypes.Keys)
                    type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine($"struct _nhp_tuple_{Identifier} {{");
            foreach(KeyValuePair<IType, int> valuePair in ValueTypes)
            {
                for (int i = 0; i < valuePair.Value; i++)
                    emitter.AppendLine($"\t{valuePair.Key.GetCName(irProgram)} {valuePair.Key.Identifier}{i};");
            }
            emitter.AppendLine("};");
        }

        public void EmitDestructorHeader(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"void free_{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} to_free");
            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* child_agent");
            emitter.Append(')');
        }

        public void EmitMoverHeader(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} move{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src");
            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* child_agent");
            emitter.Append(')');
        }

        public void EmitDestructor(IRProgram irProgram, StatementEmitter emitter)
        {
            EmitDestructorHeader(irProgram, emitter);
            emitter.AppendLine(" {");

            foreach (KeyValuePair<IType, int> valuePair in ValueTypes)
            {
                if (valuePair.Key.RequiresDisposal) {
                    for (int i = 0; i < valuePair.Value; i++)
                    {
                        emitter.Append('\t');
                        valuePair.Key.EmitFreeValue(irProgram, emitter, $"to_free.{valuePair.Key.Identifier}{i}", "child_agent");
                        emitter.AppendLine();
                    }
                }
            }

            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            EmitMoverHeader(irProgram, emitter);
            emitter.AppendLine("{");
            emitter.Append("\t");
            EmitFreeValue(irProgram, emitter, "*dest", "child_agent");
            emitter.AppendLine();
            emitter.Append("\t*dest = ");
            EmitCopyValue(irProgram, emitter, "src", "child_agent");
            emitter.AppendLine(";");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }
    }
}