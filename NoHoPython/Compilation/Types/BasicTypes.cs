using NoHoPython.IntermediateRepresentation;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public static void EmitMove(IType type, IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (!irProgram.EmitExpressionStatements)
                throw new CannotEmitDestructorError(null);
            
            emitter.Append($"({{{type.GetCName(irProgram)} _nhp_es_move_temp = {destC}; {destC} = {valueCSource}; ");
            type.EmitFreeValue(irProgram, emitter, "_nhp_es_move_temp", childAgent);
            emitter.Append($" {destC};}})");
        }
    }

    partial class Primitive
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => false;
        public bool MustSetResponsibleDestroyer => false;

        public abstract string GetCName(IRProgram irProgram);
        public string GetStandardIdentifier(IRProgram irProgram) => TypeName;

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) { }
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => emitter.Append(valueCSource);
        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent) => emitter.Append($"({destC} = {valueCSource})");
        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter) { }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }

    partial class IntegerType
    {
        public override string GetCName(IRProgram irProgram) => "long";
    }

    partial class DecimalType
    {
        public override string GetCName(IRProgram irProgram) => "double";
    }

    partial class CharacterType
    {
        public override string GetCName(IRProgram irProgram) => "char";
    }

    partial class BooleanType
    {
        public override string GetCName(IRProgram irProgram) => "int";
    }

    partial class HandleType
    {
        public override string GetCName(IRProgram irProgram) => "void*";
    }

    partial class NothingType
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => false;
        public bool MustSetResponsibleDestroyer => false;

        public string GetCName(IRProgram irProgram) => "void";
        public string GetStandardIdentifier(IRProgram irProgram) => "nothing";

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string recordCSource) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }
}
