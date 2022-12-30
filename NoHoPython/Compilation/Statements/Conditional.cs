using NoHoPython.Compilation;
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

            if (Condition.IsTruey)
                IfTrueValue.ScopeForUsedTypes(typeargs, irBuilder);
            else if (Condition.IsFalsey)
                IfFalseValue.ScopeForUsedTypes(typeargs, irBuilder);
            else
            {
                Condition.ScopeForUsedTypes(typeargs, irBuilder);
                IfTrueValue.ScopeForUsedTypes(typeargs, irBuilder);
                IfFalseValue.ScopeForUsedTypes(typeargs, irBuilder);
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if (Condition.IsTruey)
                IRValue.EmitMemorySafe(IfTrueValue, irProgram, emitter, typeargs);
            else if (Condition.IsFalsey)
                IRValue.EmitMemorySafe(IfFalseValue, irProgram, emitter, typeargs);
            else
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

        public void EmitInitialize(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            foreach (VariableDeclaration declaration in DeclaredVariables)
                declaration.EmitCDecl(irProgram, emitter, typeargs, indent);
        }

        public void EmitNoOpen(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent, bool insertFinalBreak)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.Emit(irProgram, emitter, typeargs, indent + 1));

            if (!CodeBlockAllCodePathsReturn())
            {
                foreach (VariableDeclaration declaration in DeclaredVariables)
                    declaration.EmitCFree(irProgram, emitter, typeargs, indent);
                
                if (insertFinalBreak)
                {
                    if (BreakLabelId != null)
                        throw new InvalidOperationException();

                    CIndent(emitter, indent + 1);
                    emitter.AppendLine("break;");
                }
            }
            CIndent(emitter, indent);
            emitter.AppendLine("}");

            if (BreakLabelId != null)
            {
                CIndent(emitter, indent);
                emitter.AppendLine($"loopbreaklabel{BreakLabelId.Value}:");
            }
        }

        public virtual void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            emitter.AppendLine(" {");
            EmitInitialize(irProgram, emitter, typeargs, indent);
            EmitNoOpen(irProgram, emitter, typeargs, indent, false);
        }
    }

    partial class IfElseBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (Condition.IsTruey && Condition.IsPure)
                IfTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
            else if (Condition.IsFalsey && Condition.IsPure)
                IfFalseBlock.ScopeForUsedTypes(typeargs, irBuilder);
            else
            {
                Condition.ScopeForUsedTypes(typeargs, irBuilder);
                IfTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
                IfFalseBlock.ScopeForUsedTypes(typeargs, irBuilder);
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Condition.IsTruey && Condition.IsPure)
            {
                CodeBlock.CIndent(emitter, indent);
                IfTrueBlock.Emit(irProgram, emitter, typeargs, indent);
            }
            else if (Condition.IsFalsey && Condition.IsPure)
            {
                CodeBlock.CIndent(emitter, indent);
                IfFalseBlock.Emit(irProgram, emitter, typeargs, indent);
            }
            else
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
    }

    partial class IfBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (Condition.IsFalsey && Condition.IsPure)
                return;
            if (Condition.IsTruey && Condition.IsPure)
                IfTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
            else
            {
                Condition.ScopeForUsedTypes(typeargs, irBuilder);
                IfTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Condition.IsFalsey && Condition.IsPure)
                return;

            if (Condition.IsTruey && Condition.IsPure)
            {
                CodeBlock.CIndent(emitter, indent);
                IfTrueBlock.Emit(irProgram, emitter, typeargs, indent);
            }
            else
            {
                CodeBlock.CIndent(emitter, indent);
                emitter.Append("if(");
                IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
                emitter.Append(')');
                IfTrueBlock.Emit(irProgram, emitter, typeargs, indent);
            }
        }
    }

    partial class WhileBlock
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (Condition.IsFalsey && Condition.IsPure)
                return;

            if (Condition.IsTruey && Condition.IsPure)
                WhileTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
            else
            {
                Condition.ScopeForUsedTypes(typeargs, irBuilder);
                WhileTrueBlock.ScopeForUsedTypes(typeargs, irBuilder);
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Condition.IsFalsey && Condition.IsPure)
                return;

            CodeBlock.CIndent(emitter, indent);
            if (Condition.IsTruey && Condition.IsPure)
                emitter.Append("for(;;)");
            else
            {
                emitter.Append("while(");
                IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
                emitter.Append(')');
            }
            WhileTrueBlock.Emit(irProgram, emitter, typeargs, indent);
        }
    }

    partial class IterationForLoop
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            IteratorVariableDeclaration.ScopeForUsedTypes(typeargs, irBuilder);
            UpperBound.ScopeForUsedTypes(typeargs, irBuilder);
            IterationBlock.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.AppendLine("{");
            IterationBlock.EmitInitialize(irProgram, emitter, typeargs, indent);
            IteratorVariableDeclaration.Emit(irProgram, emitter, typeargs, indent + 1);

            CodeBlock.CIndent(emitter, indent + 1);
            emitter.Append($"long _nhp_upper_{IteratorVariableDeclaration.Variable.Name} = ");
            UpperBound.Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
            
            CodeBlock.CIndent(emitter, indent + 1);
            emitter.AppendLine($"while((++{IteratorVariableDeclaration.Variable.GetStandardIdentifier()}) <= _nhp_upper_{IteratorVariableDeclaration.Variable.Name}) {{");
            IterationBlock.EmitNoOpen(irProgram, emitter, typeargs, indent + 1, false);
            CodeBlock.CIndent(emitter, indent);
            emitter.AppendLine("}");
        }
    }

    partial class MatchStatement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            MatchValue.ScopeForUsedTypes(typeargs, irBuilder);
            MatchValue.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            foreach (MatchHandler handler in MatchHandlers)
                handler.ToExecute.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.Append("switch(");
            IRValue.EmitMemorySafe(MatchValue, irProgram, emitter, typeargs);
            emitter.AppendLine(".option) {");

            EnumType enumType = (EnumType)MatchValue.Type.SubstituteWithTypearg(typeargs);
            foreach(MatchHandler handler in MatchHandlers)
            {
                IType currentOption = handler.MatchedType.SubstituteWithTypearg(typeargs);

                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine($"case {enumType.GetCEnumOptionForType(irProgram, currentOption)}: {{");
                
                if (handler.MatchedVariable != null)
                {
                    CodeBlock.CIndent(emitter, indent + 1);
                    emitter.Append($"{currentOption.GetCName(irProgram)} {handler.MatchedVariable.GetStandardIdentifier()} = ");
                    IRValue.EmitMemorySafe(MatchValue.GetPostEvalPure(), irProgram, emitter, typeargs);
                    emitter.AppendLine($".data.{currentOption.GetStandardIdentifier(irProgram)}_set;");
                }

                handler.ToExecute.EmitInitialize(irProgram, emitter, typeargs, indent);
                handler.ToExecute.EmitNoOpen(irProgram, emitter, typeargs, indent, true);
            }
            if(DefaultHandler != null)
            {
                CodeBlock.CIndent(emitter, indent);
                emitter.AppendLine("default: {");
                DefaultHandler.EmitInitialize(irProgram, emitter, typeargs, indent);
                DefaultHandler.EmitNoOpen(irProgram, emitter, typeargs, indent, false);
            }

            CodeBlock.CIndent(emitter, indent);
            emitter.AppendLine("}");
        }
    }

    partial class LoopStatement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            foreach (Variable variable in activeLoopVariables)
                if (variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    CodeBlock.CIndent(emitter, indent);
                    variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, variable.GetStandardIdentifier(), "NULL");
                }

            CodeBlock.CIndent(emitter, indent);
            if (Action.Type == Syntax.Parsing.TokenType.Break)
                emitter.AppendLine($"goto loopbreaklabel{breakLabelId};");
            else
                emitter.AppendLine("continue;");
        }
    }

    partial class AssertStatement
    {
        public static void EmitAsserter(StringBuilder emitter, bool doCallStack)
        {
            emitter.AppendLine("void _nhp_assert(int flag, const char* src_loc, const char* assertion_src) {");
            emitter.AppendLine("\tif(!flag) {");

            if (doCallStack)
            {
                CallStackReporting.EmitErrorLoc(emitter, "src_loc", "assertion_src", 2);
                CallStackReporting.EmitPrintStackTrace(emitter, 2);
                emitter.AppendLine("\t\tprintf(\"AssertionError: %s failed.\\n\", assertion_src);");
            }
            else
            {
                emitter.AppendLine("\t\tprintf(\"Assertion Failed, %s.\\n\\t\", src_loc);");
                emitter.AppendLine("\t\tputs(assertion_src);");
            }

            emitter.AppendLine("\t\tabort();");
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => Condition.ScopeForUsedTypes(typeargs, irBuilder);

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (irProgram.EliminateAsserts)
                return;

            CodeBlock.CIndent(emitter, indent);
            emitter.Append("_nhp_assert(");
            IRValue.EmitMemorySafe(Condition, irProgram, emitter, typeargs);
            emitter.Append(", ");
            CharacterLiteral.EmitCString(emitter, ErrorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            if (ErrorReportedElement is Syntax.IAstStatement statement)
                CharacterLiteral.EmitCString(emitter, statement.ToString(0), false, true);
            else if (ErrorReportedElement is Syntax.IAstValue value)
                CharacterLiteral.EmitCString(emitter, value.ToString(), false, true);
            emitter.AppendLine(");");
        }
    }
}