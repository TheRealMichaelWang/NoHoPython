using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append(Number);
    }

    partial class DecimalLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append(Number);
    }

    partial class CharacterLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public static void EmitCChar(StringBuilder emitter, char c, bool formatChar)
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
                case '%':
                    emitter.Append(formatChar ? "%%" : "%");
                    break;
                default:
                    if (char.IsControl(c))
                        throw new InvalidOperationException();
                    emitter.Append(c);
                    break;
            }
        }

        public static void EmitCString(StringBuilder emitter, string str, bool formatStr, bool quoteEncapsulate)
        {
            if(quoteEncapsulate)
                emitter.Append('\"');

            foreach (char c in str)
                EmitCChar(emitter, c, formatStr);
            
            if(quoteEncapsulate)
                emitter.Append('\"');
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            emitter.Append('\'');
            EmitCChar(emitter, Character, false);
            emitter.Append('\'');
        }
    }

    partial class TrueLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append('1');
    }

    partial class FalseLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append('0');
    }

    partial class EmptyTypeLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) { }
    }

    partial class ArrayLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Elements.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            StringBuilder arrayBuilder = new();
            if (Elements.Count == 0)
                arrayBuilder.Append("NULL");
            else if(Elements.TrueForAll((IRValue element) => element is CharacterLiteral)) //is string literal
            {
                arrayBuilder.Append("\"");
                Elements.ForEach((element) => CharacterLiteral.EmitCChar(arrayBuilder, ((CharacterLiteral)element).Character, false));
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
                    Elements[i].Emit(irProgram, arrayBuilder, typeargs, responsibleDestroyer);
                }
                arrayBuilder.Append('}');
            }

            emitter.Append($"marshal{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}({arrayBuilder.ToString()}, {Elements.Count}");
            if (Type.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
                emitter.Append($", {responsibleDestroyer}");
            emitter.Append(')');
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if ((!Length.IsPure && !ProtoValue.IsConstant) || (!ProtoValue.IsPure && !Length.IsConstant))
                throw new CannotEnsureOrderOfEvaluation(this);
            
            emitter.Append($"marshal_proto{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            Length.Emit(irProgram, emitter, typeargs, "NULL");
            emitter.Append(", ");

            if (ProtoValue.RequiresDisposal(typeargs))
                ProtoValue.Emit(irProgram, emitter, typeargs, "NULL");
            else
            {
                StringBuilder valueBuilder = new();
                ProtoValue.Emit(irProgram, valueBuilder, typeargs, "NULL");
                ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), "NULL");
            }

            if (Type.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
                emitter.Append($", {responsibleDestroyer})");
            else
                emitter.Append(')');
        }
    }

    partial class AllocRecord
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            RecordPrototype.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> releasedArguments, int currentNestedCall, string responsibleDestroyer)
        {
            emitter.Append($"construct_{RecordPrototype.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            EmitArguments(irProgram, emitter, typeargs, releasedArguments, currentNestedCall);
            if (Arguments.Count > 0)
                emitter.Append(", ");
            emitter.Append($"(_nhp_std_record_mask_t*){responsibleDestroyer})");
        }
    }
}
