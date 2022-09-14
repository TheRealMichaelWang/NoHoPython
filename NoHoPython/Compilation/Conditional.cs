using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IfElseValue
    {
        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            Condition.Emit(emitter, typeargs);
            emitter.Append(") ? (");
            IfTrueValue.Emit(emitter, typeargs);
            emitter.Append(") : (");
            IfFalseValue.Emit(emitter, typeargs);
            emitter.Append(')');
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
        public static void CIndent(StringBuilder emitter, int indent) => emitter.Append(new string('\t', indent));

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ScopeForUsedTypes(typeargs));
        }

        public void ForwardDeclareType(StringBuilder emitter)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ForwardDeclareType(emitter));
        }

        public void ForwardDeclare(StringBuilder emitter)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ForwardDeclare(emitter));
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            emitter.AppendLine(" {");
            Statements.ForEach((statement) => {
                CIndent(emitter, indent + 1);
                statement.Emit(emitter, typeargs, indent + 1);
            });
            CIndent(emitter, indent);
            emitter.AppendLine("}");
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
