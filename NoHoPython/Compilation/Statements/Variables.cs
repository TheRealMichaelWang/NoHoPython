using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Scoping
{
    partial class Variable
    {
        public void EmitCFree(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                CodeBlock.CIndent(emitter, indent + 1);
                Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, GetStandardIdentifier(), "NULL");
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class VariableReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append(Variable.GetStandardIdentifier());
    }

    partial class VariableDeclaration
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            InitialValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void EmitCDecl(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent + 1);
            emitter.AppendLine($"{Variable.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} {Variable.GetStandardIdentifier()};");
            if (WillRevaluate && Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                CodeBlock.CIndent(emitter, indent + 1);
                emitter.AppendLine($"int init_{Variable.GetStandardIdentifier()} = 0;");
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            bool closeExpressionStatement = false;
            if (WillRevaluate && Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEmitDestructorError(this);
                emitter.Append($"({{if(init_{Variable.GetStandardIdentifier()}) {{");
                Variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, Variable.GetStandardIdentifier(), "NULL");
                emitter.Append($"}} else {{init_{Variable.GetStandardIdentifier()} = 1;}}");
                closeExpressionStatement = true;
            }

            emitter.Append($"({Variable.GetStandardIdentifier()} = ");
            if (InitialValue.RequiresDisposal(typeargs))
                InitialValue.Emit(irProgram, emitter, typeargs, "NULL");
            else
            {
                StringBuilder valueBuilder = new();
                InitialValue.Emit(irProgram, valueBuilder, typeargs, "NULL");
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), "NULL");
            }
            emitter.Append(')');

            if (closeExpressionStatement)
                emitter.Append(";})");
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            StringBuilder valueBuilder = new();
            SetValue.Emit(irProgram, valueBuilder, typeargs, "NULL");

            if (SetValue.RequiresDisposal(typeargs))
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Variable.GetStandardIdentifier(), valueBuilder.ToString());
            else
            {
                StringBuilder copyBuilder = new();
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, copyBuilder, valueBuilder.ToString(), "NULL");
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Variable.GetStandardIdentifier(), copyBuilder.ToString());
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
        }
    }

    partial class CSymbolReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent) { }
    }
}