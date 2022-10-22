using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class VariableReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Variable.GetStandardIdentifier(irProgram));
    }

    partial class VariableDeclaration
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            InitialValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        private void EmitStandard(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"({Variable.GetStandardIdentifier(irProgram)} = ");
            if (InitialValue.RequiresDisposal(typeargs))
                InitialValue.Emit(irProgram, emitter, typeargs);
            else
            {
                StringBuilder valueBuilder = new StringBuilder();
                InitialValue.Emit(irProgram, valueBuilder, typeargs);
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString());
            }
            emitter.Append(')');
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                throw new CannotEmitDestructorError(this);
            EmitStandard(irProgram, emitter, typeargs);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            EmitStandard(irProgram, emitter, typeargs);
            emitter.AppendLine(";");
        }
    }

    partial class SetVariable
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs);
            SetValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder valueBuilder = new StringBuilder();
            SetValue.Emit(irProgram, valueBuilder, typeargs);

            if (SetValue.RequiresDisposal(typeargs))
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Variable.GetStandardIdentifier(irProgram), valueBuilder.ToString());
            else
            {
                StringBuilder copyBuilder = new StringBuilder();
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, copyBuilder, valueBuilder.ToString());
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Variable.GetStandardIdentifier(irProgram), copyBuilder.ToString());
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs);
            emitter.AppendLine(";");
        }
    }

    partial class CSymbolReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append(CSymbol.Name);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CSymbolDeclaration
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => CSymbol.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent) { }
    }
}