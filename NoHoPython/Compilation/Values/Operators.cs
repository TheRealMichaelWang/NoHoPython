using NoHoPython.IntermediateRepresentation.Statements;
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

            if (((!Left.IsPure && !Right.IsConstant) || (!Right.IsPure && !Left.IsConstant) ||
                Left.RequiresDisposal(typeargs) || (Right.RequiresDisposal(typeargs) && !isAssignmentOperator) || (ensureLeftIsMemoryPure && !Left.IsPure)) && !shortCircuit)
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEnsureOrderOfEvaluation(this);
                if (isAssignmentOperator && Left.RequiresDisposal(typeargs))
                    throw new CannotEmitDestructorError(Left);

                irProgram.ExpressionDepth++;

                Left.Emit(irProgram, leftBuilder, typeargs, "NULL");
                Right.Emit(irProgram, rightBuilder, typeargs, isAssignmentOperator ? $"{leftBuilder}->_nhp_responsible_destroyer" : "NULL");

                emitter.Append($"({{{Left.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} lhs{irProgram.ExpressionDepth} = {leftBuilder.ToString()}; {Right.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} rhs{irProgram.ExpressionDepth} = {rightBuilder.ToString()}; {Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} res{irProgram.ExpressionDepth} = ");
                EmitExpression(irProgram, emitter, typeargs, $"lhs{irProgram.ExpressionDepth}", $"rhs{irProgram.ExpressionDepth}");
                emitter.Append("; ");

                if (Left.RequiresDisposal(typeargs))
                    Left.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"lhs{irProgram.ExpressionDepth}");

                if(Right.RequiresDisposal(typeargs) && !isAssignmentOperator)
                    Right.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"rhs{irProgram.ExpressionDepth}");
                
                emitter.Append($"res{irProgram.ExpressionDepth};}})");
                irProgram.ExpressionDepth--;
            }
            else
            {
                IRValue.EmitMemorySafe(Left, irProgram, leftBuilder, typeargs);
                if (isAssignmentOperator)
                {
                    StringBuilder valueBuilder = new();
                    Right.Emit(irProgram, valueBuilder, typeargs, "NULL");
                    Right.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, rightBuilder, valueBuilder.ToString(), $"{leftBuilder}->_nhp_responsible_destroyer");
                }
                else
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
            emitter.Append($"({leftCSource}");
            switch (Operation)
            {
                case LogicalOperation.And:
                    emitter.Append(" && ");
                    break;
                case LogicalOperation.Or:
                    emitter.Append(" || ");
                    break;
            }
            emitter.Append($"{rightCSource})");
        }
    }

    partial class GetValueAtIndex
    {
        public override void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
        {
            emitter.Append(leftCSource);
            if (irProgram.DoBoundsChecking)
            {
                emitter.Append($".buffer[_nhp_bounds_check({rightCSource}, {leftCSource}.length, ");
                CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString(), false, true);
                emitter.Append(", ");
                if (ErrorReportedElement is Syntax.IAstStatement statement)
                    CharacterLiteral.EmitCString(emitter, statement.ToString(0), false, true);
                else if (ErrorReportedElement is Syntax.IAstValue value)
                    CharacterLiteral.EmitCString(emitter, value.ToString(), false, true);
                emitter.Append(")]");
            }
            else
                emitter.Append($".buffer[{rightCSource}]");
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
            if((!Array.IsPure && (!Index.IsConstant || !Value.IsConstant)) ||
               (!Index.IsPure && (!Array.IsConstant || !Value.IsConstant)) ||
               (!Value.IsPure && (!Index.IsConstant || !Array.IsConstant)))
                throw new CannotEnsureOrderOfEvaluation(this);

            StringBuilder destBuilder = new();
            IRValue.EmitMemorySafe(Array, irProgram, destBuilder, typeargs);
            if (irProgram.DoBoundsChecking)
            {
                destBuilder.Append(".buffer[_nhp_bounds_check(");
                IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs);
                destBuilder.Append(", ");
                IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, destBuilder, typeargs);
                destBuilder.Append(".length, ");
                CharacterLiteral.EmitCString(destBuilder, ErrorReportedElement.SourceLocation.ToString(), false, true);
                destBuilder.Append(", ");
                if (ErrorReportedElement is Syntax.IAstStatement statement)
                    CharacterLiteral.EmitCString(destBuilder, statement.ToString(0), false, true);
                else if (ErrorReportedElement is Syntax.IAstValue value)
                    CharacterLiteral.EmitCString(destBuilder, value.ToString(), false, true);
                destBuilder.Append(")]");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
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
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
        {
            if (IsInitializingProperty)
                emitter.Append($"({leftCSource}->{Property.Name} = {rightCSource})");
            else
            {
                Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"{leftCSource}->{Property.Name}", rightCSource);
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
        }
    }
}
