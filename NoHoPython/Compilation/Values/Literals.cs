using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Number);
    }

    partial class DecimalLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append(Number);
    }

    partial class CharacterLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public static void EmitCChar(StringBuilder emitter, char c)
        {
            switch (c)
            {
                case '\\':
                    emitter.Append("\\\\");
                    break;
                case '\"':
                    emitter.Append("\\\"");
                    break;
                case '\'':
                    emitter.Append("\\\'");
                    break;
                case '\a':
                    emitter.Append("\\a");
                    break;
                case '\b':
                    emitter.Append("\\b");
                    break;
                case '\f':
                    emitter.Append("\\f");
                    break;
                case '\t':
                    emitter.Append("\\t");
                    break;
                case '\r':
                    emitter.Append("\\r");
                    break;
                case '\n':
                    emitter.Append("\\n");
                    break;
                case '\0':
                    emitter.Append("\\0");
                    break;
                default:
                    if (char.IsControl(c))
                        throw new InvalidOperationException();
                    emitter.Append(c);
                    break;
            }
        }

        public static void EmitCString(StringBuilder emitter, string str)
        {
            emitter.Append('\"');
            foreach (char c in str)
                EmitCChar(emitter, c);
            emitter.Append('\"');
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append('\'');
            EmitCChar(emitter, Character);
            emitter.Append('\'');
        }
    }

    partial class TrueLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append('1');
    }

    partial class FalseLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) => emitter.Append('0');
    }

    partial class NothingLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs) { }
    }

    partial class ArrayLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Elements.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
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
                arrayBuilder.Append($"({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}[])");
                arrayBuilder.Append('{');

                for(int i = 0; i < Elements.Count; i++)
                {
                    if (i > 0)
                        arrayBuilder.Append(", ");
                    Elements[i].Emit(irProgram, arrayBuilder, typeargs);
                }
                arrayBuilder.Append('}');
            }

            emitter.Append($"marshal{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}({arrayBuilder.ToString()}, {Elements.Count})");
        }
    }

    partial class AllocArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Length.ScopeForUsedTypes(typeargs, irBuilder);
            ProtoValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"marshal_proto{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            Length.Emit(irProgram, emitter, typeargs);
            emitter.Append(", ");

            if (ProtoValue.RequiresDisposal(typeargs))
                ProtoValue.Emit(irProgram, emitter, typeargs);
            else
            {
                StringBuilder valueBuilder = new StringBuilder();
                ProtoValue.Emit(irProgram, valueBuilder, typeargs);
                ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString());
            }
            emitter.Append(')');
        }
    }

    partial class AllocRecord
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            RecordPrototype.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ConstructorArguments.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            emitter.Append($"construct_{RecordPrototype.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            for(int i = 0; i < ConstructorArguments.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");
                ConstructorArguments[i].Emit(irProgram, emitter, typeargs);
            }
            emitter.Append(')');
        }
    }
}
