using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using System.Text;

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

        public static IType GetStringType(AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            try 
            {
                AstType standardStringType = new AstType("string", new());
                return standardStringType.ToIRType(irBuilder, errorReportedElement);
            }
            catch (SymbolNotFoundException)
            {
                return new ArrayType(Character);
            }
        }

        public abstract string TypeName { get; }
        public string Identifier => TypeName;
        public string PrototypeIdentifier => Identifier;
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
        public string PrototypeIdentifier => "nothing";
        public bool IsEmpty => true;

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new EmptyTypeLiteral(Primitive.Nothing, errorReportedElement);

        public bool IsCompatibleWith(IType type) => type is NothingType;

        public override string ToString() => TypeName;
    }

    public sealed partial class ArrayType : IType
    {
        public string TypeName => $"array<{ElementType.TypeName}>";
        public string Identifier => $"array_{ElementType.Identifier}";
        public string PrototypeIdentifier => $"array_T";
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
        public string PrototypeIdentifier => $"span_T_of_{Length}";
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
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        private static string GetPurityName(Purity purity) => purity switch
        {
            Purity.Pure => "pure",
            Purity.OnlyAffectsArguments => "fn",
            Purity.OnlyAffectsArgumentsAndCaptured => "affectsArgsOnly"
        };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

        public string TypeName => $"{GetPurityName(Purity)}<{ReturnType.TypeName}{string.Join(", ", ParameterTypes.ConvertAll((type) => type.TypeName))}>";

        public string Identifier
        {
            get
            {
                List<IType> typeargs = new(ParameterTypes);
                typeargs.Insert(0, ReturnType);
                return IType.GetIdentifier("fn", typeargs.ToArray());
            }
        
        }
        public string PrototypeIdentifier
        {
            get
            {
                StringBuilder builder = new();
                builder.Append("fn_");
                builder.Append(ReturnType is NothingType ? "None" : "RT");
                for (int i = 0; i < ParameterTypes.Count; i++)
                    builder.Append("_T");
                return builder.ToString();
            }
        }

        public bool IsEmpty => false;

        public IType ReturnType { get; private set; }
        public readonly List<IType> ParameterTypes;
        public Purity Purity { get; private set; }

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => throw new NoDefaultValueError(this, errorReportedElement);

        public ProcedureType(IType returnType, List<IType> parameterTypes, Purity purity)
        {
            ReturnType = returnType;
            ParameterTypes = parameterTypes;
            Purity = purity;
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is ProcedureType procedureType)
            {
                if (Purity != procedureType.Purity)
                    return false;
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