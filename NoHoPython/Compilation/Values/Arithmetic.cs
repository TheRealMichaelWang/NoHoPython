using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ArithmeticCast
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) 
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Input.ScopeForUsedTypes(typeargs);
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            void EmitCCast(string castTo)
            {
                emitter.Append($"(({castTo})");
                IRValue.EmitMemorySafe(Input, emitter, typeargs);
                emitter.Append(')');
            }

            switch (Operation)
            {
                case ArithmeticCastOperation.BooleanToInt:
                case ArithmeticCastOperation.CharToInt:
                    IRValue.EmitMemorySafe(Input, emitter, typeargs);
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
                    IRValue.EmitMemorySafe(Input, emitter, typeargs);
                    emitter.Append(" ? 1 : 0");
                    emitter.Append(')');
                    break;
            }
        }
    }

    partial class ArithmeticOperator
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Left.ScopeForUsedTypes(typeargs);
            Right.ScopeForUsedTypes(typeargs);
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (Operation == ArithmeticOperation.Exponentiate)
            {
                if(Type is DecimalType)
                {
                    emitter.Append("pow(");
                    IRValue.EmitMemorySafe(Left, emitter, typeargs);
                    emitter.Append(", ");
                    IRValue.EmitMemorySafe(Right, emitter, typeargs);
                    emitter.Append(')');
                }
                else
                {
                    emitter.Append("(int)pow((double)");
                    IRValue.EmitMemorySafe(Left, emitter, typeargs);
                    emitter.Append(", (double)");
                    IRValue.EmitMemorySafe(Right, emitter, typeargs);
                    emitter.Append(')');
                }
            }
            else
            {
                emitter.Append('(');
                IRValue.EmitMemorySafe(Left, emitter, typeargs);
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
                }
                IRValue.EmitMemorySafe(Right, emitter, typeargs);
                emitter.Append(')');
            }
        }
    }
}
