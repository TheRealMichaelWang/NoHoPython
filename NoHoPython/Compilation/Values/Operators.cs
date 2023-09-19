using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class Property
    {
        public abstract bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs);

        public virtual void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public abstract bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer);
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class BinaryOperator
    {
        public virtual void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Left.ScopeForUsedTypes(typeargs, irBuilder);
            Right.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public virtual bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Left.MustUseDestinationPromise(irProgram, typeargs, true) || Right.MustUseDestinationPromise(irProgram, typeargs, true) || Left.RequiresDisposal(irProgram, typeargs, true) || Right.RequiresDisposal(irProgram, typeargs, true) || !IRValue.EvaluationOrderGuarenteed(Left, Right);

        protected abstract void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right);

        public virtual void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Left.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} lhs{indirection}; {Right.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} rhs{indirection};");
                Left.Emit(irProgram, primaryEmitter, typeargs, (leftPromise) =>
                {
                    primaryEmitter.Append($"lhs{indirection} = ");
                    leftPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Right.Emit(irProgram, primaryEmitter, typeargs, (leftPromise) =>
                {
                    primaryEmitter.Append($"rhs{indirection} = ");
                    leftPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);

                Emitter.Promise lhs = (leftEmitter) => leftEmitter.Append($"lhs{indirection}");
                Emitter.Promise rhs = (rightEmitter) => rightEmitter.Append($"rhs{indirection}");
                destination((emitter) => EmitExpression(irProgram, emitter, typeargs, lhs, rhs));

                if (Left.RequiresDisposal(irProgram, typeargs, true))
                    Left.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, lhs, Emitter.NullPromise);
                if (Right.RequiresDisposal(irProgram, typeargs, true))
                    Right.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, rhs, Emitter.NullPromise);

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) => EmitExpression(irProgram, emitter, typeargs, IRValue.EmitDirectPromise(irProgram, Left, typeargs, Emitter.NullPromise, true), IRValue.EmitDirectPromise(irProgram, Right, typeargs, Emitter.NullPromise, true)));
        }
    }

    partial class ComparativeOperator
    {
        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right)
        {
            primaryEmitter.Append('(');
            left(primaryEmitter);
            primaryEmitter.Append(Operation switch
            {
                CompareOperation.Equals => " == ",
                CompareOperation.NotEquals => " != ",
                CompareOperation.More => " > ",
                CompareOperation.Less => " < ",
                CompareOperation.MoreEqual => " >= ",
                CompareOperation.LessEqual => " <= ",
                _ => throw new InvalidOperationException()
            });
            right(primaryEmitter);
            primaryEmitter.Append(')');
        }
    }

    partial class LogicalOperator
    {
        public override bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Left.MustUseDestinationPromise(irProgram, typeargs, true) || Right.MustUseDestinationPromise(irProgram, typeargs, true);

        public override void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"int lhs{indirection};");
                Left.Emit(irProgram, primaryEmitter, typeargs, (leftPromise) =>
                {
                    primaryEmitter.Append($"lhs{indirection} = ");
                    leftPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);

                primaryEmitter.AppendStartBlock(Operation == LogicalOperation.And ? "if(lhs)" : "if(!lhs)");
                primaryEmitter.AppendLine($"int rhs{indirection};");
                Right.Emit(irProgram, primaryEmitter, typeargs, (rightPromise) =>
                {
                    primaryEmitter.Append($"rhs{indirection} = ");
                    rightPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                destination((emitter) => emitter.Append($"rhs{indirection}"));
                primaryEmitter.AppendEndBlock();

                primaryEmitter.AppendStartBlock("else");
                if (Operation == LogicalOperation.And)
                    destination((emitter) => emitter.Append('0'));
                else
                    destination((emitter) => emitter.Append('1'));
                primaryEmitter.AppendEndBlock();

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append('(');
                    IRValue.EmitDirect(irProgram, emitter, Left, typeargs, Emitter.NullPromise, true);
                    emitter.Append(Operation == LogicalOperation.And ? " && " : " || ");
                    IRValue.EmitDirect(irProgram, emitter, Right, typeargs, Emitter.NullPromise, true);
                    emitter.Append(')');
                });
        }

        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right) => throw new InvalidOperationException();
    }

    partial class BitwiseOperator
    {
        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right)
        {
            primaryEmitter.Append('(');
            left(primaryEmitter);
            primaryEmitter.Append(Operation switch
            {
                BitwiseOperation.And => " & ",
                BitwiseOperation.Or => " | ",
                BitwiseOperation.Xor => " ^ ",
                BitwiseOperation.ShiftLeft => " << ",
                BitwiseOperation.ShiftRight => " >> ",
                _ => throw new InvalidOperationException()
            });
            right(primaryEmitter);
            primaryEmitter.Append(')');
        }
    }

    partial class GetValueAtIndex
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Array.ScopeForUsedTypes(typeargs, irBuilder);
            Index.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Array.MustUseDestinationPromise(irProgram, typeargs, true) || Index.MustUseDestinationPromise(irProgram, typeargs, true) || Array.RequiresDisposal(irProgram, typeargs, true) || !IRValue.EvaluationOrderGuarenteed(Array, Index) || (irProgram.DoBoundsChecking && Array.Type is ArrayType && !IRValue.HasPostEvalPure(Array));

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Array.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arr{indirection}; {Index.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} ind{indirection};");
                Array.Emit(irProgram, primaryEmitter, typeargs, (arrayPromise) =>
                {
                    primaryEmitter.Append($"arr{indirection} = ");
                    arrayPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Index.Emit(irProgram, primaryEmitter, typeargs, (indexPromise) =>
                {
                    primaryEmitter.Append($"ind{indirection} = ");
                    indexPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);

                destination((emitter) =>
                {
                    emitter.Append($"arr{indirection}");
                    if (Array.Type is ArrayType)
                        emitter.Append(".buffer");
                    emitter.Append('[');
                    if (irProgram.DoBoundsChecking)
                    {
                        emitter.Append($"nhp_bounds_check(ind{indirection}, ");
                        if (Array.Type is MemorySpan memorySpan)
                            emitter.Append(memorySpan.Length.ToString());
                        else
                            emitter.Append($"arr{indirection}.length");
                        emitter.Append(')');
                    }
                    else
                        emitter.Append($"ind{indirection}");
                    emitter.Append(']');
                });

                if (Array.RequiresDisposal(irProgram, typeargs, true))
                    Array.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (emitter) => emitter.Append($"arr{indirection}"), Emitter.NullPromise);

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    IRValue.EmitDirect(irProgram, emitter, Array, typeargs, Emitter.NullPromise, true);
                    if (Array.Type is ArrayType)
                        emitter.Append(".buffer");
                    emitter.Append('[');
                    if (irProgram.DoBoundsChecking)
                    {
                        emitter.Append("nhp_bounds_check(");
                        IRValue.EmitDirect(irProgram, emitter, Index, typeargs, Emitter.NullPromise, true);
                        emitter.Append(", ");
                        if (Array.Type is MemorySpan memorySpan)
                            emitter.Append(memorySpan.Length.ToString());
                        else
                        {
                            Debug.Assert(!Array.GetPostEvalPure().RequiresDisposal(irProgram, typeargs, true));
                            IRValue.EmitDirect(irProgram, emitter, Array.GetPostEvalPure(), typeargs, Emitter.NullPromise, true);
                            emitter.Append(".length");
                        }
                        emitter.Append(')');
                    }
                    else
                        IRValue.EmitDirect(irProgram, emitter, Index, typeargs, Emitter.NullPromise, true);
                    emitter.Append(']');
                });
        }
    }

    partial class SetValueAtIndex
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Array.ScopeForUsedTypes(typeargs, irBuilder);
            Index.ScopeForUsedTypes(typeargs, irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Array.MustUseDestinationPromise(irProgram, typeargs, true) || Index.MustUseDestinationPromise(irProgram, typeargs, true) || Value.MustUseDestinationPromise(irProgram, typeargs, false) || !IRValue.EvaluationOrderGuarenteed(Array, Index, Value) || !IRValue.HasPostEvalPure(Array) || Value.Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            IRValue? arrayResponsibleDestroyerValue = Array.GetResponsibleDestroyer();
            Emitter.Promise arrayResponsibleDestroyer = arrayResponsibleDestroyerValue != null ? IRValue.EmitDirectPromise(irProgram, arrayResponsibleDestroyerValue, typeargs, Emitter.NullPromise, true) : Emitter.NullPromise;
            Debug.Assert(!Array.RequiresDisposal(irProgram, typeargs, true));

            if (!MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Array.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arr{indirection}; {Index.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} ind{indirection}; {Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value{indirection};");
                Array.Emit(irProgram, primaryEmitter, typeargs, (arrayPromise) =>
                {
                    primaryEmitter.Append($"arr{indirection} = ");
                    arrayPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Index.Emit(irProgram, primaryEmitter, typeargs, (indexPromise) =>
                {
                    primaryEmitter.Append($"ind{indirection} = ");
                    if (irProgram.DoBoundsChecking)
                    {
                        primaryEmitter.Append("nhp_bounds_check(");
                        indexPromise(primaryEmitter);
                        primaryEmitter.Append(", ");
                        if (Array.Type is MemorySpan memorySpan)
                            primaryEmitter.Append(memorySpan.Length.ToString());
                        else
                            primaryEmitter.Append($"arr{indirection}.length");
                        primaryEmitter.Append(')');
                    }
                    else
                        indexPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Value.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) =>
                {
                    primaryEmitter.Append($"value{indirection} = ");
                    if (Value.RequiresDisposal(irProgram, typeargs, false))
                        valuePromise(primaryEmitter);
                    else
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, valuePromise, arrayResponsibleDestroyer);
                    primaryEmitter.AppendLine(';');
                }, arrayResponsibleDestroyer, false);
                Value.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (oldValEmitter) =>
                {
                    oldValEmitter.Append($"arr{indirection}");
                    if (Array.Type is ArrayType)
                        oldValEmitter.Append(".buffer");
                    oldValEmitter.Append($"[ind{indirection}]");
                }, arrayResponsibleDestroyer);
                destination((emitter) =>
                {
                    emitter.Append($"arr{indirection}");
                    if (Array.Type is ArrayType)
                        emitter.Append(".buffer");
                    emitter.Append($"[ind{indirection}] = value{indirection}");
                });
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append('(');
                    IRValue.EmitDirect(irProgram, emitter, Array, typeargs, Emitter.NullPromise, true);
                    if (Array.Type is ArrayType)
                        emitter.Append(".buffer");
                    emitter.Append('[');
                    if (irProgram.DoBoundsChecking)
                    {
                        emitter.Append("nhp_bounds_check(");
                        IRValue.EmitDirect(irProgram, emitter, Index, typeargs, Emitter.NullPromise, true);
                        emitter.Append(", ");
                        if (Array.Type is MemorySpan memorySpan)
                            emitter.Append(memorySpan.Length.ToString());
                        else
                        {
                            Debug.Assert(!Array.GetPostEvalPure().RequiresDisposal(irProgram, typeargs, true));
                            IRValue.EmitDirect(irProgram, emitter, Array.GetPostEvalPure(), typeargs, Emitter.NullPromise, true);
                            emitter.Append(".length");
                        }
                        emitter.Append(')');
                    }
                    else
                        IRValue.EmitDirect(irProgram, emitter, Index, typeargs, Emitter.NullPromise, true);
                    emitter.Append("] = ");
                    IRValue.EmitDirect(irProgram, emitter, Value, typeargs, arrayResponsibleDestroyer, false);
                    emitter.Append(')');
                });
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class GetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Property.ScopeForUse(false, typeargs, irBuilder);
            Record.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Property.RequiresDisposal(typeargs);

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Record.MustUseDestinationPromise(irProgram, typeargs, true) || Record.RequiresDisposal(irProgram, typeargs, true);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            IPropertyContainer propertyContainer = (IPropertyContainer)Record.Type.SubstituteWithTypearg(typeargs);

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Record.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} record{indirection};");
                Record.Emit(irProgram, primaryEmitter, typeargs, (recordPromise) =>
                {
                    primaryEmitter.Append($"record{indirection} = ");
                    recordPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);

                Emitter.Promise finalEmit = (emitter) => Property.EmitGet(irProgram, emitter, typeargs, propertyContainer, (valueEmitter) => valueEmitter.Append($"record{indirection}"), responsibleDestroyer);
                if (Refinements.HasValue && Refinements.Value.Item2 != null)
                    destination((emitter) => Refinements.Value.Item2(irProgram, emitter, finalEmit, typeargs));
                else
                    destination(finalEmit);

                if (Record.RequiresDisposal(irProgram, typeargs, true))
                    Record.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (valueEmitter) => valueEmitter.Append($"record{indirection}"), Emitter.NullPromise);
                primaryEmitter.AppendEndBlock();
            }
            else
            {
                Emitter.Promise finalEmit = (emitter) => Property.EmitGet(irProgram, emitter, typeargs, propertyContainer, IRValue.EmitDirectPromise(irProgram, Record, typeargs, Emitter.NullPromise, true), responsibleDestroyer);
                if (Refinements.HasValue && Refinements.Value.Item2 != null)
                    destination((emitter) => Refinements.Value.Item2(irProgram, emitter, finalEmit, typeargs));
                else
                    destination(finalEmit);
            }
        }
    }

    partial class SetPropertyValue
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Property.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Record.ScopeForUsedTypes(typeargs, irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Record.MustUseDestinationPromise(irProgram, typeargs, true) || Value.MustUseDestinationPromise(irProgram, typeargs, false) || !IRValue.EvaluationOrderGuarenteed(Record, Value) || !IRValue.HasPostEvalPure(Record) || (Value.Type.SubstituteWithTypearg(typeargs).RequiresDisposal && !IsInitializingProperty);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (Record.RequiresDisposal(irProgram, typeargs, true))
                throw new CannotEmitDestructorError(Record);

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Record.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} record{indirection}; {Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value{indirection};");
                Record.Emit(irProgram, primaryEmitter, typeargs, (recordPromise) =>
                {
                    primaryEmitter.Append($"record{indirection} = ");
                    recordPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Emitter.Promise recordResponsibleDestroyer = (emitter) => emitter.Append($"record{indirection}");
                Value.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) =>
                {
                    primaryEmitter.Append($"value{indirection} = ");
                    if (Value.RequiresDisposal(irProgram, typeargs, false))
                        valuePromise(primaryEmitter);
                    else
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, valuePromise, recordResponsibleDestroyer);
                    primaryEmitter.AppendLine(';');
                }, recordResponsibleDestroyer, false);

                if(!IsInitializingProperty)
                    Value.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (emitter) => emitter.Append($"record{indirection}->{Property.Name}"), recordResponsibleDestroyer);
                
                destination((emitter) => emitter.Append($"record{indirection}->{Property.Name} = value{indirection}"));
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append('(');
                    IRValue.EmitDirect(irProgram, emitter, Record, typeargs, Emitter.NullPromise, isTemporaryEval);
                    emitter.Append($"->{Property.Name} = ");
                    IRValue.EmitDirect(irProgram, emitter, Value, typeargs, IRValue.EmitDirectPromise(irProgram, Record.GetPostEvalPure(), typeargs, Emitter.NullPromise, true), false);
                    emitter.Append(')');
                });
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }
}