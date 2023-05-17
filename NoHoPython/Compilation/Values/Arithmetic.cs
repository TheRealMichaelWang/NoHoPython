using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ArithmeticCast
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) 
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Input.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            void EmitCCast(string castTo)
            {
                emitter.Append($"(({castTo})");
                IRValue.EmitMemorySafe(Input, irProgram, emitter, typeargs);
                emitter.Append(')');
            }

            switch (Operation)
            {
                case ArithmeticCastOperation.BooleanToInt:
                case ArithmeticCastOperation.CharToInt:
                    IRValue.EmitMemorySafe(Input, irProgram, emitter, typeargs);
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
                    IRValue.EmitMemorySafe(Input, irProgram, emitter, typeargs);
                    emitter.Append(" ? 1 : 0");
                    emitter.Append(')');
                    break;
                case ArithmeticCastOperation.IntToHandle:
                    EmitCCast("void*");
                    break;
            }
        }
    }

    partial class ArithmeticOperator
    {
        public override void EmitExpression(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource)
        {
            if (Operation == ArithmeticOperation.Exponentiate)
            {
                if (Type is DecimalType)
                    emitter.Append($"pow({leftCSource}, {rightCSource})");
                else
                    emitter.Append($"(int)pow((double){leftCSource}, (double){rightCSource})");
            }
            else if (Operation == ArithmeticOperation.Modulo && Type is DecimalType)
                emitter.Append($"fmod({leftCSource}, {rightCSource})");
            else
            {
                emitter.Append($"({leftCSource}");
                switch (Operation)
                {
                    case ArithmeticOperation.Add:
                        emitter.Append(" + ");
                        break;
                    case ArithmeticOperation.Subtract:
                        emitter.Append(" - ");
                        break;
                    case ArithmeticOperation.Multiply:
                        emitter.Append(" * ");
                        break;
                    case ArithmeticOperation.Divide:
                        emitter.Append(" / ");
                        break;
					case ArithmeticOperation.Modulo:
						emitter.Append(" % ");
						break;
                }
                emitter.Append($"{rightCSource})");
            }
        }
    }

    partial class ArrayOperator
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ArrayValue.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ArrayValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            void EmitOp()
            {
                switch (Operation)
                {
                    case ArrayOperation.GetArrayLength:
                        emitter.Append(".length");
                        break;
                    case ArrayOperation.GetArrayHandle:
                        emitter.Append(".buffer");
                        break;
                }
            }

            if(ArrayValue.RequiresDisposal(typeargs, false))
            {
                if (!irProgram.EmitExpressionStatements || Operation == ArrayOperation.GetArrayHandle || Operation == ArrayOperation.GetSpanHandle)
                    throw new CannotEmitDestructorError(ArrayValue);

                emitter.Append($"({{{ArrayValue.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_buffer = ");
                ArrayValue.Emit(irProgram, emitter, typeargs, "NULL", false);
                emitter.Append($"; {Type.GetCName(irProgram)} _nhp_res = _nhp_buffer");
                EmitOp();
                emitter.Append("; ");
                ArrayValue.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, "_nhp_buffer", "NULL");
                emitter.Append("; _nhp_res;})");
            }
            else
            {
                ArrayValue.Emit(irProgram, emitter, typeargs, "NULL", false);
                EmitOp();
            }
        }
    }
}
