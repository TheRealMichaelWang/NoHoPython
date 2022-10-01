namespace NoHoPython.Typing
{
    partial class ArrayType
    {
        public IType Clone() => new ArrayType(ElementType);
    }

    partial class BooleanType
    {
        public override IType Clone() => new BooleanType();
    }

    partial class CharacterType
    {
        public override IType Clone() => new CharacterType();
    }

    partial class IntegerType
    {
        public override IType Clone() => new IntegerType();
    }

    partial class DecimalType
    {
        public override IType Clone() => new DecimalType();
    }

    partial class NothingType
    {
        public IType Clone() => new NothingType();
    }

    partial class ProcedureType
    {
        public IType Clone() => new ProcedureType(ReturnType.Clone(), ParameterTypes.Select((IType type) => type.Clone()).ToList());
    }

    partial class TypeParameterReference
    {
        public IType Clone() => new TypeParameterReference(TypeParameter);
    }

    partial class EnumType
    {
        public IType Clone() => new EnumType(EnumDeclaration, TypeArguments.Select((IType type) => type.Clone()).ToList());
    }

    partial class RecordType
    {
        public IType Clone() => new RecordType(RecordPrototype, TypeArguments.Select((IType type) => type.Clone()).ToList());
    }

    partial class InterfaceType
    {
        public IType Clone() => new InterfaceType(InterfaceDeclaration, TypeArguments.Select((IType type) => type.Clone()).ToList());
    }

    partial class HandleType
    {
        public IType Clone() => new HandleType();
    }
}
