﻿using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class SizeofOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => TypeToMeasure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append($"sizeof({TypeToMeasure.SubstituteWithTypearg(typeargs).GetCName(irProgram)})");
    }

    partial class MemoryGet
    {
        public override void EmitExpression(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource) => emitter.Append($"(({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*){leftCSource})[{rightCSource}]");
    }

    partial class MemorySet
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
            Index.ScopeForUsedTypes(typeargs, irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if (!IRValue.EvaluationOrderGuarenteed(Address, Index, Value))
                throw new CannotEnsureOrderOfEvaluation(this);

            emitter.Append($"((({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
            IRValue.EmitMemorySafe(Address, irProgram, emitter, typeargs);
            emitter.Append(")[");
            IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
            emitter.Append("] = ");

            string heapResponsibleDestroyer = ArrayType.GetResponsibleDestroyer(irProgram, typeargs, Address);

            if (Value.RequiresDisposal(typeargs))
                Value.Emit(irProgram, emitter, typeargs, heapResponsibleDestroyer);
            else
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), heapResponsibleDestroyer);
            emitter.Append(')');
        }
        
        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
        }
    }

    partial class MarshalHandleIntoArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Length.ScopeForUsedTypes(typeargs, irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if (!IRValue.EvaluationOrderGuarenteed(Address, Length))
                throw new CannotEnsureOrderOfEvaluation(this);

            ArrayType type = new(ElementType.SubstituteWithTypearg(typeargs));
            if(ElementType.SubstituteWithTypearg(typeargs).RequiresDisposal)
                emitter.Append($"marshal_foreign{type.GetStandardIdentifier(irProgram)}(");
            else
                emitter.Append($"marshal{type.GetStandardIdentifier(irProgram)}(");
            
            IRValue.EmitMemorySafe(Address, irProgram, emitter, typeargs);
            emitter.Append(", ");
            IRValue.EmitMemorySafe(Length, irProgram, emitter, typeargs);

            if (type.MustSetResponsibleDestroyer)
                emitter.Append($", {responsibleDestroyer})");
            else
                emitter.Append(')');
        }
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Span.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            ArrayType type = new(ElementType.SubstituteWithTypearg(typeargs));
            MemorySpan memorySpan = (MemorySpan)Span.Type.SubstituteWithTypearg(typeargs);

            if (Span.RequiresDisposal(typeargs))
            {
                emitter.Append($"(({type.GetCName(irProgram)}){{ .buffer = ");
                Span.Emit(irProgram, emitter, typeargs, responsibleDestroyer);
                emitter.Append($", .length = {memorySpan.Length}}})");
            }
            else
            {
                if (ElementType.SubstituteWithTypearg(typeargs).RequiresDisposal)
                    emitter.Append($"marshal_foreign{type.GetStandardIdentifier(irProgram)}(");
                else
                    emitter.Append($"marshal{type.GetStandardIdentifier(irProgram)}(");

                Span.Emit(irProgram, emitter, typeargs, "NULL");
                emitter.Append($", {memorySpan.Length}");

                if (type.MustSetResponsibleDestroyer)
                    emitter.Append($", {responsibleDestroyer})");
                else
                    emitter.Append(')');
            }
        }
    }

    partial class MarshalIntoLowerTuple
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypeParameters(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => TargetType.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            List<Property> targetProperties = ((TupleType)TargetType.SubstituteWithTypearg(typeargs)).GetProperties();

            if (Value.IsPure && !Value.RequiresDisposal(typeargs))
            {
                emitter.Append($"({TargetType.GetCName(irProgram)}){{");
                foreach(Property property in targetProperties)
                {
                    if (property != targetProperties.First())
                        emitter.Append(", ");

                    emitter.Append($".{property.Name} = ");

                    BufferedEmitter higherValueProperty = new();
                    IRValue.EmitMemorySafe(Value, irProgram, higherValueProperty, typeargs);
                    higherValueProperty.Append($".{property.Name}");
                    property.Type.EmitCopyValue(irProgram, emitter, higherValueProperty.ToString(), responsibleDestroyer);
                }
                emitter.Append('}');
            }
            else
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEmitDestructorError(Value);

                irProgram.ExpressionDepth++;
                emitter.Append($"({{{Value.Type.GetCName(irProgram)} higher_tuple{irProgram.ExpressionDepth} = ");
                Value.Emit(irProgram, emitter, typeargs, "NULL");
                emitter.Append(';');

                if (Value.RequiresDisposal(typeargs))
                    emitter.Append($"{TargetType.GetCName(irProgram)} res{irProgram.ExpressionDepth} = ");
                
                emitter.Append($"({TargetType.GetCName(irProgram)}){{");
                foreach (Property property in targetProperties)
                {
                    if(property != targetProperties.First())
                        emitter.Append(", ");

                    emitter.Append($".{property.Name} = ");
                    if (Value.RequiresDisposal(typeargs))
                        emitter.Append($"higher_tuple{irProgram.ExpressionDepth}.{property.Name};");
                    else
                    {
                        property.Type.EmitCopyValue(irProgram, emitter, $"higher_tuple{irProgram.ExpressionDepth}.{property.Name}", responsibleDestroyer);
                        emitter.Append(';');
                    }
                }
                emitter.Append("};");

                if (Value.RequiresDisposal(typeargs))
                {
                    TupleType targetTupleType = (TupleType)TargetType.SubstituteWithTypearg(typeargs);
                    foreach (KeyValuePair<IType, int> valueType in ((TupleType)Value.Type.SubstituteWithTypearg(typeargs)).ValueTypes)
                    {
                        if (valueType.Key.RequiresDisposal)
                        {
                            if (!targetTupleType.ValueTypes.ContainsKey(valueType.Key))
                            {
                                for(int i = 0; i < valueType.Value; i++)
                                    valueType.Key.EmitFreeValue(irProgram, emitter, $"higher_tuple{irProgram.ExpressionDepth}.{valueType.Key.Identifier}{i}", "NULL");
                            }
                            else
                            {
                                for (int i = targetTupleType.ValueTypes[valueType.Key]; i < valueType.Value; i++)
                                    valueType.Key.EmitFreeValue(irProgram, emitter, $"higher_tuple{irProgram.ExpressionDepth}.{valueType.Key.Identifier}{i}", "NULL");
                            }
                        }
                    }
                    emitter.Append($"res{irProgram.ExpressionDepth};");
                }
                else
                    emitter.Append("})");

                irProgram.ExpressionDepth--;
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class MemoryDestroy
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
            if (Index != null)
                Index.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                CodeBlock.CIndent(emitter, indent);
                
                BufferedEmitter valueBuilder = new();
                valueBuilder.Append($"(*(({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
                IRValue.EmitMemorySafe(Address, irProgram, valueBuilder, typeargs);
                if (Index != null)
                {
                    valueBuilder.Append(" + ");
                    IRValue.EmitMemorySafe(Index, irProgram, valueBuilder, typeargs);
                }
                valueBuilder.Append("))");
                Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, valueBuilder.ToString(), "NULL");
                emitter.AppendLine();
            }
        }
    }
}