using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.Scoping
{
    partial class Variable
    {
        public void EmitCFree(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                CodeBlock.CIndent(emitter, indent + 1);
                Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, GetStandardIdentifier(), "NULL");
                emitter.AppendLine();
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class VariableReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval) => emitter.Append(Variable.GetStandardIdentifier());
    }

    partial class VariableDeclaration
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            InitialValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void EmitCDecl(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent + 1);
            emitter.AppendLine($"{Variable.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} {Variable.GetStandardIdentifier()};");
            if (WillRevaluate && Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                CodeBlock.CIndent(emitter, indent + 1);
                emitter.LastSourceLocation = ErrorReportedElement.SourceLocation;
                emitter.AppendLine($"int init_{Variable.GetStandardIdentifier()} = 0;");
            }
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
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
            if (InitialValue.RequiresDisposal(typeargs, false))
                InitialValue.Emit(irProgram, emitter, typeargs, "NULL", false);
            else
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(InitialValue, irProgram, typeargs, "NULL"), "NULL");
            emitter.Append(')');

            if (closeExpressionStatement)
                emitter.Append(";})");
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL", false);
            emitter.AppendLine(";");
        }
    }

    partial class SetVariable
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs);
            SetValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            string setValueCSource = BufferedEmitter.EmitBufferedValue(SetValue, irProgram, typeargs, "NULL");

            if (SetValue.RequiresDisposal(typeargs, false))
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Variable.GetStandardIdentifier(), setValueCSource, "NULL");
            else
            {
                BufferedEmitter copyBuilder = new();
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, copyBuilder, setValueCSource, "NULL");
                Type.SubstituteWithTypearg(typeargs).EmitMoveValue(irProgram, emitter, Variable.GetStandardIdentifier(), copyBuilder.ToString(), "NULL");
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL", false);
            emitter.AppendLine(";");
        }
    }

    partial class CSymbolReference
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
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

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent) { }
    }
}