using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ComparativeOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Left.ScopeForUsedTypes(typeargs);
            Right.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            IRValue.EmitMemorySafe(Left, emitter, typeargs);

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

            IRValue.EmitMemorySafe(Right, emitter, typeargs);
            emitter.Append(')');
        }
    }

    partial class LogicalOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Left.ScopeForUsedTypes(typeargs);
            Right.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            IRValue.EmitMemorySafe(Left, emitter, typeargs);

            switch (Operation)
            {
                case LogicalOperation.And:
                    emitter.Append(" && ");
                    break;
                case LogicalOperation.Or:
                    emitter.Append(" || ");
                    break;
            }

            IRValue.EmitMemorySafe(Right, emitter, typeargs);
            emitter.Append(')');
        }
    }

    partial class GetValueAtIndex
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Array.ScopeForUsedTypes(typeargs);
            Index.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            IRValue.EmitMemorySafe(Array, emitter, typeargs);
            emitter.Append(".buffer[_nhp_bounds_check(");
            IRValue.EmitMemorySafe(Index, emitter, typeargs);
            emitter.Append(", ");
            IRValue.EmitMemorySafe(Array, emitter, typeargs);
            emitter.Append(".length");
            emitter.Append(")]");
        }
    }

    partial class SetValueAtIndex
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Array.ScopeForUsedTypes(typeargs);
            Index.ScopeForUsedTypes(typeargs);
            Value.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder destBuilder = new();
            IRValue.EmitMemorySafe(Array, destBuilder, typeargs);
            destBuilder.Append(".buffer[_nhp_bounds_check(");
            IRValue.EmitMemorySafe(Index, destBuilder, typeargs);
            destBuilder.Append(", ");
            IRValue.EmitMemorySafe(Array, destBuilder, typeargs);
            destBuilder.Append(".length");
            destBuilder.Append(")]");

            StringBuilder valueBuilder = new();
            if (Value.RequiresDisposal(typeargs))
                Value.Emit(valueBuilder, typeargs);
            else
            {
                StringBuilder toCopyBuilder = new();
                Value.Emit(toCopyBuilder, typeargs);
                Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(valueBuilder, toCopyBuilder.ToString());
            }

            Value.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(emitter, destBuilder.ToString(), valueBuilder.ToString());
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(emitter, typeargs);
            emitter.AppendLine(";");
        }
    }

    partial class GetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Record.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder valueBuilder = new StringBuilder();
            IRValue.EmitMemorySafe(Record, valueBuilder, typeargs);
            IPropertyContainer propertyContainer = (IPropertyContainer)Record.Type;
            propertyContainer.EmitGetProperty(emitter, valueBuilder.ToString(), Property);
        }
    }

    partial class SetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Record.ScopeForUsedTypes(typeargs);
            Value.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder toCopyBuilder = new();
            if (Value.RequiresDisposal(typeargs))
                Value.Emit(toCopyBuilder, typeargs);
            else
            {
                StringBuilder valueBuilder = new();
                Value.Emit(valueBuilder, typeargs);
                Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(toCopyBuilder, valueBuilder.ToString());
            }

            StringBuilder recordBuilder = new();
            IRValue.EmitMemorySafe(Record, recordBuilder, typeargs);

            Property.Type.SubstituteWithTypearg(typeargs).EmitMoveValue(emitter, $"{recordBuilder}->{Property.Name}", toCopyBuilder.ToString());
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(emitter, typeargs);
            emitter.AppendLine(";");
        }
    }
}
