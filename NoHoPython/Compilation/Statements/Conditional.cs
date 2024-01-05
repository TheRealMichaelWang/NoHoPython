using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IfElseValue
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => IfTrueValue.RequiresDisposal(irProgram, typeargs, isTemporaryEval) || IfFalseValue.RequiresDisposal(irProgram, typeargs, isTemporaryEval);

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

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Condition.MustUseDestinationPromise(irProgram, typeargs, true) || IfTrueValue.MustUseDestinationPromise(irProgram, typeargs, false) || IfFalseValue.MustUseDestinationPromise(irProgram, typeargs, false);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            void EmitCorrectCopy(IRValue irValue, Emitter emitter)
            {
                if (RequiresDisposal(irProgram, typeargs, isTemporaryEval) && !irValue.RequiresDisposal(irProgram, typeargs, false))
                    irValue.Emit(irProgram, emitter, typeargs, (valPromise) => irValue.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valPromise, responsibleDestroyer, this), responsibleDestroyer, isTemporaryEval);
                else
                    IRValue.EmitDirect(irProgram, emitter, irValue, typeargs, responsibleDestroyer, isTemporaryEval);
            }

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();

                if ((Condition.IsTruey || Condition.IsFalsey) && !Condition.IsPure)
                    IRValue.EmitAsStatement(irProgram, primaryEmitter, Condition, typeargs);

                if (Condition.IsTruey)
                    EmitCorrectCopy(IfTrueValue, primaryEmitter);
                else if (Condition.IsFalsey)
                    EmitCorrectCopy(IfFalseValue, primaryEmitter);
                else
                {
                    if (Condition.MustUseDestinationPromise(irProgram, typeargs, true))
                    {
                        primaryEmitter.AppendLine($"int cond{indirection};");
                        primaryEmitter.SetArgument(Condition, $"cond{indirection}", irProgram, typeargs, true);
                        primaryEmitter.AppendStartBlock($"if(cond{indirection})");
                    }
                    else
                    {
                        primaryEmitter.Append("if(");
                        IRValue.EmitDirect(irProgram, primaryEmitter, Condition, typeargs, Emitter.NullPromise, true);
                        primaryEmitter.AppendStartBlock(")");
                    }
                    EmitCorrectCopy(IfTrueValue, primaryEmitter);
                    primaryEmitter.AppendEndBlock();
                    primaryEmitter.AppendStartBlock("else");
                    EmitCorrectCopy(IfFalseValue, primaryEmitter);
                    primaryEmitter.AppendEndBlock();
                }
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append("((");
                    IRValue.EmitDirect(irProgram, emitter, Condition, typeargs, Emitter.NullPromise, true);
                    emitter.Append(") ? (");
                    EmitCorrectCopy(IfTrueValue, emitter);
                    emitter.Append(") : (");
                    EmitCorrectCopy(IfFalseValue, emitter);
                    emitter.Append("))");
                });
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => statement.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public void EmitInitialize(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            foreach (VariableDeclaration declaration in DeclaredVariables)
                declaration.EmitCDecl(irProgram, emitter, typeargs);
        }

        public void EmitNoOpen(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, bool insertFinalBreak)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            Statements.ForEach((statement) => {
                emitter.LastSourceLocation = statement.ErrorReportedElement.SourceLocation;
                statement.Emit(irProgram, emitter, typeargs);
            });

            emitter.LastSourceLocation = BlockBeginLocation;
            if (!CodeBlockAllCodePathsReturn())
            {
                emitter.DestroyBlockResources();
                if (insertFinalBreak)
                {
                    if (BreakLabelId != null)
                        throw new InvalidOperationException();

                    emitter.AppendLine("break;");
                }
            }
            emitter.AppendEndBlock();

            if (BreakLabelId != null)
                emitter.AppendLine($"loopbreaklabel{BreakLabelId.Value}:");
        }

        public virtual void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (Statements == null)
                throw new InvalidOperationException();

            primaryEmitter.AppendStartBlock();
            EmitInitialize(irProgram, primaryEmitter, typeargs);
            EmitNoOpen(irProgram, primaryEmitter, typeargs, false);
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if ((Condition.IsTruey || Condition.IsFalsey) && !Condition.IsPure)
                IRValue.EmitAsStatement(irProgram, primaryEmitter, Condition, typeargs);

            if (Condition.IsTruey)
                IfTrueBlock.Emit(irProgram, primaryEmitter, typeargs);
            else if (Condition.IsFalsey)
                IfFalseBlock.Emit(irProgram, primaryEmitter, typeargs);
            else if(Condition.MustUseDestinationPromise(irProgram, typeargs, true))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"int cond{indirection} = ");
                primaryEmitter.SetArgument(Condition, $"cond{indirection}", irProgram, typeargs, true);
                primaryEmitter.Append($"if(cond{indirection})");
                IfTrueBlock.Emit(irProgram, primaryEmitter, typeargs);
                primaryEmitter.Append("else");
                IfFalseBlock.Emit(irProgram, primaryEmitter, typeargs);
                primaryEmitter.AppendEndBlock();
            }
            else
            {
                primaryEmitter.Append("if(");
                IRValue.EmitDirect(irProgram, primaryEmitter, Condition, typeargs, Emitter.NullPromise, true);
                primaryEmitter.Append(')');
                IfTrueBlock.Emit(irProgram, primaryEmitter, typeargs);
                primaryEmitter.Append("else");
                IfFalseBlock.Emit(irProgram, primaryEmitter, typeargs);
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if ((Condition.IsTruey || Condition.IsFalsey) && !Condition.IsPure)
                IRValue.EmitAsStatement(irProgram, primaryEmitter, Condition, typeargs);

            if (Condition.IsTruey)
                IfTrueBlock.Emit(irProgram, primaryEmitter, typeargs);
            else if (Condition.IsFalsey)
                return;
            else if (Condition.MustUseDestinationPromise(irProgram, typeargs, true))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"int cond{indirection} = ");
                primaryEmitter.SetArgument(Condition, $"cond{indirection}", irProgram, typeargs, true);
                primaryEmitter.Append($"if(cond{indirection})");
                IfTrueBlock.Emit(irProgram, primaryEmitter, typeargs);
                primaryEmitter.AppendEndBlock();
            }
            else
            {
                primaryEmitter.Append("if(");
                IRValue.EmitDirect(irProgram, primaryEmitter, Condition, typeargs, Emitter.NullPromise, true);
                primaryEmitter.Append(')');
                IfTrueBlock.Emit(irProgram, primaryEmitter, typeargs);
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if(Condition.IsFalsey)
            {
                if(!Condition.IsPure)
                    IRValue.EmitAsStatement(irProgram, primaryEmitter, Condition, typeargs);
                
                return;
            }

            int indirection;
            primaryEmitter.DeclareLoopBlock();
            if (Condition.IsTruey || Condition.MustUseDestinationPromise(irProgram, typeargs, true))
                indirection = primaryEmitter.AppendStartBlock("for(;;)");
            else
            {
                primaryEmitter.Append("while(");
                IRValue.EmitDirect(irProgram, primaryEmitter, Condition, typeargs, Emitter.NullPromise, true);
                indirection = primaryEmitter.AppendStartBlock(")");
            }

            if (Condition.IsTruey && !Condition.IsPure)
                IRValue.EmitAsStatement(irProgram, primaryEmitter, Condition, typeargs);
            else if (Condition.MustUseDestinationPromise(irProgram, typeargs, true))
            {
                primaryEmitter.AppendLine($"int cond{indirection};");
                primaryEmitter.SetArgument(Condition, $"cond{indirection}", irProgram, typeargs, true);

                if (!Condition.IsTruey)
                    primaryEmitter.AppendLine($"if(!cond{indirection}) {{ break; }}");
            }

            WhileTrueBlock.EmitInitialize(irProgram, primaryEmitter, typeargs);
            WhileTrueBlock.EmitNoOpen(irProgram, primaryEmitter, typeargs, false);
            primaryEmitter.EndLoopBlock();
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            int indirection = primaryEmitter.AppendStartBlock();
            primaryEmitter.DeclareLoopBlock();
            IterationBlock.EmitInitialize(irProgram, primaryEmitter, typeargs);
            IteratorVariableDeclaration.Emit(irProgram, primaryEmitter, typeargs);
            primaryEmitter.AppendLine($"{UpperBound.Type.GetCName(irProgram)} upper_{indirection};");
            primaryEmitter.SetArgument(UpperBound, $"upper_{indirection}", irProgram, typeargs, true);
            primaryEmitter.AppendStartBlock($"while((++{IteratorVariableDeclaration.Variable.GetStandardIdentifier()}) <= upper_{indirection})");
            IterationBlock.EmitNoOpen(irProgram, primaryEmitter, typeargs, false);
            primaryEmitter.AppendEndBlock();
            primaryEmitter.EndLoopBlock();
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
            if (DefaultHandler != null)
                DefaultHandler.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            EnumType enumType = (EnumType)MatchValue.Type.SubstituteWithTypearg(typeargs);

            bool bufferedMatchVal = MatchValue.MustUseDestinationPromise(irProgram, typeargs, true) || MatchValue.RequiresDisposal(irProgram, typeargs, true) || !MatchValue.IsPure;
            int indirection = -1;
            if (bufferedMatchVal)
            {
                indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{enumType.GetCName(irProgram)} matchVal{indirection};");
                primaryEmitter.SetArgument(MatchValue, $"matchVal{indirection}", irProgram, typeargs, true);
                primaryEmitter.AppendLine($"switch(matchVal{indirection}.option) {{");
            }
            else
            {
                primaryEmitter.Append("switch(");
                IRValue.EmitDirect(irProgram, primaryEmitter, MatchValue, typeargs, Emitter.NullPromise, true);
                primaryEmitter.AppendLine(".option) {");
            }

            foreach (MatchHandler handler in MatchHandlers)
            {
                List<IType> currentOptions = handler.MatchTypes.ConvertAll((type) => type.SubstituteWithTypearg(typeargs));

                foreach (IType option in currentOptions)
                    primaryEmitter.AppendStartBlock($"case {enumType.GetCEnumOptionForType(irProgram, option)}:");

                if (handler.MatchedVariable != null)
                {
                    primaryEmitter.Append($"{currentOptions[0].GetCName(irProgram)} {handler.MatchedVariable.GetStandardIdentifier()} = ");
                    currentOptions[0].EmitCopyValue(irProgram, primaryEmitter, (emitter) =>
                    {
                        if (bufferedMatchVal)
                            emitter.Append($"matchVal{indirection}");
                        else
                            IRValue.EmitDirect(irProgram, emitter, MatchValue, typeargs, Emitter.NullPromise, true);
                        emitter.Append($".data.{currentOptions[0].GetStandardIdentifier(irProgram)}_set");
                    }, Emitter.NullPromise, MatchValue);
                    primaryEmitter.AppendLine(';');
                }

                handler.ToExecute.EmitInitialize(irProgram, primaryEmitter, typeargs);
                handler.ToExecute.EmitNoOpen(irProgram, primaryEmitter, typeargs, true);
            }
            if(DefaultHandler != null)
            {
                primaryEmitter.AppendStartBlock("default:");
                DefaultHandler.EmitInitialize(irProgram, primaryEmitter, typeargs);
                DefaultHandler.EmitNoOpen(irProgram, primaryEmitter, typeargs, false);
            }
            primaryEmitter.AppendLine('}');

            if (bufferedMatchVal)
                primaryEmitter.AppendEndBlock();
        }
    }

    partial class LoopStatement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            primaryEmitter.DestroyLoopResources();
            if (Action.Type == Syntax.Parsing.TokenType.Break)
                primaryEmitter.AppendLine($"goto loopbreaklabel{breakLabelId};");
            else
                primaryEmitter.AppendLine("continue;");
        }
    }

    partial class AssertStatement
    {
        public static void EmitAsserter(Emitter emitter, bool doCallStack)
        {
            emitter.AppendLine("void nhp_assert(int flag, const char* src_loc, const char* assertion_src) {");
            emitter.AppendLine("\tif(!flag) {");

            if (doCallStack)
            {
                CallStackReporting.EmitErrorLoc(emitter, "src_loc", "assertion_src");
                CallStackReporting.EmitPrintStackTrace(emitter);
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

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (irProgram.EliminateAsserts)
                return;

            Condition.Emit(irProgram, primaryEmitter, typeargs, (conditionPromise) =>
            {
                primaryEmitter.Append("nhp_assert(");
                conditionPromise(primaryEmitter);
                primaryEmitter.Append(", ");
                CharacterLiteral.EmitCString(primaryEmitter, ErrorReportedElement.SourceLocation.ToString(), false, true);
                primaryEmitter.Append(", ");
                ErrorReportedElement.EmitSrcAsCString(primaryEmitter);
                primaryEmitter.AppendLine(");");
            }, Emitter.NullPromise, true);
        }
    }
}