using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class VariableReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append(Variable.GetStandardIdentifier());
        }
    }

    partial class VariableDeclaration
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            InitialValue.ScopeForUsedTypes(typeargs);
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"({Variable.GetStandardIdentifier()} = ");
            if (InitialValue.RequiresDisposal(typeargs))
                InitialValue.Emit(emitter, typeargs);
            else
            {
                StringBuilder valueBuilder = new StringBuilder();
                InitialValue.Emit(valueBuilder, typeargs);
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(emitter, valueBuilder.ToString());
            }
            emitter.Append(')');
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(emitter, typeargs);
            emitter.AppendLine(";");
        }
    }

    partial class SetVariable
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs);
            SetValue.ScopeForUsedTypes(typeargs);
        }

        public void ForwardDeclareType(StringBuilder emitter) { }

        public void ForwardDeclare(StringBuilder emitter) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder valueBuilder = new StringBuilder();
            SetValue.Emit(valueBuilder, typeargs);

            if (SetValue.RequiresDisposal(typeargs))
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(emitter, Variable.GetStandardIdentifier(), valueBuilder.ToString());
            else
            {
                StringBuilder copyBuilder = new StringBuilder();
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(copyBuilder, valueBuilder.ToString());
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(emitter, Variable.GetStandardIdentifier(), copyBuilder.ToString());
            }
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(emitter, typeargs);
            emitter.AppendLine(";");
        }
    }
}