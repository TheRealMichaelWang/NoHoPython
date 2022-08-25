using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.Typing
{
    public sealed class TypeParameter : IScopeSymbol
    {
        public static void ValidateTypeArguments(List<TypeParameter> typeParameters, List<IType> typeArguments)
        {
            if (typeArguments.Count != typeParameters.Count)
                throw new UnexpectedTypeArgumentsException(typeParameters.Count, typeArguments.Count);
            for (int i = 0; i < typeArguments.Count; i++)
                if (!typeParameters[i].SupportsType(typeArguments[i]))
                    throw new UnexpectedTypeException(new TypeParameterReference(typeParameters[i]), typeArguments[i]);
        }

        public static string GetMangledTypeArgumentNames(List<IType> typeArguments) => string.Join('_', typeArguments.Select((IType type) => type.TypeName));

        public bool IsGloballyNavigable => false;

        public string Name { get;private set; }
        public readonly List<IType> RequiredSupportedTypes;

        public TypeParameter(string name, List<IType> requiredSupportedTypes)
        {
            Name = name;
            RequiredSupportedTypes = requiredSupportedTypes;
        }

        public bool SupportsType(IType type)
        {
            foreach (IType requiredSupportedType in RequiredSupportedTypes)
                if (!requiredSupportedType.IsCompatibleWith(type))
                    return false;
            return true;
        }
    }

    partial class TypeParameterReference
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => typeargs[TypeParameter].Clone();
    }

    partial class ArrayType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayType(ElementType.SubstituteWithTypearg(typeargs));
    }

    partial class BooleanType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new BooleanType();
    }

    partial class CharacterType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new CharacterType();
    }

    partial class DecimalType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new DecimalType();
    }

    partial class IntegerType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IntegerType();
    }

    partial class EnumType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new EnumType(EnumDeclaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList()); 
    }

    partial class RecordType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new RecordType(RecordPrototype, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());
    }

    partial class InterfaceType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new InterfaceType(InterfaceDeclaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());
    }

    partial class ProcedureType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ProcedureType(ReturnType.SubstituteWithTypearg(typeargs), ParameterTypes.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IntegerLiteral(Number);
    }

    partial class DecimalLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new DecimalLiteral(Number);
    }

    partial class CharacterLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new CharacterLiteral(Character);
    }

    partial class ArrayLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayLiteral(ElementType.SubstituteWithTypearg(typeargs), Elements.Select((IRValue element) => element.SubstituteWithTypearg(typeargs)).ToList());
    }

    partial class AllocArray
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocArray(ElementType.SubstituteWithTypearg(typeargs), Size.SubstituteWithTypearg(typeargs));
    }

    partial class AllocRecord
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocRecord((RecordType)RecordPrototype.SubstituteWithTypearg(typeargs), ConstructorArguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    partial class ArithmeticCast
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArithmeticCast(Operation, Input.SubstituteWithTypearg(typeargs));
    }

    partial class ArithmeticOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArithmeticOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs));
    }

    partial class ComparativeOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ComparativeOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs));
    }

    partial class LogicalOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LogicalOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs));
    }

    partial class GetValueAtIndex
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new GetValueAtIndex(Array.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs));
    } 

    partial class SetValueAtIndex
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetValueAtIndex(Array.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs));
    }

    partial class GetPropertyValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new GetPropertyValue(Record.SubstituteWithTypearg(typeargs), Property.SubstituteWithTypearg(typeargs));
    }

    partial class SetPropertyValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetPropertyValue(Record.SubstituteWithTypearg(typeargs), Property.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs));
    }
}