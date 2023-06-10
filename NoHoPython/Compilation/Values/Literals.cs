using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval) => emitter.Append(Number.ToString());
    }

    partial class DecimalLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval) => emitter.Append(Number.ToString());
    }

    partial class CharacterLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public static void EmitCChar(IEmitter emitter, char c, bool formatChar)
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
                    Debug.Assert(!char.IsControl(c));
                    emitter.Append(c);
                    break;
            }
        }

        public static void EmitCString(IEmitter emitter, string str, bool formatStr, bool quoteEncapsulate)
        {
            if(quoteEncapsulate)
                emitter.Append('\"');

            foreach (char c in str)
                EmitCChar(emitter, c, formatStr);
            
            if(quoteEncapsulate)
                emitter.Append('\"');
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            emitter.Append('\'');
            EmitCChar(emitter, Character, false);
            emitter.Append('\'');
        }
    }

    partial class TrueLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval) => emitter.Append('1');
    }

    partial class FalseLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval) => emitter.Append('0');
    }

    partial class NullPointerLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        
        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (HandleType.ValueType is not NothingType)
                emitter.Append($"({HandleType.SubstituteWithTypearg(typeargs).GetCName(irProgram)})");
            emitter.Append("NULL");
        }
    }

    partial class StaticCStringLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            emitter.Append('\"');
            foreach (char c in String)
                CharacterLiteral.EmitCChar(emitter, c, false);
            emitter.Append('\"');
        }
    }

    partial class EmptyTypeLiteral
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval) { }
    }

    partial class ArrayLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Elements.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => !(isTemporaryEval && Elements.All((elem) => !elem.RequiresDisposal(typeargs, true)));

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            void EmitStatic()
            {
                emitter.Append($"({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}[])");
                emitter.Append('{');

                for (int i = 0; i < Elements.Count; i++)
                {
                    if (i > 0)
                        emitter.Append(", ");
                    Elements[i].Emit(irProgram, emitter, typeargs, responsibleDestroyer, isTemporaryEval);
                }
                emitter.Append('}');
            }

            if(!RequiresDisposal(typeargs, isTemporaryEval))
            {
                EmitStatic();
                return;
            }

            string sizeCSource = $"{Elements.Count} * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)})";
            emitter.Append($"memcpy({irProgram.MemoryAnalyzer.Allocate(sizeCSource)}, ");

            if (Elements.Count == 0)
                emitter.Append("NULL");
            else
                EmitStatic();

            emitter.Append($", {sizeCSource})");
        }
    }

    partial class TupleLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            TupleElements.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval)
        {
            foreach (IType valueType in TupleType.ValueTypes.Keys)
                if (valueType.SubstituteWithTypearg(typeargs).RequiresDisposal)
                    return true;
            return false;
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (!IRValue.EvaluationOrderGuarenteed(TupleElements))
                throw new CannotEnsureOrderOfEvaluation(this);

            emitter.Append($"({TupleType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}) {{");

            List<Property> initializeProperties = ((TupleType)TupleType.SubstituteWithTypearg(typeargs)).GetProperties();
            ITypeComparer typeComparer = new ITypeComparer();
            initializeProperties.Sort((a, b) => typeComparer.Compare(a.Type, b.Type));
            
            for(int i = 0; i < initializeProperties.Count; i++)
            {
                if (i > 0)
                    emitter.Append(", ");

                emitter.Append($".{initializeProperties[i].Name} = ");
                if (TupleElements[i].RequiresDisposal(typeargs, false))
                    TupleElements[i].Emit(irProgram, emitter, typeargs, responsibleDestroyer, false);
                else
                    initializeProperties[i].Type.EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(TupleElements[i], irProgram, typeargs, "NULL"), responsibleDestroyer);
            }

            emitter.Append('}');
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (!IRValue.EvaluationOrderGuarenteed(Length, ProtoValue))
                throw new CannotEnsureOrderOfEvaluation(this);
            
            emitter.Append($"marshal_proto{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            Length.Emit(irProgram, emitter, typeargs, "NULL", true);
            emitter.Append(", ");

            if (ProtoValue.RequiresDisposal(typeargs, false))
                ProtoValue.Emit(irProgram, emitter, typeargs, "NULL", false);
            else
                ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(ProtoValue, irProgram, typeargs, "NULL"), "NULL");

            if (Type.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
                emitter.Append($", {responsibleDestroyer})");
            else
                emitter.Append(')');
        }
    }

    partial class AllocMemorySpan
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ProtoValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            emitter.Append($"buffer_alloc_{ElementType.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
            
            if (ProtoValue.RequiresDisposal(typeargs, false))
                ProtoValue.Emit(irProgram, emitter, typeargs, "NULL", false);
            else
                ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(ProtoValue, irProgram, typeargs, "NULL"), "NULL");
            emitter.Append($", {Length}");

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

        public override void EmitCall(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, SortedSet<int> releasedArguments, int currentNestedCall, string responsibleDestroyer)
        {
            RecordType recordType = (RecordType)RecordPrototype.SubstituteWithTypearg(typeargs);
            emitter.Append($"construct_{recordType.GetOriginalStandardIdentifer(irProgram)}(");
            EmitArguments(irProgram, emitter, typeargs, releasedArguments, currentNestedCall);
            if (Arguments.Count > 0)
                emitter.Append(", ");
            emitter.Append($"(_nhp_std_record_mask_t*){responsibleDestroyer})");
        }
    }
}
