using NoHoPython.IntermediateRepresentation;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public static void EmitMoveExpressionStatement(IType type, IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource)
        {
            Debug.Assert(irProgram.EmitExpressionStatements);

            emitter.Append($"({{{type.GetCName(irProgram)} _nhp_es_move_temp = {destC}; {destC} = {valueCSource}; ");
            type.EmitFreeValue(irProgram, emitter, "_nhp_es_move_temp", "NULL");
            emitter.Append($" {destC};}})");
        }
    }

    partial class Primitive
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => false;
        public bool HasResponsibleDestroyer => false;

        public abstract string GetCName(IRProgram irProgram);
        public string GetStandardIdentifier(IRProgram irProgram) => TypeName;

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string childAgent) { }
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => emitter.Append(valueCSource);
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"({destC} = {valueCSource})");
        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter) { }

        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, $"{recordCSource}->_nhp_responsible_destroyer");

        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => emitter.Append(valueCSource);

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
        public bool HasResponsibleDestroyer => false;

        public string GetCName(IRProgram irProgram) => "void";
        public string GetStandardIdentifier(IRProgram irProgram) => "nothing";

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => throw new CannotCompileEmptyTypeError(null);
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => throw new CannotCompileEmptyTypeError(null);
        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, StringBuilder emitter, string valueCSource, string newResponsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }
}
