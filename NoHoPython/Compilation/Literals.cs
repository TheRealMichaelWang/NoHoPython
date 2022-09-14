using NoHoPython.Typing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public void ScopeForUsedTypes() { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Number);
    }

    partial class DecimalLiteral
    {
        public void ScopeForUsedTypes() { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Number);
    }

    partial class CharacterLiteral
    {
        public static void EmitCChar(StringBuilder emitter, char c)
        {
            if (char.IsControl(c))
            {
                switch (c)
                {
                    case '\"':
                        emitter.Append("\\\"");
                        break;
                    case '\'':
                        emitter.Append("\\\'");
                        break;
                    case '\a':
                        emitter.Append("\\\a");
                        break;
                    case '\b':
                        emitter.Append("\\\b");
                        break;
                    case '\f':
                        emitter.Append("\\\f");
                        break;
                    case '\t':
                        emitter.Append("\\\t");
                        break;
                    case '\r':
                        emitter.Append("\\\r");
                        break;
                    case '\n':
                        emitter.Append("\\\n");
                        break;
                    case '\0':
                        emitter.Append("\\0");
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
            else
                emitter.Append(c);
        }

        public void ScopeForUsedTypes() { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('\'');
            EmitCChar(emitter, Character);
            emitter.Append('\'');
        }
    }

    partial class TrueLiteral
    {
        public void ScopeForUsedTypes() { }
        public void Emit(StringBuilder emitter) => emitter.Append('1');
    }

    partial class FalseLiteral
    {
        public void ScopeForUsedTypes() { }
        public void Emit(StringBuilder emitter) => emitter.Append('0');
    }

    partial class NothingLiteral
    {
        public void ScopeForUsedTypes() { }
        public void Emit(StringBuilder emitter) { }
    }

    partial class ArrayLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) => ElementType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"({Type.SubstituteWithTypearg(typeargs).GetCName()}[])");
            emitter.Append("memcpy(malloc(");

            emitter.Append(Elements.Count);
            emitter.Append($" * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName()})), ");

            if(Elements.TrueForAll((IRValue element) => element is CharacterLiteral)) //is string literal
            {
                emitter.Append("\"");
                Elements.ForEach((element) => CharacterLiteral.EmitCChar(emitter, ((CharacterLiteral)element).Character));
                emitter.Append("\"");
            }
            else
            {
                emitter.Append($"({ElementType.SubstituteWithTypearg(typeargs).GetCName()}[])");
                emitter.Append('{');

                for(int i = 0; i < Elements.Count; i++)
                {
                    if (i > 0)
                        emitter.Append(", ");
                    Elements[i].Emit(emitter, typeargs);
                }
                emitter.Append('}');
            }
            emitter.Append(", ");
            emitter.Append(Elements.Count);
            emitter.Append($" * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName()}));");
        }
    }

    partial class AllocRecord
    {
        public void ScopeForUsedTypes() => RecordPrototype.ForwardDeclare();
    }
}
