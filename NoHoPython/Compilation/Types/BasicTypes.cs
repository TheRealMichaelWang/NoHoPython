using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NoHoPython.IntermediateRepresentation;

namespace NoHoPython.Typing
{
    partial class Primitive
    {
        public bool RequiresDisposal => false;

        public abstract string GetCName();
        public string GetStandardIdentifier() => TypeName;

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) { }
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append(valueCSource);
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"({destC} = {valueCSource})");

        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => EmitCopyValue(emitter, valueCSource);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(emitter, valueCSource);

        public void ScopeForUsedTypes() { }
    }

    partial class IntegerType
    {
        public override string GetCName() => "long";
    }

    partial class DecimalType
    {
        public override string GetCName() => "double";
    }

    partial class CharacterType
    {
        public override string GetCName() => "char";
    }

    partial class BooleanType
    {
        public override string GetCName() => "int";
    }

    partial class NothingType
    {
        public bool RequiresDisposal => false;

        public string GetCName() => "void";
        public string GetStandardIdentifier() => "nothing";

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => throw new CannotCompileNothingError(null);
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => throw new CannotCompileNothingError(null);
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => throw new CannotCompileNothingError(null);
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => throw new CannotCompileNothingError(null);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => throw new CannotCompileNothingError(null);

        public void ScopeForUsedTypes() { }
    }
}
