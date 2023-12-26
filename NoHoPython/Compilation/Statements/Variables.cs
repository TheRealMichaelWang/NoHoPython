using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class VariableReference
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (Refinements.HasValue)
                Refinements.Value.Item1.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
            if (Refinements.HasValue && Refinements.Value.Item2 != null)
                Refinements.Value.Item2(irProgram, emitter, (e) => e.Append(Variable.GetStandardIdentifier()), typeargs);
            else
                emitter.Append(Variable.GetStandardIdentifier());
        });
    }

    partial class VariableDeclaration
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            InitialValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => (WillRevaluate && Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal) || InitialValue.MustUseDestinationPromise(irProgram, typeargs, false);

        public void EmitCDecl(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.AppendLine($"{Variable.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} {Variable.GetStandardIdentifier()};");
            if (WillRevaluate && Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                emitter.LastSourceLocation = ErrorReportedElement.SourceLocation;
                emitter.AppendLine($"int init_{Variable.GetStandardIdentifier()} = 0;");
            }
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                //function doesn't need to use set argument
                if (WillRevaluate && Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    primaryEmitter.AppendStartBlock($"if(init_{Variable.GetStandardIdentifier()})");
                    Variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (emitter) => emitter.Append(Variable.GetStandardIdentifier()), Emitter.NullPromise);
                    primaryEmitter.AppendEndBlock();
                    primaryEmitter.AppendLine($"else {{init_{Variable.GetStandardIdentifier()} = 1;}}");
                }
                InitialValue.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) =>
                {
                    primaryEmitter.Append($"{Variable.GetStandardIdentifier()} = ");
                    if (InitialValue.RequiresDisposal(irProgram, typeargs, false))
                        valuePromise(primaryEmitter);
                    else
                        Variable.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, valuePromise, Emitter.NullPromise);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, false);
                destination((emitter) => emitter.Append(Variable.GetStandardIdentifier()));
            }
            else
                destination((emitter) =>
                {
                    emitter.Append($"({Variable.GetStandardIdentifier()} = ");
                    InitialValue.Emit(irProgram, emitter, typeargs, (valuePromise) =>
                    {
                        if (InitialValue.RequiresDisposal(irProgram, typeargs, false))
                            valuePromise(emitter);
                        else
                            Variable.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valuePromise, Emitter.NullPromise);
                    }, Emitter.NullPromise, false);
                    emitter.Append(')');
                });

            if(Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                Variable.DelayedLinkSetResourceDestructorId(primaryEmitter.AddResourceDestructor((emitter) => {
                    if (WillRevaluate)
                        emitter.AppendStartBlock($"if(init_{Variable.GetStandardIdentifier()})");

                    Variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, (e) => e.Append(Variable.GetStandardIdentifier()), Emitter.NullPromise);

                    if (WillRevaluate)
                        emitter.AppendEndBlock();
                }));
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class SetVariable
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs);
            SetValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal || SetValue.MustUseDestinationPromise(irProgram, typeargs, false);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            //doesn't need setARguments
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                if (Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                    primaryEmitter.AppendLine($"{Variable.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value{indirection};");
                SetValue.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) =>
                {
                    if (Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                        primaryEmitter.Append($"value{indirection}");
                    else
                        primaryEmitter.Append($"{Variable.GetStandardIdentifier()}");
                    primaryEmitter.Append(" = ");

                    if (SetValue.RequiresDisposal(irProgram, typeargs, false))
                        valuePromise(primaryEmitter);
                    else
                        Variable.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, valuePromise, Emitter.NullPromise);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, false);
                if (Variable.Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
                {
                    Variable.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (emitter) => emitter.Append(Variable.GetStandardIdentifier()), Emitter.NullPromise);
                    destination((emitter) => emitter.Append($"({Variable.GetStandardIdentifier()} = value{indirection})"));
                }
                else
                    destination((emitter) => emitter.Append(Variable.GetStandardIdentifier()));
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) => SetValue.Emit(irProgram, emitter, typeargs, (valuePromise) =>
                {
                    emitter.Append($"({Variable.GetStandardIdentifier()} = ");
                    valuePromise(emitter);
                    emitter.Append(')');
                }, Emitter.NullPromise, false));
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class CSymbolReference
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append(CSymbol.Name));
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CSymbolDeclaration
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => CSymbol.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) { }
    }
}