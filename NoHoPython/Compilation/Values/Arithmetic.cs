using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ArithmeticCast
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) 
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Input.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Input.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
            void EmitCCast(string castTo)
            {
                emitter.Append($"(({castTo})");
                IRValue.EmitDirect(irProgram, emitter, Input, typeargs, Emitter.NullPromise, true);
                emitter.Append(')');
            }

            switch (Operation)
            {
                case ArithmeticCastOperation.BooleanToInt:
                case ArithmeticCastOperation.CharToInt:
                    IRValue.EmitDirect(irProgram, emitter, Input, typeargs, Emitter.NullPromise, true);
                    break;
                case ArithmeticCastOperation.HandleToInt:
                case ArithmeticCastOperation.DecimalToInt:
                    EmitCCast("long");
                    break;
                case ArithmeticCastOperation.IntToChar:
                    EmitCCast("char");
                    break;
                case ArithmeticCastOperation.IntToDecimal:
                    EmitCCast("double");
                    break;
                case ArithmeticCastOperation.IntToBoolean:
                    emitter.Append('(');
                    IRValue.EmitDirect(irProgram, emitter, Input, typeargs, Emitter.NullPromise, isTemporaryEval);
                    emitter.Append(" ? 1 : 0");
                    emitter.Append(')');
                    break;
                case ArithmeticCastOperation.IntToHandle:
                    EmitCCast("void*");
                    break;
            }
        });
    }

    partial class HandleCast
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Input.RequiresDisposal(irProgram, typeargs, isTemporaryEval);

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Input.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Input.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
            emitter.Append($"(({TargetHandleType.GetCName(irProgram)})");
            IRValue.EmitDirect(irProgram, emitter, Input, typeargs, Emitter.NullPromise, isTemporaryEval);
            emitter.Append(')');
        });
    }

    partial class ArithmeticOperator
    {
        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right)
        {
            if(Operation == ArithmeticOperation.Exponentiate)
            {
                if (Type is not DecimalType)
                    primaryEmitter.Append("(int)");
                primaryEmitter.Append("pow(");
                left(primaryEmitter);
                primaryEmitter.Append(", ");
                right(primaryEmitter);
                primaryEmitter.Append(')');
            }
            else if(Operation == ArithmeticOperation.Modulo && Type is DecimalType)
            {
                primaryEmitter.Append("fmod(");
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
                    ArithmeticOperation.Add => " + ",
                    ArithmeticOperation.Subtract => " - ",
                    ArithmeticOperation.Multiply => " * ",
                    ArithmeticOperation.Divide => " / ",
                    ArithmeticOperation.Modulo => " % ",
                    _ => throw new InvalidOperationException()
                });
                right(primaryEmitter);
                primaryEmitter.Append(')');
            }
        }
    }

    partial class PointerAddOperator
    {
        protected override void EmitExpression(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.Promise left, Emitter.Promise right)
        {
            primaryEmitter.Append('(');
            left(primaryEmitter);
            primaryEmitter.Append(" + ");
            right(primaryEmitter);
            primaryEmitter.Append(')');
        }
    }

    partial class ArrayOperator
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => ArrayValue.MustUseDestinationPromise(irProgram, typeargs, true) || ArrayValue.RequiresDisposal(irProgram, typeargs, true);

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ArrayValue.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ArrayValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();

                primaryEmitter.AppendLine($"{ArrayValue.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arr{indirection};");
                primaryEmitter.SetArgument(ArrayValue, $"arr{indirection}", irProgram, typeargs, true);

                if (Operation != ArrayOperation.GetArrayLength && ArrayValue.RequiresDisposal(irProgram, typeargs, true))
                    throw new CannotEmitDestructorError(ArrayValue);

                destination((emitter) =>
                {
                    emitter.Append($"arr{indirection}");
                    if (Operation == ArrayOperation.GetArrayLength)
                        emitter.Append(".length");
                    else if (Operation == ArrayOperation.GetArrayHandle)
                        emitter.Append(".buffer");
                });

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    IRValue.EmitDirect(irProgram, emitter, ArrayValue, typeargs, Emitter.NullPromise, true);
                    if (Operation == ArrayOperation.GetArrayLength)
                        emitter.Append(".length");
                    else if (Operation == ArrayOperation.GetArrayHandle)
                        emitter.Append(".buffer");
                });
        }
    }
}
