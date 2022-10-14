using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ArithmeticCast
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) 
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Input.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
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
            }
        }
    }

    partial class ArithmeticOperator
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Left.ScopeForUsedTypes(typeargs, irBuilder);
            Right.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (Operation == ArithmeticOperation.Exponentiate)
            {
                if(Type is DecimalType)
                {
                    emitter.Append("pow(");
                    IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs);
                    emitter.Append(", ");
                    IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs);
                    emitter.Append(')');
                }
                else
                {
                    emitter.Append("(int)pow((double)");
                    IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs);
                    emitter.Append(", (double)");
                    IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs);
                    emitter.Append(')');
                }
            }
			else if(Operation == ArithmeticOperation.Modulo && Type is DecimalType)
			{
				emitter.Append("fmod(");
                IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs);
				emitter.Append(", ");
                IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs);
                emitter.Append(')');
			}
            else
            {
                emitter.Append('(');
                IRValue.EmitMemorySafe(Left, irProgram, emitter, typeargs);
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
                IRValue.EmitMemorySafe(Right, irProgram, emitter, typeargs);
                emitter.Append(')');
            }
        }
    }

    partial class ArrayOperator
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ArrayValue.Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ArrayValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
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

            if(ArrayValue.RequiresDisposal(typeargs))
            {
                if (!irProgram.EmitExpressionStatements || Operation == ArrayOperation.GetArrayHandle)
                    throw new CannotEmitDestructorError(ArrayValue);

                emitter.Append($"({{{ArrayValue.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_buffer = ");
                ArrayValue.Emit(irProgram, emitter, typeargs);
                emitter.Append($"; {Type.GetCName(irProgram)} _nhp_res = _nhp_buffer");
                EmitOp();
                emitter.Append("; ");
                ArrayValue.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, "_nhp_buffer");
                emitter.Append("; _nhp_res;})");
            }
            else
            {
                ArrayValue.Emit(irProgram, emitter, typeargs);
                EmitOp();
            }
        }
    }
}
