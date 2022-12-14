using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Typing;
using System.Text;

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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            StringBuilder leftBuilder = new();
            StringBuilder rightBuilder = new();

            if((!ShortCircuit && (!Left.IsPure && !Right.IsConstant) || (!Right.IsPure && !Left.IsConstant))
                || Left.RequiresDisposal(typeargs) || Right.RequiresDisposal(typeargs))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEnsureOrderOfEvaluation(this);

                irProgram.ExpressionDepth++;

                Left.Emit(irProgram, leftBuilder, typeargs, "NULL");
                Right.Emit(irProgram, rightBuilder, typeargs, "NULL");

                emitter.Append($"({{{Left.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} lhs{irProgram.ExpressionDepth} = {leftBuilder.ToString()}; {Right.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} rhs{irProgram.ExpressionDepth} = {rightBuilder.ToString()}; {Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} res{irProgram.ExpressionDepth} = ");
                EmitExpression(irProgram, emitter, typeargs, $"lhs{irProgram.ExpressionDepth}", $"rhs{irProgram.ExpressionDepth}");
                emitter.Append("; ");

                if (Left.RequiresDisposal(typeargs))
                    Left.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"lhs{irProgram.ExpressionDepth}", "NULL");

                if(Right.RequiresDisposal(typeargs))
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

        public abstract void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource);
    }

    partial class ComparativeOperator
    {
        public override void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
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
        public override void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
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
        public override void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if ((!Array.IsPure && !Index.IsConstant) ||
                (!Index.IsPure && !Array.IsConstant))
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
                emitter.Append($"; arr{irProgram.ExpressionDepth}.buffer[ind{irProgram.ExpressionDepth}];}})");

                irProgram.ExpressionDepth--;
            }
            else
            {
                IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs);
                if (irProgram.DoBoundsChecking)
                {
                    emitter.Append(".buffer[");
                    ArrayType.EmitBoundsCheckedIndex(irProgram, emitter, typeargs, Array, Index, ErrorReportedElement);
                    emitter.Append(']');
                }
                else
                {
                    emitter.Append(".buffer[");
                    IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
                    emitter.Append(']');
                }
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if ((!Array.IsPure && (!Index.IsConstant || !Value.IsConstant)) ||
               (!Index.IsPure && (!Array.IsConstant || !Value.IsConstant)) ||
               (!Value.IsPure && (!Index.IsConstant || !Array.IsConstant)))
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
                StringBuilder valueBuilder = new();
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, valueBuilder, typeargs, $"arr{irProgram.ExpressionDepth}.responsible_destroyer");
                else
                {
                    StringBuilder toCopyBuilder = new();
                    Value.Emit(irProgram, toCopyBuilder, typeargs, "NULL");
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, toCopyBuilder.ToString(), $"arr{irProgram.ExpressionDepth}.responsible_destroyer");
                }

                Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"arr{irProgram.ExpressionDepth}.buffer[ind{irProgram.ExpressionDepth}]", valueBuilder.ToString());
                emitter.Append(";})");

                irProgram.ExpressionDepth--;
            }
            else
            {
                StringBuilder destBuilder = new();
                IRValue.EmitMemorySafe(Array, irProgram, destBuilder, typeargs);
                if (irProgram.DoBoundsChecking)
                {
                    destBuilder.Append(".buffer[");
                    ArrayType.EmitBoundsCheckedIndex(irProgram, destBuilder, typeargs, Array, Index, ErrorReportedElement);
                    destBuilder.Append(']');
                }
                else
                {
                    destBuilder.Append(".buffer[");
                    IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs);
                    destBuilder.Append(']');
                }

                StringBuilder arrayResponsibleDestructor = new();
                IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, arrayResponsibleDestructor, typeargs);
                arrayResponsibleDestructor.Append(".responsible_destroyer");

                StringBuilder valueBuilder = new();
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, valueBuilder, typeargs, arrayResponsibleDestructor.ToString());
                else
                {
                    StringBuilder toCopyBuilder = new();
                    Value.Emit(irProgram, toCopyBuilder, typeargs, "NULL");
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, toCopyBuilder.ToString(), arrayResponsibleDestructor.ToString());
                }

                Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, destBuilder.ToString(), valueBuilder.ToString());
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);

            if ((!Array.IsPure && (!Index.IsConstant || !Value.IsConstant)) ||
               (!Index.IsPure && (!Array.IsConstant || !Value.IsConstant)) ||
               (!Value.IsPure && (!Index.IsConstant || !Array.IsConstant)))
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

                StringBuilder valueBuilder = new();
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, valueBuilder, typeargs, "arr.responsible_destroyer");
                else
                {
                    StringBuilder toCopyBuilder = new();
                    Value.Emit(irProgram, toCopyBuilder, typeargs, "NULL");
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, toCopyBuilder.ToString(), "arr.responsible_destroyer");
                }

                CodeBlock.CIndent(emitter, indent + 1);
                Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, "arr.buffer[ind]", valueBuilder.ToString());
                emitter.AppendLine(";");
                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine("}");
            }
            else
            {
                Emit(irProgram, emitter, typeargs, "NULL");
                emitter.AppendLine(";");
            }
        }
    }

    partial class GetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Record.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            StringBuilder valueBuilder = new();
            IRValue.EmitMemorySafe(Record, irProgram, valueBuilder, typeargs);

            if (Record.Type.SubstituteWithTypearg(typeargs) is IPropertyContainer propertyContainer)
                propertyContainer.EmitGetProperty(irProgram, emitter, valueBuilder.ToString(), Property);
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if ((!Record.IsPure && !Value.IsConstant) || (!Value.IsPure && !Record.IsConstant))
            {
                irProgram.ExpressionDepth++;

                emitter.Append($"({{{Record.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} record{irProgram.ExpressionDepth} = ");
                IRValue.EmitMemorySafe(Record, irProgram, emitter, typeargs);
                emitter.Append($"; {Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value{irProgram.ExpressionDepth} = ");

                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, emitter, typeargs, $"record{irProgram.ExpressionDepth}->_nhp_responsible_destroyer");
                else
                {
                    StringBuilder valueBuilder = new();
                    Value.Emit(irProgram, valueBuilder, typeargs, "NULL");
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), $"record{irProgram.ExpressionDepth}->_nhp_responsible_destroyer");
                }

                emitter.Append(';');
                if (IsInitializingProperty)
                    emitter.Append($"(record{irProgram.ExpressionDepth}->{Property.Name} = value{irProgram.ExpressionDepth});}})");
                else
                {
                    Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"record{irProgram.ExpressionDepth}->{Property.Name}", $"value{irProgram.ExpressionDepth}");
                    emitter.Append(";})");
                }
                
                irProgram.ExpressionDepth--;
            }
            else
            {
                StringBuilder recordBuilder = new();
                IRValue.EmitMemorySafe(Record, irProgram, recordBuilder, typeargs);

                StringBuilder recordResponsibleDestroyer = new();
                IRValue.EmitMemorySafe(Record.GetPostEvalPure(), irProgram, recordResponsibleDestroyer, typeargs);
                recordResponsibleDestroyer.Append("->_nhp_responsible_destroyer");

                if (IsInitializingProperty)
                {
                    emitter.Append($"({recordBuilder}->{Property.Name} = ");
                    if (Value.RequiresDisposal(typeargs))
                        Value.Emit(irProgram, emitter, typeargs, recordResponsibleDestroyer.ToString());
                    else
                    {
                        StringBuilder valueBuilder = new();
                        Value.Emit(irProgram, valueBuilder, typeargs, "NULL");
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), recordResponsibleDestroyer.ToString());
                    }
                    emitter.Append(')');
                }
                else
                {
                    StringBuilder toCopyBuilder = new();
                    if (Value.RequiresDisposal(typeargs))
                        Value.Emit(irProgram, toCopyBuilder, typeargs, recordResponsibleDestroyer.ToString());
                    else
                    {
                        StringBuilder valueBuilder = new();
                        Value.Emit(irProgram, valueBuilder, typeargs, "NULL");
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, toCopyBuilder, valueBuilder.ToString(), recordResponsibleDestroyer.ToString());
                    }
                    Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"{recordBuilder}->{Property.Name}", toCopyBuilder.ToString());
                }
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            if ((!Record.IsPure && !Value.IsConstant) || (!Value.IsPure && !Record.IsConstant))
            {
                emitter.AppendLine("{");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.Append($"{Record.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} record = ");
                IRValue.EmitMemorySafe(Record, irProgram, emitter, typeargs);
                emitter.AppendLine(";");

                CodeBlock.CIndent(emitter, indent + 1);
                emitter.Append($"{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value = ");
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, emitter, typeargs, "record->_nhp_responsible_destroyer");
                else
                {
                    StringBuilder valueBuilder = new();
                    Value.Emit(irProgram, valueBuilder, typeargs, "NULL");
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), "record->_nhp_responsible_destroyer");
                }
                emitter.AppendLine(";");

                CodeBlock.CIndent(emitter, indent + 1);
                if (IsInitializingProperty)
                    emitter.AppendLine($"record->{Property.Name} = value;");
                else
                {
                    Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"record->{Property.Name}", "value");
                    emitter.AppendLine(";");
                }
                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine("}");
            }
            else
            {
                Emit(irProgram, emitter, typeargs, "NULL");
                emitter.AppendLine(";");
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ArrayType
    {
        public static void EmitBoundsCheckedIndex(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, IRValue array, IRValue index, Syntax.IAstElement errorReportedElement)
        {
            emitter.Append("_nhp_bounds_check(");
            IRValue.EmitMemorySafe(index, irProgram, emitter, typeargs);
            emitter.Append(", ");
            IRValue.EmitMemorySafe(array.GetPostEvalPure(), irProgram, emitter, typeargs);
            emitter.Append(".length, ");
            CharacterLiteral.EmitCString(emitter, errorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            errorReportedElement.EmitSrcAsCString(emitter);
            emitter.Append(')');
        }
    }
}