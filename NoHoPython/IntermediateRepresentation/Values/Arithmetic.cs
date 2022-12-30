using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class ArithmeticCast : IRValue
    {
        public enum ArithmeticCastOperation
        {
            DecimalToInt,
            CharToInt,
            BooleanToInt,
            HandleToInt,
            IntToDecimal,
            IntToChar,
            IntToBoolean,
            IntToHandle
        }

        private static Dictionary<ArithmeticCastOperation, IType> outputTypes = new()
        {
            {ArithmeticCastOperation.DecimalToInt, new IntegerType() },
            {ArithmeticCastOperation.CharToInt, new IntegerType() },
            {ArithmeticCastOperation.BooleanToInt, new IntegerType() },
            {ArithmeticCastOperation.HandleToInt, new IntegerType() },
            {ArithmeticCastOperation.IntToDecimal, new DecimalType() },
            {ArithmeticCastOperation.IntToChar, new CharacterType() },
            {ArithmeticCastOperation.IntToBoolean, new CharacterType() },
            {ArithmeticCastOperation.IntToHandle, new HandleType() }
        };

        private static Dictionary<ArithmeticCastOperation, IType> inputTypes = new()
        {
            {ArithmeticCastOperation.IntToDecimal, new IntegerType() },
            {ArithmeticCastOperation.IntToChar, new IntegerType() },
            {ArithmeticCastOperation.IntToBoolean, new IntegerType() },
            {ArithmeticCastOperation.IntToHandle, new IntegerType() },
            {ArithmeticCastOperation.DecimalToInt, new DecimalType() },
            {ArithmeticCastOperation.CharToInt, new CharacterType() },
            {ArithmeticCastOperation.BooleanToInt, new BooleanType() },
            {ArithmeticCastOperation.HandleToInt, new HandleType() }
        };

        private static ArithmeticCastOperation[] toIntOperation = new ArithmeticCastOperation[]
        {
            ArithmeticCastOperation.DecimalToInt,
            ArithmeticCastOperation.CharToInt,
            ArithmeticCastOperation.BooleanToInt,
            ArithmeticCastOperation.HandleToInt,
        };

        private static ArithmeticCastOperation[] intToOperation = new ArithmeticCastOperation[]
        {
            ArithmeticCastOperation.IntToDecimal,
            ArithmeticCastOperation.IntToChar,
            ArithmeticCastOperation.IntToBoolean,
            ArithmeticCastOperation.IntToHandle
        };

        public static IRValue CastTo(IRValue value, IType typeTarget)
        {
            if (typeTarget.IsCompatibleWith(value.Type))
                return value;
            else if (value.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty($"to_{typeTarget.Identifier}")) 
            {
                IRValue call = new AnonymousProcedureCall(new GetPropertyValue(value, $"to_{typeTarget.Identifier}", value.ErrorReportedElement), new List<IRValue>(), value.ErrorReportedElement);
                if (!call.Type.IsCompatibleWith(typeTarget))
                    throw new UnexpectedTypeException(typeTarget, call.Type, value.ErrorReportedElement);
                return call;
            }
            else if (typeTarget is HandleType handleType)
                return value.Type is ArrayType
                    ? new ArrayOperator(ArrayOperator.ArrayOperation.GetArrayHandle, value, value.ErrorReportedElement)
                    : value.Type is Primitive
                    ? PrimitiveCast(value, handleType)
                    : throw new UnexpectedTypeException(typeTarget, value.Type, value.ErrorReportedElement);
            else return typeTarget is EnumType enumType
                ? new MarshalIntoEnum(enumType, value, value.ErrorReportedElement)
                : typeTarget is InterfaceType interfaceType
                ? new MarshalIntoInterface(interfaceType, value, value.ErrorReportedElement)
                : typeTarget is RecordType recordType
                    ? new AllocRecord(recordType, new List<IRValue>() { value }, value.ErrorReportedElement)
                    : typeTarget is Primitive primitive
                        ? PrimitiveCast(value, primitive)
                        : throw new UnexpectedTypeException(typeTarget, value.Type, value.ErrorReportedElement);
        }

        private static IRValue PrimitiveCast(IRValue primitive, Primitive targetType)
        {
            if (primitive.Type is not Primitive)
                if (primitive.Type is ArrayType)
                    return targetType is IntegerType
                        ? new ArrayOperator(ArrayOperator.ArrayOperation.GetArrayLength, primitive, primitive.ErrorReportedElement)
                        : throw new UnexpectedTypeException(targetType, primitive.Type, primitive.ErrorReportedElement);
                else
                    throw new UnexpectedTypeException(primitive.Type, primitive.ErrorReportedElement);

            if (targetType.IsCompatibleWith(primitive.Type))
                throw new UnexpectedTypeException(primitive.Type, primitive.ErrorReportedElement);

            Primitive input = (Primitive)primitive.Type;
            return targetType is IntegerType
                ? new ArithmeticCast(toIntOperation[input.Id - 1], primitive, primitive.ErrorReportedElement)
                : primitive.Type is IntegerType
                ? new ArithmeticCast(intToOperation[targetType.Id - 1], primitive, primitive.ErrorReportedElement)
                : (IRValue)new ArithmeticCast(intToOperation[targetType.Id - 1], new ArithmeticCast(toIntOperation[input.Id - 1], primitive, primitive.ErrorReportedElement), primitive.ErrorReportedElement);
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => outputTypes[Operation];
        public bool IsTruey => Input.IsTruey;
        public bool IsFalsey => Input.IsFalsey;

        public ArithmeticCastOperation Operation { get; private set; }
        public IRValue Input { get; private set; }

        private ArithmeticCast(ArithmeticCastOperation operation, IRValue input, IAstElement errorReportedElement)
        {
            Operation = operation;
            Input = input;
            ErrorReportedElement = errorReportedElement;

            if (!inputTypes[Operation].IsCompatibleWith(input.Type))
                throw new UnexpectedTypeException(inputTypes[Operation], input.Type, errorReportedElement);
        }
    }

    public sealed partial class ArithmeticOperator : BinaryOperator
    {
        public enum ArithmeticOperation
        {
            Add = 0,
            Subtract = 1,
            Multiply = 2,
            Divide = 3,
            Modulo = 5,
            Exponentiate = 7
        }

        public static bool OperationIsCommunicative(ArithmeticOperation operation) => operation == ArithmeticOperation.Add || operation == ArithmeticOperation.Multiply;

        private static Dictionary<ArithmeticOperation, string> operatorOverloadIdentifiers = new()
        {
            {ArithmeticOperation.Add, "add"},
            {ArithmeticOperation.Subtract,"subtract" },
            {ArithmeticOperation.Divide,"divide" },
            {ArithmeticOperation.Multiply ,"multiply"},
            {ArithmeticOperation.Modulo ,"modulo"},
            {ArithmeticOperation.Exponentiate, "exponentiate"}
        };

        public static bool IsCommunicative(ArithmeticOperation operation) => ((int)operation % 2) == 0;

        public static IRValue ComposeArithmeticOperation(ArithmeticOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement)
        {
            return left.Type is IPropertyContainer leftContainer && leftContainer.HasProperty(operatorOverloadIdentifiers[operation])
                ? new AnonymousProcedureCall(new GetPropertyValue(left, operatorOverloadIdentifiers[operation], errorReportedElement), new List<IRValue>()
                {
                    right
                }, errorReportedElement)
                : OperationIsCommunicative(operation) && right.Type is IPropertyContainer rightContainer && rightContainer.HasProperty(operatorOverloadIdentifiers[operation])
                ? new AnonymousProcedureCall(new GetPropertyValue(right, operatorOverloadIdentifiers[operation], errorReportedElement), new List<IRValue>()
                {
                    left
                }, errorReportedElement)
                : (IRValue)new ArithmeticOperator(operation, left, right, errorReportedElement);
        }

        public ArithmeticOperation Operation { get; private set; }
        public override IType Type { get; }

        private ArithmeticOperator(ArithmeticOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(left, right, false, errorReportedElement)
        {
            Operation = operation;

            if (left.Type is DecimalType)
            {
                Type = Primitive.Decimal;
                Right = ArithmeticCast.CastTo(right, Primitive.Decimal);
            }
            else if (right.Type is DecimalType)
            {
                Type = Primitive.Decimal;
                Left = ArithmeticCast.CastTo(left, Primitive.Decimal);
            }
            else if (left.Type is IntegerType)
            {
                Type = Primitive.Integer;
                Right = ArithmeticCast.CastTo(right, Primitive.Integer);
            }
            else if (right.Type is IntegerType)
            {
                Type = Primitive.Integer;
                Left = ArithmeticCast.CastTo(left, Primitive.Integer);
            }
            else
                throw new UnexpectedTypeException(left.Type, errorReportedElement);
        }
    }

    public sealed partial class ArrayOperator : IRValue
    {
        public enum ArrayOperation
        {
            GetArrayLength,
            GetArrayHandle
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public ArrayOperation Operation { get; private set; }
        public IType Type => Operation == ArrayOperation.GetArrayLength ? new IntegerType() : new HandleType();
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue ArrayValue { get; private set; }

        public ArrayOperator(ArrayOperation arrayOperation, IRValue arrayValue, IAstElement errorReportedElement)
        {
            Operation = arrayOperation;
            ArrayValue = arrayValue;
            ErrorReportedElement = errorReportedElement;
        }
    }
}
