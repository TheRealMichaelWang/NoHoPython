using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IfElseValue
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Condition.ScopeForUsedTypes(typeargs);
            IfTrueValue.ScopeForUsedTypes(typeargs);
            IfFalseValue.ScopeForUsedTypes(typeargs);
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            emitter.Append('(');
            IRValue.EmitMemorySafe(Condition, emitter, typeargs);
            emitter.Append(") ? (");
            IRValue.EmitMemorySafe(IfTrueValue, emitter, typeargs);
            emitter.Append(") : (");
            IRValue.EmitMemorySafe(IfFalseValue, emitter, typeargs);
            emitter.Append(')');
            emitter.Append(')');
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
        public static void CIndent(StringBuilder emitter, int indent) => emitter.Append(new string('\t', indent));

        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ScopeForUsedTypes(typeargs));
        }

        public virtual void ForwardDeclareType(StringBuilder emitter)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ForwardDeclareType(emitter));
        }

        public virtual void ForwardDeclare(StringBuilder emitter)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ForwardDeclare(emitter));
        }

        public void EmitNoOpen(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();
            foreach(Variable variable in DeclaredVariables)
            {
                CIndent(emitter, indent + 1);
                emitter.AppendLine($"{variable.Type.SubstituteWithTypearg(typeargs).GetCName()} {variable.GetStandardIdentifier()};");
            }

            Statements.ForEach((statement) => statement.Emit(emitter, typeargs, indent + 1));

            foreach (Variable variable in DeclaredVariables)
            {
                if (variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    CIndent(emitter, indent + 1);
                    variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(emitter, variable.GetStandardIdentifier());
                }
            }

            CIndent(emitter, indent);
            emitter.AppendLine("}");
        }

        public virtual void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            emitter.AppendLine(" {");
            EmitNoOpen(emitter, typeargs, indent);
        }
    }

    partial class IfElseBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            IfTrueBlock.ScopeForUsedTypes(typeargs);
            IfFalseBlock.ScopeForUsedTypes(typeargs);
        }

        public void ForwardDeclareType(StringBuilder emitter)
        {
            IfTrueBlock.ForwardDeclareType(emitter);
            IfFalseBlock.ForwardDeclareType(emitter);
        }

        public void ForwardDeclare(StringBuilder emitter)
        {
            IfTrueBlock.ForwardDeclare(emitter);
            IfFalseBlock.ForwardDeclare(emitter);
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("if(");
            Condition.Emit(emitter, typeargs);
            emitter.Append(')');
            IfTrueBlock.Emit(emitter, typeargs, indent);

            CodeBlock.CIndent(emitter, indent);
            emitter.Append("else");
            IfFalseBlock.Emit(emitter, typeargs, indent);
        }
    }

    partial class IfBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) => IfTrueBlock.ScopeForUsedTypes(typeargs);

        public void ForwardDeclareType(StringBuilder emitter) => IfTrueBlock.ForwardDeclareType(emitter);

        public void ForwardDeclare(StringBuilder emitter) => IfTrueBlock.ForwardDeclare(emitter);

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("if(");
            Condition.Emit(emitter, typeargs);
            emitter.Append(')');
            IfTrueBlock.Emit(emitter, typeargs, indent);
        }
    }

    partial class WhileBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) => WhileTrueBlock.ScopeForUsedTypes(typeargs);

        public void ForwardDeclareType(StringBuilder emitter) => WhileTrueBlock.ForwardDeclareType(emitter);

        public void ForwardDeclare(StringBuilder emitter) => WhileTrueBlock.ForwardDeclare(emitter);

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("while(");
            Condition.Emit(emitter, typeargs);
            emitter.Append(')');
            WhileTrueBlock.Emit(emitter, typeargs, indent);
        }
    }
}
