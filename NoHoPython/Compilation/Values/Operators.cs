using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ComparativeOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Left.ScopeForUsedTypes(typeargs, irBuilder);
            Right.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs);

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

            IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs);
            emitter.Append(')');
        }
    }

    partial class LogicalOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Left.ScopeForUsedTypes(typeargs, irBuilder);
            Right.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs);

            switch (Operation)
            {
                case LogicalOperation.And:
                    emitter.Append(" && ");
                    break;
                case LogicalOperation.Or:
                    emitter.Append(" || ");
                    break;
            }

            IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs);
            emitter.Append(')');
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs);
            if (irProgram.DoBoundsChecking)
            {
                emitter.Append(".buffer[_nhp_bounds_check(");
                IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
                emitter.Append(", ");
                IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, emitter, typeargs);
                emitter.Append(".length, ");
                CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString());
                emitter.Append(", ");
                if (ErrorReportedElement is Syntax.IAstStatement statement)
                    CharacterLiteral.EmitCString(emitter, statement.ToString(0));
                else if (ErrorReportedElement is Syntax.IAstValue value)
                    CharacterLiteral.EmitCString(emitter, value.ToString());
                emitter.Append(")]");
            }
            else
            {
                emitter.Append(".buffer[");
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder destBuilder = new();
            IRValue.EmitMemorySafe(Array, irProgram, destBuilder, typeargs);
            if (irProgram.DoBoundsChecking)
            {
                destBuilder.Append(".buffer[_nhp_bounds_check(");
                IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs);
                destBuilder.Append(", ");
                IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, destBuilder, typeargs);
                destBuilder.Append(".length, ");
                CharacterLiteral.EmitCString(destBuilder, ErrorReportedElement.SourceLocation.ToString());
                destBuilder.Append(", ");
                if (ErrorReportedElement is Syntax.IAstStatement statement)
                    CharacterLiteral.EmitCString(destBuilder, statement.ToString(0));
                else if (ErrorReportedElement is Syntax.IAstValue value)
                    CharacterLiteral.EmitCString(destBuilder, value.ToString());
                destBuilder.Append(")]");
            }
            else
            {
                destBuilder.Append(".buffer[");
                IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs);
                destBuilder.Append(']');
            }
                    
            StringBuilder valueBuilder = new();
            if (Value.RequiresDisposal(typeargs))
                Value.Emit(irProgram, valueBuilder, typeargs);
            else
            {
                StringBuilder toCopyBuilder = new();
                Value.Emit(irProgram, toCopyBuilder, typeargs);
                Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, valueBuilder, toCopyBuilder.ToString());
            }

            Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, destBuilder.ToString(), valueBuilder.ToString());
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs);
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder valueBuilder = new StringBuilder();
            IRValue.EmitMemorySafe(Record, irProgram, valueBuilder, typeargs);

            if (Record.Type.SubstituteWithTypearg(typeargs) is IPropertyContainer propertyContainer)
                propertyContainer.EmitGetProperty(emitter, valueBuilder.ToString(), Property);
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {

            StringBuilder recordBuilder = new();
            IRValue.EmitMemorySafe(Record, irProgram, recordBuilder, typeargs);

            if (IsInitializingProperty)
            {
                emitter.Append($"({recordBuilder}->{Property.Name} = ");
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, emitter, typeargs);
                else
                {
                    StringBuilder valueBuilder = new();
                    Value.Emit(irProgram, valueBuilder, typeargs);
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString());
                }
                emitter.Append(')');
            }
            else
            {
                StringBuilder toCopyBuilder = new();
                if (Value.RequiresDisposal(typeargs))
                    Value.Emit(irProgram, toCopyBuilder, typeargs);
                else
                {
                    StringBuilder valueBuilder = new();
                    Value.Emit(irProgram, valueBuilder, typeargs);
                    Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, toCopyBuilder, valueBuilder.ToString());
                }
                Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, $"{recordBuilder}->{Property.Name}", toCopyBuilder.ToString());
            }
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs);
            emitter.AppendLine(";");
        }
    }
}
