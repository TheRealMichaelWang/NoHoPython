using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class SizeofOperator
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => TypeToMeasure.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer) => emitter.Append($"sizeof({TypeToMeasure.SubstituteWithTypearg(typeargs).GetCName(irProgram)})");
    }

    partial class MemoryGet
    {
        public override void EmitExpression(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string leftCSource, string rightCSource) => emitter.Append($"(({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*){leftCSource})[{rightCSource}]");
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            emitter.Append($"((({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
            IRValue.EmitMemorySafe(Address, irProgram, emitter, typeargs);
            emitter.Append(")[");
            IRValue.EmitMemorySafe(Index, irProgram, emitter, typeargs);
            emitter.Append("] = ");

            StringBuilder responsibleDestroyerBuilder = new();
            if (ResponsibleDestroyer != null)
            {
                IRValue.EmitMemorySafe(ResponsibleDestroyer, irProgram, responsibleDestroyerBuilder, typeargs);
                if (ResponsibleDestroyer.Type is ArrayType)
                    responsibleDestroyerBuilder.Append(".responsible_destroyer");
                else if (ResponsibleDestroyer.Type is RecordType)
                    responsibleDestroyerBuilder.Append("->_nhp_responsible_destroyer");
            }
            else
                IRValue.EmitMemorySafe(Address, irProgram, responsibleDestroyerBuilder, typeargs);

            if (Value.RequiresDisposal(typeargs))
                Value.Emit(irProgram, emitter, typeargs, responsibleDestroyer.ToString());
            else
            {
                StringBuilder valueBuilder = new();
                Value.Emit(irProgram, valueBuilder, typeargs, "NULL");
                Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, valueBuilder.ToString(), responsibleDestroyerBuilder.ToString());
            }
            emitter.Append(')');
        }
        
        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            Emit(irProgram, emitter, typeargs, "NULL");
            emitter.AppendLine(";");
        }
    }

    partial class MarshalIntoArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            ElementType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Length.ScopeForUsedTypes(typeargs, irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            ArrayType type = new(ElementType.SubstituteWithTypearg(typeargs));
            if(ElementType.SubstituteWithTypearg(typeargs).RequiresDisposal)
                emitter.Append($"marshal_foreign{type.GetStandardIdentifier(irProgram)}(");
            else
                emitter.Append($"marshal{type.GetStandardIdentifier(irProgram)}(");
            
            IRValue.EmitMemorySafe(Address, irProgram, emitter, typeargs);
            emitter.Append(", ");
            IRValue.EmitMemorySafe(Length, irProgram, emitter, typeargs);
            emitter.Append($", {responsibleDestroyer})");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class MemoryDestroy
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Address.ScopeForUsedTypes(typeargs, irBuilder);
            if (Index != null)
                Index.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (Type.SubstituteWithTypearg(typeargs).RequiresDisposal)
            {
                CodeBlock.CIndent(emitter, indent);
                StringBuilder valueBuilder = new();
                valueBuilder.Append($"(*(({Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)}*)");
                IRValue.EmitMemorySafe(Address, irProgram, valueBuilder, typeargs);
                if (Index != null)
                {
                    valueBuilder.Append(" + ");
                    IRValue.EmitMemorySafe(Index, irProgram, valueBuilder, typeargs);
                }
                valueBuilder.Append("))");
                Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, valueBuilder.ToString(), "NULL");
            }
        }
    }
}