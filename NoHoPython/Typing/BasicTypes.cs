using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using System.Text;

namespace NoHoPython.Typing
{
    public abstract class Primitive : IType
    {
        public static readonly IntegerType Integer = new();
        public static readonly DecimalType Decimal = new();
        public static readonly CharacterType Character = new();
        public static readonly BooleanType Boolean = new();

        public static readonly NothingType Nothing = new(); //not a primitive but also commonly used

        public abstract string TypeName { get; }
        public abstract int Size { get; }

        public abstract int Id { get; }

        public abstract string GetCName();
        public abstract bool IsCompatibleWith(IType type);
        public abstract IType Clone();
        public abstract IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeArgs);

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, IAstElement errorReportedElement)
        {
            if (!IsCompatibleWith(argument))
                throw new UnexpectedTypeException(this, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument) => ArithmeticCast.CastTo(argument, this);

        public override string ToString() => TypeName;
        public override int GetHashCode() => Id;
    }

    public sealed partial class IntegerType : Primitive
    {
        public override string TypeName => "int";
        public override int Size => 8;
        public override int Id => 0;

        public override string GetCName() => "long";

        public override bool IsCompatibleWith(IType type)
        {
            return type is IntegerType;
        }
    }

    public sealed partial class DecimalType : Primitive
    {
        public override string TypeName => "dec";
        public override int Size => 8;
        public override int Id => 1;

        public override string GetCName() => "double";

        public override bool IsCompatibleWith(IType type)
        {
            return type is DecimalType;
        }
    }

    public sealed partial class CharacterType : Primitive
    {
        public override string TypeName { get => "char"; }
        public override int Size => 1;
        public override int Id => 2;

        public override string GetCName() => "char";

        public override bool IsCompatibleWith(IType type)
        {
            return type is CharacterType;
        }
    }

    public sealed partial class BooleanType : Primitive
    {
        public override string TypeName { get => "bool"; }
        public override int Size => 4;
        public override int Id => 3;

        public override string GetCName() => "int";

        public override bool IsCompatibleWith(IType type)
        {
            return type is BooleanType;
        }
    }

    public sealed partial class NothingType : IType
    {
        public string TypeName => "nothing";
        public string GetCName() => "void";

        public bool IsCompatibleWith(IType type)
        {
            return type is NothingType;
        }

        public override string ToString() => TypeName;
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class ArrayType : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName => $"{ElementType.TypeName}[]";

        public IType ElementType { get; private set; }

        public string GetCName() => $"{ElementType.GetCName()}*";

        public ArrayType(IType elementType)
        {
            ElementType = elementType;
        }

        public bool IsCompatibleWith(IType type)
        {
            return type is ArrayType arrayType && ElementType.IsCompatibleWith(arrayType.ElementType);
        }
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class ProcedureType : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName => $"({string.Join(", ", ParameterTypes)}) => {ReturnType}";

        public IType ReturnType { get; private set; }
        public readonly List<IType> ParameterTypes;

        public ProcedureType(IType returnType, List<IType> parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
            cName = new Lazy<string>(() => defineCType(this));
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is ProcedureType procedureType)
            {
                if (!ReturnType.IsCompatibleWith(procedureType.ReturnType))
                    return false;
                for (int i = 0; i < ParameterTypes.Count; i++)
                    if (!procedureType.ParameterTypes[i].IsCompatibleWith(ParameterTypes[i]))
                        return false;
                return true;
            }
            return false;
        }
    }
}