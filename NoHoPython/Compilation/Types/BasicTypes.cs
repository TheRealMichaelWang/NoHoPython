using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInformation);

        public string GetCName(IRProgram irProgram);
        public string GetStandardIdentifier(IRProgram irProgram);
        public string? GetInvalidState();
        public Emitter.SetPromise? IsInvalid(Emitter emitter); //only return non-null for types that have invalid states

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent);
        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement);
        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord);

        public void EmitCStruct(IRProgram irProgram, Emitter emitter);
    }

    partial class Primitive
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => false;
        public bool MustSetResponsibleDestroyer => false;
        public bool IsTypeDependency => false;

        public virtual bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => false;

        public abstract string GetCName(IRProgram irProgram);
        public virtual string? GetInvalidState() => null;
        public virtual Emitter.SetPromise? IsInvalid(Emitter emitter) => null;
        public virtual string GetStandardIdentifier(IRProgram irProgram) => TypeName;

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent) { }
        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement) => valueCSource(emitter);
        public void EmitCStruct(IRProgram irProgram, Emitter emitter) { }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer, null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord, null);

        public virtual void ScopeForUsedTypes(AstIRProgramBuilder irBuilder) { }
    }

    partial class IntegerType
    {
        public override string GetCName(IRProgram irProgram) => "long";
    }

    partial class DecimalType
    {
        public override string GetCName(IRProgram irProgram) => "double";

        public override string? GetInvalidState() => "NAN";

        public override Emitter.SetPromise? IsInvalid(Emitter emitter) => promise =>
        {
            emitter.Append("isnan(");
            promise(emitter);
            emitter.Append(")");
        };
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
        public override bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => ValueType.TypeParameterAffectsCodegen(effectInfo);

        public override void ScopeForUsedTypes(AstIRProgramBuilder irBuilder) => ValueType.ScopeForUsedTypes(irBuilder);

        public override string GetStandardIdentifier(IRProgram irProgram) => $"handle_{ValueType.GetCName(irProgram)}";

        public override string GetCName(IRProgram irProgram) => ValueType is NothingType ? "void*" : $"{ValueType.GetCName(irProgram)}*";
    }

    partial class NothingType
    {
        public bool IsNativeCType => true;
        public bool RequiresDisposal => false;
        public bool MustSetResponsibleDestroyer => false;
        public bool IsTypeDependency => false;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => false;

        public string GetCName(IRProgram irProgram) => "void";
        public string? GetInvalidState() => null;
        public Emitter.SetPromise? IsInvalid(Emitter emitter) => null;
        public string GetStandardIdentifier(IRProgram irProgram) => "nothing";

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)=> throw new CannotCompileEmptyTypeError(null);
        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCStruct(IRProgram irProgram, Emitter emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }
}
