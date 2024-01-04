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
                bool directEmitLeft = Left.IsConstant && Left.IsPure && !Left.RequiresDisposal(irProgram, typeargs, true);
                bool directEmitRight = Right.IsConstant && Right.IsPure && !Right.RequiresDisposal(irProgram, typeargs, true);

                if (!directEmitLeft)
                    primaryEmitter.Append($"{Left.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} lhs{indirection};");
                if (!directEmitLeft)
                    primaryEmitter.Append($"{Right.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} rhs{indirection};");
                primaryEmitter.AppendLine();

                if (!Left.IsConstant || !Left.IsPure)
                    primaryEmitter.SetArgument(Left, $"lhs{indirection}", irProgram, typeargs, true);
                if (!Right.IsConstant || !Right.IsPure)
                    primaryEmitter.SetArgument(Right, $"rhs{indirection}", irProgram, typeargs, true);

                Emitter.Promise lhs = (leftEmitter) =>
                {
                    if (directEmitLeft)
                        IRValue.EmitDirect(irProgram, leftEmitter, Left, typeargs, Emitter.NullPromise, true);
                    else
                        leftEmitter.Append($"lhs{indirection}");
                };
                Emitter.Promise rhs = (rightEmitter) =>
                {
                    if (directEmitRight)
                        IRValue.EmitDirect(irProgram, rightEmitter, Right, typeargs, Emitter.NullPromise, true);
                    else
                        rightEmitter.Append($"rhs{indirection}");
                };
                destination((emitter) => EmitExpression(irProgram, emitter, typeargs, lhs, rhs));

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) => EmitExpression(irProgram, emitter, typeargs, IRValue.EmitDirectPromise(irProgram, Left, typeargs, Emitter.NullPromise, true), IRValue.EmitDirectPromise(irProgram, Right, typeargs, Emitter.NullPromise, true)));
        }
    }

    partial class ComparativeOperator
    {
        public override bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval)
        {
            if (Operation == CompareOperation.Equals || Operation == CompareOperation.NotEquals)
            {
                if (Left.IsTruey || Left.IsFalsey)
                    return Left.MustUseDestinationPromise(irProgram, typeargs, true);
                else if (Right.IsTruey || Right.IsFalsey)
                    return Right.MustUseDestinationPromise(irProgram, typeargs, true);
            }
            return base.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);
        }

        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right)
        {
            if (Left.Type is DecimalType)
            {
                primaryEmitter.Append(Operation switch
                {
                    CompareOperation.Equals => "!islessgreater",
                    CompareOperation.NotEquals => "islessgreater",
                    CompareOperation.More => "isgreater",
                    CompareOperation.Less => "isless",
                    CompareOperation.MoreEqual => "isgreaterequal",
                    CompareOperation.LessEqual => "islessequal",
                    _ => throw new InvalidOperationException()
                });
                primaryEmitter.Append('(');
                left(primaryEmitter);
                primaryEmitter.Append(", ");
                right(primaryEmitter);
                primaryEmitter.Append(')');
            }
            else
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

        public override void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if(Operation == CompareOperation.Equals || Operation == CompareOperation.NotEquals)
            {
                if ((Operation == CompareOperation.Equals && Left.IsTruey) || (Operation == CompareOperation.NotEquals && Left.IsFalsey))
                {
                    Right.Emit(irProgram, primaryEmitter, typeargs, rightPromise => destination((emitter) =>
                    {
                        if (Right.Type is not BooleanType)
                            emitter.Append("(int)");
                        rightPromise(emitter);
                    }), Emitter.NullPromise, true);
                    return;
                }
                else if ((Operation == CompareOperation.Equals && Left.IsFalsey) || (Operation == CompareOperation.NotEquals && Left.IsTruey))
                {
                    Right.Emit(irProgram, primaryEmitter, typeargs, (rightPromise) => destination((emitter) =>
                    {
                        emitter.Append('!');
                        if (Right.Type is not BooleanType)
                            emitter.Append("(int)");
                        rightPromise(emitter);
                    }), Emitter.NullPromise, true);
                    return;
                }
                else if ((Operation == CompareOperation.Equals && Right.IsTruey) || (Operation == CompareOperation.NotEquals && Right.IsFalsey))
                {
                    Left.Emit(irProgram, primaryEmitter, typeargs, leftPromise => destination((emitter) =>
                    {
                        if (Left.Type is not BooleanType)
                            emitter.Append("(int)");
                        leftPromise(emitter);
                    }), Emitter.NullPromise, true);
                    return;
                }
                else if ((Operation == CompareOperation.Equals && Right.IsFalsey) || (Operation == CompareOperation.NotEquals && Right.IsTruey))
                {
                    Left.Emit(irProgram, primaryEmitter, typeargs, (leftPromise) => destination((emitter) =>
                    {
                        if (Left.Type is not BooleanType)
                            emitter.Append("(int)");
                        emitter.Append('!');
                        leftPromise(emitter);
                    }), Emitter.NullPromise, true);
                    return;
                }
            }
            base.Emit(irProgram, primaryEmitter, typeargs, destination, responsibleDestroyer, isTemporaryEval);
        }
    }

    partial class LogicalOperator
    {
        public override bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval)
        {
            if (IsTruey || IsFalsey)
                return false;
            if (Left.IsTruey || Left.IsFalsey)
                return Right.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);
            else if (Right.IsTruey || Right.IsFalsey)
                return Left.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);
            return Left.MustUseDestinationPromise(irProgram, typeargs, true) || Right.MustUseDestinationPromise(irProgram, typeargs, true);
        }

        public override void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (IsTruey)
            {
                destination((emitter) => emitter.Append('1'));
                return;
            }
            else if (IsFalsey)
            {
                destination((emitter) => emitter.Append('0'));
                return;
            }

            if ((Left.IsTruey && Operation == LogicalOperation.And) || (Left.IsFalsey && Operation == LogicalOperation.Or))
            {
                Right.Emit(irProgram, primaryEmitter, typeargs, destination, Emitter.NullPromise, true);
                return;
            }
            else if ((Right.IsTruey && Operation == LogicalOperation.And) || (Right.IsFalsey && Operation == LogicalOperation.Or))
            {
                Left.Emit(irProgram, primaryEmitter, typeargs, destination, Emitter.NullPromise, true);
                return;
            }

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"int lhs{indirection};");
                primaryEmitter.SetArgument(Left, $"lhs{indirection}", irProgram, typeargs, true);

                primaryEmitter.AppendStartBlock(Operation == LogicalOperation.And ? "if(lhs)" : "if(!lhs)");
                primaryEmitter.AppendLine($"int rhs{indirection};");
                primaryEmitter.SetArgument(Right, $"rhs{indirection}", irProgram, typeargs, true);
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
                primaryEmitter.SetArgument(Array, $"arr{indirection}", irProgram, typeargs, true);
                primaryEmitter.SetArgument(Index, $"ind{indirection}", irProgram, typeargs, true);

                destination((emitter) =>
                {
                    emitter.Append($"arr{indirection}");
                    if (Array.Type is ArrayType)
                        emitter.Append(".buffer");
                    emitter.Append('[');
                    EmitBoundsCheck(irProgram, primaryEmitter, e => e.Append($"ind{indirection}"), new(e => e.Append($"arr{indirection}")), Array.Type, ErrorReportedElement);
                    emitter.Append(']');
                });

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    IRValue.EmitDirect(irProgram, emitter, Array, typeargs, Emitter.NullPromise, true);
                    if (Array.Type is ArrayType)
                        emitter.Append(".buffer");
                    emitter.Append('[');
                    EmitBoundsCheck(irProgram, primaryEmitter, IRValue.EmitDirectPromise(irProgram, Index, typeargs, Emitter.NullPromise, true), new(() => IRValue.EmitDirectPromise(irProgram, Array.GetPostEvalPure(), typeargs, Emitter.NullPromise, true)), Array.Type, ErrorReportedElement);
                    emitter.Append(']');
                });
        }

        public static void EmitBoundsCheck(IRProgram irProgram, Emitter primaryEmitter, Emitter.Promise index, Lazy<Emitter.Promise> postEvalPureArray, IType arrayType, Syntax.IAstElement errorReportedElement)
        {
            if (irProgram.DoBoundsChecking)
            {
                primaryEmitter.Append("nhp_bounds_check(");
                index(primaryEmitter);
                primaryEmitter.Append(", ");
                if (arrayType is MemorySpan memorySpan)
                    primaryEmitter.Append($"{memorySpan.Length}");
                else if (arrayType is ArrayType)
                {
                    postEvalPureArray.Value(primaryEmitter);
                    primaryEmitter.Append(".length");
                }
                else
                    throw new InvalidOperationException();
                primaryEmitter.Append(", ");
                CharacterLiteral.EmitCString(primaryEmitter, errorReportedElement.SourceLocation.ToString(), false, true);
                primaryEmitter.Append(", ");
                errorReportedElement.EmitSrcAsCString(primaryEmitter);
                primaryEmitter.Append(')');
            }
            else
                index(primaryEmitter);
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

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Array.MustUseDestinationPromise(irProgram, typeargs, true) || Index.MustUseDestinationPromise(irProgram, typeargs, true) || Value.MustUseDestinationPromise(irProgram, typeargs, false) || !IRValue.EvaluationOrderGuarenteed(Array, Index, Value) || (!IRValue.HasPostEvalPure(Array) && Array.Type is ArrayType) || Value.Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            IRValue? arrayResponsibleDestroyerValue = Array.GetResponsibleDestroyer();
            Emitter.Promise arrayResponsibleDestroyer = arrayResponsibleDestroyerValue != null ? IRValue.EmitDirectPromise(irProgram, arrayResponsibleDestroyerValue, typeargs, Emitter.NullPromise, true) : Emitter.NullPromise;
            Debug.Assert(!Array.RequiresDisposal(irProgram, typeargs, true));

            if (!MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                //this entire function does not need to use primaryEmitter.setArgument
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
                    GetValueAtIndex.EmitBoundsCheck(irProgram, primaryEmitter, indexPromise, new(e => e.Append($"arr{indirection}")), Array.Type, ErrorReportedElement);
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
                    GetValueAtIndex.EmitBoundsCheck(irProgram, primaryEmitter, IRValue.EmitDirectPromise(irProgram, Index, typeargs, Emitter.NullPromise, true), new(() => IRValue.EmitDirectPromise(irProgram, Array.GetPostEvalPure(), typeargs, Emitter.NullPromise, true)), Array.Type, ErrorReportedElement);
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
                primaryEmitter.SetArgument(Record, $"record{indirection}", irProgram, typeargs, true);

                Emitter.Promise finalEmit = (emitter) => Property.EmitGet(irProgram, emitter, typeargs, propertyContainer, (valueEmitter) => valueEmitter.Append($"record{indirection}"), responsibleDestroyer);
                if (Refinements.HasValue && Refinements.Value.Item2 != null)
                    destination((emitter) => Refinements.Value.Item2(irProgram, emitter, finalEmit, typeargs));
                else
                    destination(finalEmit);
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
                //this function does not need to use primaryEmitter.setArgument
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
                    Emitter.Promise record = IRValue.EmitDirectPromise(irProgram, Record.GetPostEvalPure(), typeargs, Emitter.NullPromise, true);
                    if (Value.RequiresDisposal(irProgram, typeargs, false))
                        IRValue.EmitDirect(irProgram, emitter, Value, typeargs, record, false);
                    else
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, IRValue.EmitDirectPromise(irProgram, Value, typeargs, record, false), record);
                    emitter.Append(')');
                });
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class ReleaseReferenceElement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => ReferenceBox.ScopeForUsedTypes(typeargs, irBuilder);

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => ReferenceBox.Type.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            ReferenceType referenceType = (ReferenceType)ReferenceBox.Type.SubstituteWithTypearg(typeargs);

            int indirection = primaryEmitter.AppendStartBlock();
            primaryEmitter.AppendLine($"{referenceType.GetCName(irProgram)} rc{indirection}");
            primaryEmitter.SetArgument(ReferenceBox, $"rc{indirection}", irProgram, typeargs, true);
            
            if(referenceType.RequiresDisposal)
                primaryEmitter.AppendLine($"rc{indirection}->is_released = 1;");
            
            destination(emitter => emitter.Append($"rc{indirection}->elem"));
            primaryEmitter.AppendEndBlock();
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class SetReferenceTypeElement
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ReferenceBox.ScopeForUsedTypes(typeargs, irBuilder);
            NewElement.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            ReferenceType referenceType = (ReferenceType)ReferenceBox.Type.SubstituteWithTypearg(typeargs);

            int indirection = primaryEmitter.AppendStartBlock();
            primaryEmitter.AppendLine($"{referenceType.GetCName(irProgram)} rc{indirection}; {referenceType.ElementType.GetCName(irProgram)} elem{indirection};");
            primaryEmitter.SetArgument(ReferenceBox, $"rc{indirection}", irProgram, typeargs, true);

            NewElement.Emit(irProgram, primaryEmitter, typeargs, promise =>
            {
                primaryEmitter.Append($"elem{indirection} = ");
                if (NewElement.RequiresDisposal(irProgram, typeargs, false))
                    promise(primaryEmitter);
                else
                    referenceType.ElementType.EmitCopyValue(irProgram, primaryEmitter, promise, e => e.Append($"rc{indirection}"));
                primaryEmitter.AppendLine(';');
            }, e => e.Append($"rc{indirection}"), false);

            if (referenceType.ElementType.RequiresDisposal)
                referenceType.ElementType.EmitFreeValue(irProgram, primaryEmitter, e => e.Append($"rc{indirection}->elem"), e => e.Append($"rc{indirection}"));
            primaryEmitter.Append($"rc{indirection}->elem = elem{indirection};");
            destination(emitter => emitter.Append($"rc{indirection}->elem"));
            primaryEmitter.AppendEndBlock();
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }
}