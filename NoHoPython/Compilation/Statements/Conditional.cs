using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IfElseValue
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Condition.ScopeForUsedTypes(typeargs, irBuilder);
            IfTrueValue.ScopeForUsedTypes(typeargs, irBuilder);
            IfFalseValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('(');
            emitter.Append('(');
            IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
            emitter.Append(") ? (");
            IRValue.EmitMemorySafe(IfTrueValue, irProgram, emitter, typeargs);
            emitter.Append(") : (");
            IRValue.EmitMemorySafe(IfFalseValue, irProgram, emitter, typeargs);
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

        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public virtual void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ForwardDeclareType(irProgram, emitter));
        }

        public virtual void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ForwardDeclare(irProgram, emitter));
        }

        public void EmitNoOpen(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();
            foreach(Variable variable in DeclaredVariables)
            {
                CIndent(emitter, indent + 1);
                emitter.AppendLine($"{variable.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} {variable.GetStandardIdentifier(irProgram)};");
            }

            Statements.ForEach((statement) => statement.Emit(irProgram, emitter, typeargs, indent + 1));

            foreach (Variable variable in DeclaredVariables)
            {
                if (variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    CIndent(emitter, indent + 1);
                    variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, variable.GetStandardIdentifier(irProgram));
                }
            }

            CIndent(emitter, indent);
            emitter.AppendLine("}");
        }

        public virtual void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            emitter.AppendLine(" {");
            EmitNoOpen(irProgram, emitter, typeargs, indent);
        }
    }

    partial class IfElseBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            IfTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
            IfFalseBlock.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter)
        {
            IfTrueBlock.ForwardDeclareType(irProgram, emitter);
            IfFalseBlock.ForwardDeclareType(irProgram, emitter);
        }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
        {
            IfTrueBlock.ForwardDeclare(irProgram, emitter);
            IfFalseBlock.ForwardDeclare(irProgram, emitter);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("if(");
            IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
            emitter.Append(')');
            IfTrueBlock.Emit(irProgram, emitter, typeargs, indent);

            CodeBlock.CIndent(emitter, indent);
            emitter.Append("else");
            IfFalseBlock.Emit(irProgram, emitter, typeargs, indent);
        }
    }

    partial class IfBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => IfTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) => IfTrueBlock.ForwardDeclareType(irProgram, emitter);

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) => IfTrueBlock.ForwardDeclare(irProgram, emitter);

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("if(");
            IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
            emitter.Append(')');
            IfTrueBlock.Emit(irProgram, emitter, typeargs, indent);
        }
    }

    partial class WhileBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => WhileTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) => WhileTrueBlock.ForwardDeclareType(irProgram, emitter);

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) => WhileTrueBlock.ForwardDeclare(irProgram, emitter);

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("while(");
            IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
            emitter.Append(')');
            WhileTrueBlock.Emit(irProgram, emitter, typeargs, indent);
        }
    }

    partial class AssertStatement
    {
        public static void EmitAsserter(StringBuilder emitter)
        {
            emitter.AppendLine("void _nhp_assert(int flag, const char* src_loc, const char* assertion_src) {");
            emitter.AppendLine("\tif(!flag) {");
            emitter.AppendLine("\t\tprintf(\"Assertion Failed, %s.\\n\\t\", src_loc);");
            emitter.AppendLine("\t\tputs(assertion_src);");
            emitter.AppendLine("\t\tabort();");
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => Condition.ScopeForUsedTypes(typeargs, irBuilder);

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter) { }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("_nhp_assert(");
            IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
            emitter.Append(", ");
            CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString());
            emitter.Append(", ");
            if (ErrorReportedElement is Syntax.IAstStatement statement)
                CharacterLiteral.EmitCString(emitter, statement.ToString(0));
            else if (ErrorReportedElement is Syntax.IAstValue value)
                CharacterLiteral.EmitCString(emitter, value.ToString());
            emitter.AppendLine(");");
        }
    }
}