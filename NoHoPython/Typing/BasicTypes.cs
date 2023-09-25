using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;

namespace NoHoPython.Typing
{
    public abstract partial class Primitive : IType
    {
        public static readonly IntegerType Integer = new();
        public static readonly DecimalType Decimal = new();
        public static readonly CharacterType Character = new();
        public static readonly BooleanType Boolean = new();

        public static readonly HandleType Handle = new(new NothingType());
        public static readonly HandleType CString = new(new CharacterType());
        public static readonly NothingType Nothing = new(); //not a primitive but also commonly used

        public static RecordType GetStringType(AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (irBuilder.SymbolMarshaller.FindSymbol("string", errorReportedElement) is IntermediateRepresentation.Statements.RecordDeclaration recordDeclaration)
                return new RecordType(recordDeclaration, new(), errorReportedElement);
            throw new UnexpectedStringSymbolException(errorReportedElement);
        }

        public abstract string TypeName { get; }
        public string Identifier => TypeName;
        public bool IsEmpty => false;

        public abstract int Id { get; }

        public abstract IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder);

        public abstract bool IsCompatibleWith(IType type);
        public abstract IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeArgs);

        public virtual void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, IAstElement errorReportedElement)
        {
            if (!IsCompatibleWith(argument))
                throw new UnexpectedTypeException(this, errorReportedElement);
        }

        public virtual IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder) => ArithmeticCast.CastTo(argument, this, irBuilder);

        public override string ToString() => TypeName;
        public override int GetHashCode() => Id;
    }

    public sealed partial class IntegerType : Primitive
    {
        public override string TypeName => "int";
        public override int Id => 0;

        public override IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new IntegerLiteral(0, errorReportedElement);

        public override bool IsCompatibleWith(IType type)
        {
            return type is IntegerType;
        }
    }

    public sealed partial class DecimalType : Primitive
    {
        public override string TypeName => "dec";
        public override int Id => 1;

        public override IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new DecimalLiteral(0, errorReportedElement);

        public override bool IsCompatibleWith(IType type)
        {
            return type is DecimalType;
        }
    }

    public sealed partial class CharacterType : Primitive
    {
        public override string TypeName { get => "char"; }
        public override int Id => 2;

        public override IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new CharacterLiteral('\0', errorReportedElement);

        public override bool IsCompatibleWith(IType type)
        {
            return type is CharacterType;
        }
    }

    public sealed partial class BooleanType : Primitive
    {
        public override string TypeName => "bool";
        public override int Id => 3;

        public override IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new FalseLiteral(errorReportedElement);

        public override bool IsCompatibleWith(IType type)
        {
            return type is BooleanType;
        }
    }

    public sealed partial class HandleType : Primitive
    {
        public override string TypeName => $"handle<{ValueType.TypeName}>";
        public override int Id => 4;

        public override IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public IType ValueType { get; private set; }

        public HandleType(IType valueType)
        {
            ValueType = valueType;
        }

        public override bool IsCompatibleWith(IType type) => type is HandleType handleType && ValueType.IsCompatibleWith(handleType.ValueType);
    }

    public sealed partial class NothingType : IType
    {
        public string TypeName => "nothing";
        public string Identifier => "nothing";
        public bool IsEmpty => true;

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new EmptyTypeLiteral(Primitive.Nothing, errorReportedElement);

        public bool IsCompatibleWith(IType type) => type is NothingType;

        public override string ToString() => TypeName;
    }

    public sealed partial class ArrayType : IType
    {
        public string TypeName => $"array<{ElementType.TypeName}>";
        public string Identifier => $"array_{ElementType.Identifier}";
        public bool IsEmpty => false;

        public IType ElementType { get; private set; }

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new ArrayLiteral(ElementType, new(), irBuilder, errorReportedElement);

        public ArrayType(IType elementType)
        {
            ElementType = elementType;
        }

        public bool IsCompatibleWith(IType type) => type is ArrayType arrayType && ElementType.IsCompatibleWith(arrayType.ElementType);
    }

    public sealed partial class MemorySpan : IType
    {
        public string TypeName => $"span<{ElementType.TypeName}, {Length}>";
        public string Identifier => $"span_{ElementType.Identifier}_of_{Length}";
        public bool IsEmpty => false;

        public IType ElementType { get; private set; }
        public int Length { get; private set; }

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new ArrayLiteral(ElementType, new(), irBuilder, errorReportedElement);

        public MemorySpan(IType elementType, int length)
        {
            ElementType = elementType;
            Length = length;
        }

        public bool IsCompatibleWith(IType type) => type is MemorySpan memorySpan && ElementType.IsCompatibleWith(memorySpan.ElementType) && memorySpan.Length == Length;
    }

    public sealed partial class ProcedureType : IType
    {
        public string TypeName => $"fn<{ReturnType.TypeName}, {string.Join(", ", ParameterTypes.ConvertAll((type) => type.TypeName))}>";
        public string Identifier => $"proc_{string.Join(string.Empty, ParameterTypes.ConvertAll((type) => $"{type.Identifier}_"))}ret_{ReturnType.Identifier}";
        public bool IsEmpty => false;

        public IType ReturnType { get; private set; }
        public readonly List<IType> ParameterTypes;

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public ProcedureType(IType returnType, List<IType> parameterTypes)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is ProcedureType procedureType)
            {
                if (ParameterTypes.Count != procedureType.ParameterTypes.Count)
                    return false;
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