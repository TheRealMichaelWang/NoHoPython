using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Number);
    }

    partial class DecimalLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Number);
    }

    partial class CharacterLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

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

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('\'');
            EmitCChar(emitter, Character);
            emitter.Append('\'');
        }
    }

    partial class TrueLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }
        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append('1');
    }

    partial class FalseLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }
        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append('0');
    }

    partial class NothingLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }
        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) { }
    }

    partial class ArrayLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Elements.ForEach((element) => element.ScopeForUsedTypes(typeargs));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            StringBuilder arrayBuilder = new StringBuilder();
            if (Elements.Count == 0)
                arrayBuilder.Append("NULL");
            else if(Elements.TrueForAll((IRValue element) => element is CharacterLiteral)) //is string literal
            {
                arrayBuilder.Append("\"");
                Elements.ForEach((element) => CharacterLiteral.EmitCChar(arrayBuilder, ((CharacterLiteral)element).Character));
                arrayBuilder.Append("\"");
            }
            else
            {
                arrayBuilder.Append($"({ElementType.SubstituteWithTypearg(typeargs).GetCName()}[])");
                arrayBuilder.Append('{');

                for(int i = 0; i < Elements.Count; i++)
                {
                    if (i > 0)
                        arrayBuilder.Append(", ");
                    Elements[i].Emit(arrayBuilder, typeargs);
                }
                arrayBuilder.Append('}');
            }

            emitter.Append($"marshal{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier()}({arrayBuilder.ToString()}, {Elements.Count})");
        }
    }

    partial class AllocArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Length.ScopeForUsedTypes(typeargs);
            ProtoValue.ScopeForUsedTypes(typeargs);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"marshal_proto{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier()}(");
            Length.Emit(emitter, typeargs);
            emitter.Append(", ");

            if (ProtoValue.RequiresDisposal(typeargs))
                ProtoValue.Emit(emitter, typeargs);
            else
            {
                StringBuilder valueBuilder = new StringBuilder();
                ProtoValue.Emit(valueBuilder, typeargs);
                ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(emitter, valueBuilder.ToString());
            }
            emitter.Append(')');
        }
    }

    partial class AllocRecord
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            RecordPrototype.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            ConstructorArguments.ForEach((element) => element.ScopeForUsedTypes(typeargs));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"construct_{RecordPrototype.SubstituteWithTypearg(typeargs).GetStandardIdentifier()}(");
            for(int i = 0; i < ConstructorArguments.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");
                ConstructorArguments[i].Emit(emitter, typeargs);
            }
            emitter.Append(')');
        }
    }
}
