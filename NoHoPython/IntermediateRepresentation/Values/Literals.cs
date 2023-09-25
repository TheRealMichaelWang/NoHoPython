using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IntegerLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => Primitive.Integer; }
        public bool IsTruey => Number != 0;
        public bool IsFalsey => Number == 0;

        public long Number { get; private set; }

        public IntegerLiteral(long number, IAstElement errorReportedElement)
        {
            Number = number;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class DecimalLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => Primitive.Decimal; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public decimal Number { get; private set; }

        public DecimalLiteral(decimal number, IAstElement errorReportedElement)
        {
            Number = number;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class CharacterLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => Primitive.Character;
        public bool IsTruey => Character == '\0';
        public bool IsFalsey => Character == '\0';

        public char Character { get; private set; }

        public CharacterLiteral(char character, IAstElement errorReportedElement)
        {
            Character = character;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class TrueLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public bool IsTruey => true;
        public bool IsFalsey => false;

        public TrueLiteral(IAstElement errorReportedElement) => ErrorReportedElement = errorReportedElement;

        public IType Type => Primitive.Boolean;
    }

    public sealed partial class FalseLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => true;

        public IType Type => Primitive.Boolean;

        public FalseLiteral(IAstElement errorReportedElement) => ErrorReportedElement = errorReportedElement;
    }

    public sealed partial class NullPointerLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => true;

        public IType Type { get; private set; }

        public NullPointerLiteral(IType expectedType, IAstElement errorReportedElement)
        {
            if (!(expectedType is HandleType || (expectedType is ForeignCType foreignType && foreignType.Declaration.PointerPropertyAccess)))
                throw new UnexpectedTypeException(expectedType, errorReportedElement);

            Type = expectedType;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class StaticCStringLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IType Type => Primitive.CString;
        
        public string String { get; private set; }

        public StaticCStringLiteral(string @string, IAstElement errorReportedElement)
        {
            String = @string;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class EmptyTypeLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IType Type { get; private set; }

        public EmptyTypeLiteral(IType type, IAstElement errorReportedElement)
        {
            Type = type;
            if (!Type.IsEmpty)
                throw new UnexpectedTypeException(Type, errorReportedElement);
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class ArrayLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => new MemorySpan(ElementType, Elements.Count); }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IType ElementType { get; private set; }

        public readonly List<IRValue> Elements;

        public ArrayLiteral(IType elementType, List<IRValue> elements, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ElementType = elementType;
            Elements = elements;
            ErrorReportedElement = errorReportedElement;

            for (int i = 0; i < elements.Count; i++)
                elements[i] = ArithmeticCast.CastTo(elements[i], ElementType, irBuilder);
        }

        private ArrayLiteral(IType elementType, List<IRValue> elements, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            ElementType = elementType;
            Elements = elements;
        }

        public ArrayLiteral(List<IRValue> elements, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Elements = new List<IRValue>(elements.Count);
            ErrorReportedElement = errorReportedElement;

            bool CanBeElementType(IType type)
            {
                try
                {
                    for (int i = 0; i < elements.Count; i++)
                        Elements.Add(ArithmeticCast.CastTo(elements[i], type, irBuilder));
                    return true;
                }
                catch (UnexpectedTypeException)
                {
                    return false;
                }
            }

            foreach(IRValue element in elements)
                if (CanBeElementType(element.Type))
                {
                    ElementType = element.Type;
                    return;
                }

            throw new UnexpectedTypeException(Primitive.Nothing, errorReportedElement);
        }
    }

    public sealed partial class TupleLiteral : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TupleType;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public TupleType TupleType => new TupleType(Elements.ConvertAll((elem) => elem.Type));

        public readonly List<IRValue> Elements;

        public TupleLiteral(List<IRValue> tupleElements, IAstElement errorReportedElement)
        {
            Elements = tupleElements;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class InterpolatedString : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TargetArrayChar ? new ArrayType(Primitive.Character) : Primitive.CString;

        public bool IsTruey => false;
        public bool IsFalsey => false;

        public readonly List<object> InterpolatedValues; //all objects are either IRValue or string

        private bool TargetArrayChar;

        public InterpolatedString(List<object> interpolatedValues, bool targetArrayChar, IAstElement errorReportedElement)
        {
            InterpolatedValues = new(interpolatedValues.Count);
            ErrorReportedElement = errorReportedElement;
            TargetArrayChar = targetArrayChar;

            for (int i = 0; i < interpolatedValues.Count; i++)
            {
                if (interpolatedValues[i] is IRValue irValue)
                    InterpolatedValues.Add(irValue);
                else if (interpolatedValues[i] is string str)
                {
                    if (str != string.Empty)
                        InterpolatedValues.Add(str);
                }
                else
                    throw new InvalidOperationException();
            }
        }
    }

    public sealed partial class AllocArray : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get => new ArrayType(ElementType); }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IType ElementType { get; private set; }
        public IRValue Length { get; private set; }
        public IRValue ProtoValue { get; private set; }

        public AllocArray(IType elementType, IRValue length, IRValue protoValue, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            ElementType = elementType;
            Length = ArithmeticCast.CastTo(length, Primitive.Integer, irBuilder);
            ProtoValue = ArithmeticCast.CastTo(protoValue, ElementType, irBuilder);
        }

        public AllocArray(IType elementType, IRValue length, IRValue protoValue, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            ElementType = elementType;
            Length = length;
            ProtoValue = protoValue;
        }
    }

    public sealed partial class AllocMemorySpan : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get => new MemorySpan(ElementType, Length); }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IType ElementType { get; private set; }
        public int Length { get; private set; }
        public IRValue ProtoValue { get; private set; }

        public AllocMemorySpan(IType elementType, int length, IRValue protoValue, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            ElementType = elementType;
            Length = length;
            ProtoValue = protoValue;
        }
    }

    public sealed partial class AllocRecord : ProcedureCall
    {
        public override IType Type { get => RecordPrototype; }

        public RecordType RecordPrototype { get; private set; }

        public AllocRecord(RecordType recordPrototype, List<IRValue> constructorArguments, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(recordPrototype.GetConstructorParameterTypes(), constructorArguments, irBuilder, errorReportedElement)
        {
            RecordPrototype = recordPrototype;
        }

        private AllocRecord(RecordType recordPrototype, List<IRValue> constructorArguments, IAstElement errorReportedElement) : base(constructorArguments, errorReportedElement)
        {
            RecordPrototype = recordPrototype;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class IntegerLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.IntegerLiteral(Number, this);
    }

    partial class DecimalLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.DecimalLiteral(Number, this);
    }

    partial class TrueLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.TrueLiteral(this);
    }

    partial class FalseLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.FalseLiteral(this);
    }

    partial class CharacterLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.CharacterLiteral(Character, this);
    }

    partial class NothingLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.EmptyTypeLiteral(Primitive.Nothing, this);
    }

    partial class NullPointerLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.NullPointerLiteral(expectedType ?? Primitive.Handle, this);
    }

    partial class ArrayLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IType? inferedElementType = AnnotatedElementType != null
                ? AnnotatedElementType.ToIRType(irBuilder, this)
                : expectedType != null && expectedType is ArrayType arrayType
                ? arrayType.ElementType
                : null;

            List<IRValue> elements = Elements.ConvertAll((IAstValue element) => element.GenerateIntermediateRepresentationForValue(irBuilder, inferedElementType, willRevaluate));
            if (inferedElementType != null)
                return new IntermediateRepresentation.Values.ArrayLiteral(inferedElementType, elements, irBuilder, this);
            else
                return new IntermediateRepresentation.Values.ArrayLiteral(elements, irBuilder, this);
        }
    }

    partial class StringLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            if((expectedType is ArrayType arrayType && arrayType.ElementType is CharacterType) || (expectedType is MemorySpan spanType && spanType.ElementType is CharacterType)) //return array of string
            {
                return new IntermediateRepresentation.Values.ArrayLiteral(Primitive.Character, String.ToList().ConvertAll((c) => (IRValue)new IntermediateRepresentation.Values.CharacterLiteral(c, this)), irBuilder, this);
            }
            else if(expectedType is HandleType)
                return new IntermediateRepresentation.Values.StaticCStringLiteral(String, this);

            return new IntermediateRepresentation.Values.AllocRecord(Primitive.GetStringType(irBuilder, this), new List<IRValue>() { new IntermediateRepresentation.Values.StaticCStringLiteral(String, this), new IntermediateRepresentation.Values.TrueLiteral(this) }, irBuilder, this);
        }
    }

    partial class InterpolatedString
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            List<object> IRInterpolatedValues = new();

            RecordType stringType = Primitive.GetStringType(irBuilder, this);
            foreach (object value in InterpolatedValues)
            {
                if (value is IAstValue astValue)
                {
                    IRValue irValue = astValue.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);

                    if (!irValue.Type.HasFormatSpecifier)
                    {
                        try
                        {
                            irValue = IntermediateRepresentation.Values.ArithmeticCast.CastTo(irValue, stringType, irBuilder);
                        }
                        catch
                        {
                            irValue = IntermediateRepresentation.Values.ArithmeticCast.CastTo(irValue, new ArrayType(Primitive.Character), irBuilder);
                        }
                    }

                    IRInterpolatedValues.Add(irValue);
                }
                else
#pragma warning disable CS8604 // Possible null reference argument.
                    IRInterpolatedValues.Add(value as string);
#pragma warning restore CS8604 // Possible null reference argument.
            }

            if (expectedType is ArrayType arrayType && arrayType.ElementType is CharacterType)
                return new IntermediateRepresentation.Values.InterpolatedString(IRInterpolatedValues, true, this);

            return new IntermediateRepresentation.Values.AllocRecord(Primitive.GetStringType(irBuilder, this), new List<IRValue>() { new IntermediateRepresentation.Values.InterpolatedString(IRInterpolatedValues, false, this), new IntermediateRepresentation.Values.FalseLiteral(this) }, irBuilder, this);
        }
    }

    partial class AllocArray
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IType elementType = ElementType != null 
                ? ElementType.ToIRType(irBuilder, this)
                : expectedType != null && expectedType is ArrayType expectedArrayType
                ? expectedArrayType.ElementType
                : throw new UnexpectedTypeException(expectedType ?? Primitive.Nothing, this);

            IRValue protoIRValue = ProtoValue == null ? elementType.GetDefaultValue(this, irBuilder) : ProtoValue.GenerateIntermediateRepresentationForValue(irBuilder, elementType, willRevaluate);

            if (Length is IntegerLiteral integerLiteral)
                return new IntermediateRepresentation.Values.AllocMemorySpan(elementType, (int)integerLiteral.Number, protoIRValue, this);

            return new IntermediateRepresentation.Values.AllocArray(elementType, Length.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, willRevaluate), protoIRValue, this);
        }
    }

    partial class InstantiateNewRecord
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IType prototype = RecordType != null
                ? RecordType.ToIRType(irBuilder, this)
                : expectedType != null && expectedType is RecordType expectedRecordType
                ? expectedRecordType
                : throw new UnexpectedTypeException(expectedType ?? Primitive.Nothing, this);

            return prototype is RecordType record
                ? (IRValue)new IntermediateRepresentation.Values.AllocRecord(record, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate)), irBuilder, this)
                : throw new UnexpectedTypeException(prototype, this);
        }
    }

    partial class FlagLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => irBuilder.Flags.Contains(Flag) ? new IntermediateRepresentation.Values.TrueLiteral(this) : new IntermediateRepresentation.Values.FalseLiteral(this); 
    }
}