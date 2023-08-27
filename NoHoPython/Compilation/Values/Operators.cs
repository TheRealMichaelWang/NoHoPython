using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    //partial interface IPropertyContainer
    //{
    //    public void EmitGetProperty(IRProgram irProgram, IEmitter emitter, string valueCSource, string propertyIdentifier);
    //}

    partial class Property
    {
        public abstract bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs);

        public virtual void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public abstract bool EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource, string responsibleDestroyer);
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class BinaryOperator
    {
        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Left.ScopeForUsedTypes(typeargs, irBuilder);
            Right.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            BufferedEmitter leftBuilder = new();
            BufferedEmitter rightBuilder = new();

            if((!ShortCircuit && !IRValue.EvaluationOrderGuarenteed(Left, Right))
                || Left.RequiresDisposal(typeargs, true) || Right.RequiresDisposal(typeargs, true))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEnsureOrderOfEvaluation(this);

                irProgram.ExpressionDepth++;

                Left.Emit(irProgram, leftBuilder, typeargs, "NULL", true);
                Right.Emit(irProgram, rightBuilder, typeargs, "NULL", true);

                emitter.Append($"({{{Left.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} lhs{irProgram.ExpressionDepth} = {leftBuilder}; {Right.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} rhs{irProgram.ExpressionDepth} = {rightBuilder}; {Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} res{irProgram.ExpressionDepth} = ");
                EmitExpression(irProgram, emitter, typeargs, $"lhs{irProgram.ExpressionDepth}", $"rhs{irProgram.ExpressionDepth}");
                emitter.Append("; ");

                if (Left.RequiresDisposal(typeargs, true))
                    Left.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"lhs{irProgram.ExpressionDepth}", "NULL");

                if(Right.RequiresDisposal(typeargs, true))
                    Right.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"rhs{irProgram.ExpressionDepth}", "NULL");
                
                emitter.Append($"res{irProgram.ExpressionDepth};}})");
                irProgram.ExpressionDepth--;
            }
            else
            {
                IRValue.EmitMemorySafe(Left, irProgram, leftBuilder, typeargs);
                IRValue.EmitMemorySafe(Right, irProgram, rightBuilder, typeargs);
                EmitExpression(irProgram, emitter, typeargs, leftBuilder.ToString(), rightBuilder.ToString());
            } 
        }

        public abstract void EmitExpression(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource);
    }

    partial class ComparativeOperator
    {
        public override void EmitExpression(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
        {
            emitter.Append($"({leftCSource}");
            switch (Operation)
            {
                case CompareOperation.Equals:
                    emitter.Append(" == ");
                    break;
                case CompareOperation.NotEquals:
                    emitter.Append(" != ");
                    break;
                case CompareOperation.More:
                    emitter.Append(" > ");
                    break;
                case CompareOperation.Less:
                    emitter.Append(" < ");
                    break;
                case CompareOperation.MoreEqual:
                    emitter.Append(" >= ");
                    break;
                case CompareOperation.LessEqual:
                    emitter.Append(" <= ");
                    break;
            }
            emitter.Append($"{rightCSource})");
        }
    }

    partial class LogicalOperator
    {
        public override void EmitExpression(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
        {
            emitter.Append($"({leftCSource} ");
            switch (Operation)
            {
                case LogicalOperation.And:
                    emitter.Append("&&");
                    break;
                case LogicalOperation.Or:
                    emitter.Append("||");
                    break;
            }
            emitter.Append($" {rightCSource})");
        }
    }

    partial class BitwiseOperator
    {
        public override void EmitExpression(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
        {
            emitter.Append($"({leftCSource} ");
            switch (Operation)
            {
                case BitwiseOperation.And:
                    emitter.Append('&');
                    break;
                case BitwiseOperation.Or:
                    emitter.Append('|');
                    break;
                case BitwiseOperation.Xor:
                    emitter.Append('^');
                    break;
                case BitwiseOperation.ShiftLeft:
                    emitter.Append("<<");
                    break;
                case BitwiseOperation.ShiftRight:
                    emitter.Append(">>");
                    break;
            }
            emitter.Append($" {rightCSource})");
        }
    }

    partial class GetValueAtIndex
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Array.ScopeForUsedTypes(typeargs, irBuilder);
            Index.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (!IRValue.EvaluationOrderGuarenteed(Array, Index))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEnsureOrderOfEvaluation(this);

                irProgram.ExpressionDepth++;

                emitter.Append($"({{{Array.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arr{irProgram.ExpressionDepth} = ");
                IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs);

                emitter.Append($"; long ind{irProgram.ExpressionDepth} = ");
                if (irProgram.DoBoundsChecking)
                    ArrayType.EmitBoundsCheckedIndex(irProgram, emitter, typeargs, Array, Index, ErrorReportedElement);
                else
                    IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
                emitter.Append($"; arr{irProgram.ExpressionDepth}");

                if (Array.Type is ArrayType)
                    emitter.Append(".buffer");

                emitter.Append("[ind{irProgram.ExpressionDepth}];}})");

                irProgram.ExpressionDepth--;
            }
            else
            {
                IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs);

                if (Array.Type is ArrayType)
                    emitter.Append(".buffer");

                emitter.Append('[');
                if (irProgram.DoBoundsChecking)
                    ArrayType.EmitBoundsCheckedIndex(irProgram, emitter, typeargs, Array, Index, ErrorReportedElement);
                else
                    IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
                emitter.Append(']');        
            }
        }
    }

    partial class SetValueAtIndex
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Array.ScopeForUsedTypes(typeargs, irBuilder);
            Index.ScopeForUsedTypes(typeargs, irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            string arrayResponsibleDestroyer = ArrayType.GetResponsibleDestroyer(irProgram, typeargs, Array);
            if (!IRValue.EvaluationOrderGuarenteed(Array, Index, Value))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEnsureOrderOfEvaluation(this);

                irProgram.ExpressionDepth++;

                emitter.Append($"({{{Array.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arr{irProgram.ExpressionDepth} = ");
                IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs);
                emitter.Append($"; long ind{irProgram.ExpressionDepth} = ");

                if (irProgram.DoBoundsChecking)
                    ArrayType.EmitBoundsCheckedIndex(irProgram, emitter, typeargs, Array, Index, ErrorReportedElement);
                else
                    IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);

                emitter.Append(';');
                BufferedEmitter valueBuilder = new();
                if (Value.RequiresDisposal(typeargs, false))
                    Value.Emit(irProgram, valueBuilder, typeargs, arrayResponsibleDestroyer, false);
                else
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), arrayResponsibleDestroyer);

                Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Array.Type is ArrayType ? $"arr{irProgram.ExpressionDepth}.buffer[ind{irProgram.ExpressionDepth}]" : $"arr{irProgram.ExpressionDepth}[ind{irProgram.ExpressionDepth}]", valueBuilder.ToString(), arrayResponsibleDestroyer);
                emitter.Append(";})");

                irProgram.ExpressionDepth--;
            }
            else
            {
                BufferedEmitter destBuilder = new();
                IRValue.EmitMemorySafe(Array, irProgram, destBuilder, typeargs);

                if (Array.Type is ArrayType)
                    destBuilder.Append(".buffer");
                destBuilder.Append('[');
                if (irProgram.DoBoundsChecking)
                    ArrayType.EmitBoundsCheckedIndex(irProgram, destBuilder, typeargs, Array, Index, ErrorReportedElement);
                else
                    IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs);
                destBuilder.Append(']');

                BufferedEmitter valueBuilder = new();
                if (Value.RequiresDisposal(typeargs, false))
                    Value.Emit(irProgram, valueBuilder, typeargs, arrayResponsibleDestroyer, false);
                else
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), arrayResponsibleDestroyer);

                Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, destBuilder.ToString(), valueBuilder.ToString(), arrayResponsibleDestroyer);
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);

            string arrayResponsibleDestroyer = ArrayType.GetResponsibleDestroyer(irProgram, typeargs, Array);
            if (!IRValue.EvaluationOrderGuarenteed(Array, Index, Value))
            {
                emitter.AppendLine("{");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.Append($"{Array.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arr = ");
                IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs);
                emitter.AppendLine(";");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.Append("long ind = ");
                if (irProgram.DoBoundsChecking)
                    ArrayType.EmitBoundsCheckedIndex(irProgram, emitter, typeargs, Array, Index, ErrorReportedElement);
                else
                    IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
                emitter.AppendLine(";");

                BufferedEmitter valueBuilder = new();
                if (Value.RequiresDisposal(typeargs, false))
                    Value.Emit(irProgram, valueBuilder, typeargs, arrayResponsibleDestroyer, false);
                else
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), arrayResponsibleDestroyer);

                CodeBlock.CIndent(emitter, indent + 1);
                Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, "arr.buffer[ind]", valueBuilder.ToString(), arrayResponsibleDestroyer);
                emitter.AppendLine(";");
                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine("}");
            }
            else
            {
                Emit(irProgram, emitter, typeargs, "NULL", false);
                emitter.AppendLine(";");
            }
        }
    }

    partial class GetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Property.ScopeForUse(false, typeargs, irBuilder);
            Record.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Property.RequiresDisposal(typeargs);

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (Record.Type.SubstituteWithTypearg(typeargs) is IPropertyContainer propertyContainer)
            {
                if(Refinements.HasValue && Refinements.Value.Item2 != null)
                {
                    BufferedEmitter propertyGet = new();
                    Property.EmitGet(irProgram, propertyGet, typeargs, propertyContainer, BufferedEmitter.EmittedBufferedMemorySafe(Record, irProgram, typeargs), responsibleDestroyer);
                    Refinements.Value.Item2(irProgram, emitter, propertyGet.ToString(), typeargs);
                }
                else
                    Property.EmitGet(irProgram, emitter, typeargs, propertyContainer, BufferedEmitter.EmittedBufferedMemorySafe(Record, irProgram, typeargs), responsibleDestroyer);
            }
            else
                throw new UnexpectedTypeException(Record.Type.SubstituteWithTypearg(typeargs), ErrorReportedElement);
        }
    }

    partial class SetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Record.ScopeForUsedTypes(typeargs, irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (!IRValue.EvaluationOrderGuarenteed(Record, Value))
            {
                irProgram.ExpressionDepth++;

                emitter.Append($"({{{Record.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} record{irProgram.ExpressionDepth} = ");
                IRValue.EmitMemorySafe(Record, irProgram, emitter, typeargs);
                emitter.Append($"; {Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value{irProgram.ExpressionDepth} = ");

                if (Value.RequiresDisposal(typeargs, false))
                    Value.Emit(irProgram, emitter, typeargs, $"record{irProgram.ExpressionDepth}", false);
                else
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), $"record{irProgram.ExpressionDepth}");

                emitter.Append(';');
                if (IsInitializingProperty)
                    emitter.Append($"(record{irProgram.ExpressionDepth}->{Property.Name} = value{irProgram.ExpressionDepth});}})");
                else
                {
                    Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"record{irProgram.ExpressionDepth}->{Property.Name}", $"value{irProgram.ExpressionDepth}", $"record{irProgram.ExpressionDepth}");
                    emitter.Append(";})");
                }
                
                irProgram.ExpressionDepth--;
            }
            else
            {
                string recordCSource = BufferedEmitter.EmittedBufferedMemorySafe(Record, irProgram, typeargs);
                string recordResponsibleDestroyer = BufferedEmitter.EmittedBufferedMemorySafe(Record.GetPostEvalPure(), irProgram, typeargs);

                if (IsInitializingProperty)
                {
                    emitter.Append($"({recordCSource}->{Property.Name} = ");
                    if (Value.RequiresDisposal(typeargs, false))
                        Value.Emit(irProgram, emitter, typeargs, recordResponsibleDestroyer, false);
                    else
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), recordResponsibleDestroyer.ToString());
                    emitter.Append(')');
                }
                else
                {
                    BufferedEmitter toCopyBuilder = new();
                    if (Value.RequiresDisposal(typeargs, false))
                        Value.Emit(irProgram, toCopyBuilder, typeargs, recordResponsibleDestroyer.ToString(), false);
                    else
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, toCopyBuilder, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), recordResponsibleDestroyer.ToString());
                    Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"{recordCSource}->{Property.Name}", toCopyBuilder.ToString(), recordCSource);
                }
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            if (!IRValue.EvaluationOrderGuarenteed(Record, Value))
            {
                emitter.AppendLine("{");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.Append($"{Record.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} record = ");
                IRValue.EmitMemorySafe(Record, irProgram, emitter, typeargs);
                emitter.AppendLine(";");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.Append($"{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value = ");
                if (Value.RequiresDisposal(typeargs, false))
                    Value.Emit(irProgram, emitter, typeargs, "record", false);
                else
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(Value, irProgram, typeargs, "NULL"), "record");
                emitter.AppendLine(";");

                CodeBlock.CIndent(emitter, indent + 1);
                if (IsInitializingProperty)
                    emitter.AppendLine($"record->{Property.Name} = value;");
                else
                {
                    Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"record->{Property.Name}", "value", "record");
                    emitter.AppendLine(";");
                }
                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine("}");
            }
            else
            {
                Emit(irProgram, emitter, typeargs, "NULL", false);
                emitter.AppendLine(";");
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ArrayType
    {
        public static void EmitBoundsCheckedIndex(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IRValue array, IRValue index, Syntax.IAstElement errorReportedElement)
        {
            emitter.Append("nhp_bounds_check(");
            IRValue.EmitMemorySafe(index, irProgram, emitter, typeargs);
            emitter.Append(", ");
            if (array.Type is ArrayType)
            {
                IRValue.EmitMemorySafe(array.GetPostEvalPure(), irProgram, emitter, typeargs);
                emitter.Append(".length, ");
            }
            else
            {
                MemorySpan memorySpan = (MemorySpan)array.Type;
                emitter.Append($"{memorySpan.Length}, ");
            }
            CharacterLiteral.EmitCString(emitter, errorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            errorReportedElement.EmitSrcAsCString(emitter);
            emitter.Append(')');
        }

        public static string GetResponsibleDestroyer(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, IRValue array)
        {
            IRValue? responsibleDestroyer = array.GetResponsibleDestroyer();
            if (responsibleDestroyer == null)
                return "NULL";
            else
            {
                BufferedEmitter bufferedEmitter = new();
                responsibleDestroyer.Emit(irProgram, bufferedEmitter, typeargs, "NULL", false);
                return bufferedEmitter.ToString();
            }
        }
    }
}