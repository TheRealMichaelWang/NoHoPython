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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            emitter.Append('(');
            IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs, "NULL");

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

            IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs, "NULL");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            emitter.Append('(');
            IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs, "NULL");

            switch (Operation)
            {
                case LogicalOperation.And:
                    emitter.Append(" && ");
                    break;
                case LogicalOperation.Or:
                    emitter.Append(" || ");
                    break;
            }

            IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs, "NULL");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            IRValue.EmitMemorySafe(Array, irProgram, emitter, typeargs, "NULL");
            if (irProgram.DoBoundsChecking)
            {
                emitter.Append(".buffer[_nhp_bounds_check(");
                IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs, "NULL");
                emitter.Append(", ");
                IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, emitter, typeargs, "NULL");
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
                IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs, "NULL");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            StringBuilder destBuilder = new();
            IRValue.EmitMemorySafe(Array, irProgram, destBuilder, typeargs, "NULL");
            if (irProgram.DoBoundsChecking)
            {
                destBuilder.Append(".buffer[_nhp_bounds_check(");
                IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs, "NULL");
                destBuilder.Append(", ");
                IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, destBuilder, typeargs, "NULL");
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
                IRValue.EmitMemorySafe(Index, irProgram, destBuilder, typeargs, "NULL");
                destBuilder.Append(']');
            }

            StringBuilder arrayResponsibleDestructor = new();
            IRValue.EmitMemorySafe(Array.GetPostEvalPure(), irProgram, arrayResponsibleDestructor, typeargs, "NULL");
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

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

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
            IRValue.EmitMemorySafe(Record, irProgram, valueBuilder, typeargs, "NULL");

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
            StringBuilder recordBuilder = new();
            IRValue.EmitMemorySafe(Record, irProgram, recordBuilder, typeargs, "NULL");

            StringBuilder recordResponsibleDestroyer = new();
            IRValue.EmitMemorySafe(Record.GetPostEvalPure(), irProgram, recordResponsibleDestroyer, typeargs, "NULL");
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

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
        }
    }
}
