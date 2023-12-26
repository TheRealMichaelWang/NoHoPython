using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class SizeofOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => TypeToMeasure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append($"sizeof({TypeToMeasure.SubstituteWithTypearg(typeargs).GetCName(irProgram)})"));
    }

    partial class MemoryGet
    {
        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right)
        {
            HandleType handleType = (HandleType)Left.Type;

            primaryEmitter.Append('(');
            if (handleType.ValueType is NothingType)
                primaryEmitter.Append($"({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
            left(primaryEmitter);
            primaryEmitter.Append(")[");
            right(primaryEmitter);
            primaryEmitter.Append(']');
        }
    }

    partial class MemorySet
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
            Index.ScopeForUsedTypes(typeargs, irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => !IRValue.EvaluationOrderGuarenteed(Address, Index, Value) || Address.MustUseDestinationPromise(irProgram, typeargs, true) || Index.MustUseDestinationPromise(irProgram, typeargs, true) || Value.MustUseDestinationPromise(irProgram, typeargs, false);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise destinationResponsibleDestroyer, bool isTemporaryEval)
        {
            IRValue? responsibleDestroyerVal = Address.GetResponsibleDestroyer();
            Emitter.Promise responsibleDestroyer = responsibleDestroyerVal != null ? IRValue.EmitDirectPromise(irProgram, responsibleDestroyerVal, typeargs, Emitter.NullPromise, true) : Emitter.NullPromise;
            HandleType handleType = (HandleType)Address.Type.SubstituteWithTypearg(typeargs);

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                //function does not need setARgument
                int indirection = primaryEmitter.AppendStartBlock();

                if (handleType.ValueType is not NothingType)
                    primaryEmitter.Append($"{handleType.GetCName(irProgram)}");
                else
                    primaryEmitter.Append($"{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*");
                primaryEmitter.Append($"address{indirection}; ");

                primaryEmitter.AppendLine($"{Index.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} index{indirection};");
                Address.Emit(irProgram, primaryEmitter, typeargs, (addressPromise) =>
                {
                    primaryEmitter.Append($"address{indirection} = ");
                    if (handleType.ValueType is NothingType)
                        primaryEmitter.Append($"({Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
                    addressPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Index.Emit(irProgram, primaryEmitter, typeargs, (indexPromise) =>
                {
                    primaryEmitter.Append($"index{indirection} = ");
                    indexPromise(primaryEmitter);
                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                Value.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) =>
                {
                    primaryEmitter.Append($"address{indirection}[index{indirection}] = ");
                    if (Value.RequiresDisposal(irProgram, typeargs, false))
                        valuePromise(primaryEmitter);
                    else
                        Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, valuePromise, responsibleDestroyer);
                    primaryEmitter.AppendLine(';');
                }, responsibleDestroyer, false);

                destination((emitter) => primaryEmitter.Append($"address{indirection}[index{indirection}]"));
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append("((");
                    if (handleType.ValueType is NothingType)
                        emitter.Append($"({Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
                    IRValue.EmitDirect(irProgram, emitter, Address, typeargs, Emitter.NullPromise, true);
                    emitter.Append(")[");
                    IRValue.EmitDirect(irProgram, emitter, Index, typeargs, Emitter.NullPromise, true);
                    emitter.Append("] = ");
                    Value.Emit(irProgram, emitter, typeargs, (valuePromise) =>
                    {
                        if (Value.RequiresDisposal(irProgram, typeargs, false))
                            valuePromise(emitter);
                        else
                            Value.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valuePromise, responsibleDestroyer);
                    }, responsibleDestroyer, false);
                    emitter.Append(')');
                });
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
    }

    partial class MarshalHandleIntoArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Length.ScopeForUsedTypes(typeargs, irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Address.MustUseDestinationPromise(irProgram, typeargs, true) || Length.MustUseDestinationPromise(irProgram, typeargs, true) || !Length.IsPure || !IRValue.EvaluationOrderGuarenteed(Address, Length);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            ArrayType type = new(ElementType.SubstituteWithTypearg(typeargs));
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Address.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} addr{indirection}; {Length.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} len{indirection};");
                primaryEmitter.SetArgument(Address, $"addr{indirection}",irProgram, typeargs, true);
                primaryEmitter.SetArgument(Length, $"len{indirection}", irProgram, typeargs, true);
                destination((emitter) =>
                {
                    if (ElementType.SubstituteWithTypearg(typeargs).RequiresDisposal) 
                    {
                        emitter.Append($"marshal_foreign{type.GetStandardIdentifier(irProgram)}(");
                        emitter.Append($"addr{indirection}, len{indirection}");
                        if (type.MustSetResponsibleDestroyer)
                        {
                            emitter.Append(", ");
                            responsibleDestroyer(emitter);
                        }
                        emitter.Append(')');
                    }
                    else
                    {
                        emitter.Append($"({type.GetCName(irProgram)}){{.buffer = memcpy({irProgram.MemoryAnalyzer.Allocate($"len{indirection} * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)})")}, addr{indirection}), .length = len{indirection}}}");
                    }
                });
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append($"({type.GetCName(irProgram)}){{.buffer = memcpy(");
                    irProgram.MemoryAnalyzer.EmitAllocate(emitter, (e) =>
                    {
                        IRValue.EmitDirect(irProgram, e, Length, typeargs, Emitter.NullPromise, true);
                        e.Append($" * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)})");
                    });
                    emitter.Append(", ");
                    IRValue.EmitDirect(irProgram, emitter, Address, typeargs, Emitter.NullPromise, true);
                    emitter.Append(", ");
                    IRValue.EmitDirect(irProgram, emitter, Length, typeargs, Emitter.NullPromise, true);
                    emitter.Append($" * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}))");

                    emitter.Append(", .length = ");
                    IRValue.EmitDirect(irProgram, emitter, Length, typeargs, Emitter.NullPromise, true);
                    emitter.Append('}');
                });
        }
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Span.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Span.RequiresDisposal(irProgram, typeargs, isTemporaryEval);

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Span.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            ArrayType type = new(ElementType.SubstituteWithTypearg(typeargs));
            MemorySpan memorySpan = (MemorySpan)Span.Type.SubstituteWithTypearg(typeargs);

            Span.Emit(irProgram, primaryEmitter, typeargs, (spanPromise) => destination((emitter) =>
            {
                emitter.Append($"(({type.GetCName(irProgram)}){{ .buffer = ");
                spanPromise(emitter);
                emitter.Append($", .length = {memorySpan.Length}}})");
            }), responsibleDestroyer, isTemporaryEval);
        }
    }

    partial class MarshalIntoLowerTuple
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypeParameters(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => TargetType.SubstituteWithTypearg(typeargs).RequiresDisposal;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Value.MustUseDestinationPromise(irProgram, typeargs, true) || Value.RequiresDisposal(irProgram, typeargs, true) || !Value.IsPure;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            TupleType targetType = (TupleType)TargetType.SubstituteWithTypearg(typeargs);
            List<Property> targetProperties = targetType.GetProperties();

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} upper{indirection};");
                primaryEmitter.SetArgument(Value, $"upper{indirection}", irProgram, typeargs, true);
                primaryEmitter.AppendLine($"{targetType.GetCName(irProgram)} result{indirection};");
                foreach (Property property in targetProperties)
                {
                    primaryEmitter.Append($"result{indirection}.{property.Name} = ");
                    property.Type.EmitCopyValue(irProgram, primaryEmitter, (emitter) => emitter.Append($"upper{indirection}.{property.Name}"), responsibleDestroyer);
                    primaryEmitter.AppendLine(';');
                }
                destination((emitter) => emitter.Append($"result{indirection}"));
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append($"({TargetType.GetCName(irProgram)}){{");
                    foreach (Property property in targetProperties)
                    {
                        if (property != targetProperties.First())
                            emitter.Append(", ");

                        emitter.Append($".{property.Name} = ");
                        property.Type.EmitCopyValue(irProgram, emitter, IRValue.EmitDirectPromise(irProgram, Value, typeargs, responsibleDestroyer, true), responsibleDestroyer);
                        emitter.Append($".{property.Name}");
                    }
                    emitter.Append('}');
                });
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class MemoryDestroy
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            AddressType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (AddressType.ValueType.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                AddressType.ValueType.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (emitter) => Address.Emit(irProgram, emitter, typeargs, (addressPromise) =>
                {
                    emitter.Append("*(");
                    addressPromise(emitter);
                    emitter.Append(')');
                }, Emitter.NullPromise, true), Emitter.NullPromise);
            }
        }
    }
}