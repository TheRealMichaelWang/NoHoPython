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
            {ArithmeticCastOperation.DecimalToInt, Primitive.Integer },
            {ArithmeticCastOperation.CharToInt, Primitive.Integer },
            {ArithmeticCastOperation.BooleanToInt, Primitive.Integer },
            {ArithmeticCastOperation.HandleToInt, Primitive.Integer },
            {ArithmeticCastOperation.IntToDecimal, Primitive.Decimal },
            {ArithmeticCastOperation.IntToChar, Primitive.Character },
            {ArithmeticCastOperation.IntToBoolean, Primitive.Boolean },
            {ArithmeticCastOperation.IntToHandle, Primitive.Handle }
        };

        private static Dictionary<ArithmeticCastOperation, IType> inputTypes = new()
        {
            {ArithmeticCastOperation.DecimalToInt, Primitive.Decimal },
            {ArithmeticCastOperation.CharToInt, Primitive.Character },
            {ArithmeticCastOperation.BooleanToInt, Primitive.Boolean },
            {ArithmeticCastOperation.HandleToInt, Primitive.Handle },
            {ArithmeticCastOperation.IntToDecimal, Primitive.Integer },
            {ArithmeticCastOperation.IntToChar, Primitive.Integer },
            {ArithmeticCastOperation.IntToBoolean, Primitive.Integer },
            {ArithmeticCastOperation.IntToHandle, Primitive.Integer }
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

        public static IRValue CastTo(IRValue value, IType typeTarget, AstIRProgramBuilder irBuilder, bool explicitCast=false)
        {
            if (typeTarget.IsCompatibleWith(value.Type))
                return value;

            //infalible type conversions
            if (GetPropertyValue.HasMessageReceiver(value, $"to_{typeTarget.Identifier}", irBuilder))
                return AnonymousProcedureCall.SendMessage(value, $"to_{typeTarget.Identifier}", typeTarget, new(), irBuilder, value.ErrorReportedElement, false);
            if (GetPropertyValue.HasMessageReceiver(value, $"to_{typeTarget.PrototypeIdentifier}", irBuilder))
                return AnonymousProcedureCall.SendMessage(value, $"to_{typeTarget.PrototypeIdentifier}", typeTarget, new(), irBuilder, value.ErrorReportedElement, false);

            if (typeTarget is HandleType handleType)
            {
                if (explicitCast && value.Type is HandleType)
                    return new HandleCast(handleType, value, value.ErrorReportedElement);
                else if(value.Type is ArrayType)
                    return new ArrayOperator(ArrayOperator.ArrayOperation.GetArrayHandle, value, value.ErrorReportedElement);
                else if (value.Type is MemorySpan)
                    return new ArrayOperator(ArrayOperator.ArrayOperation.GetSpanHandle, value, value.ErrorReportedElement);
            }
            if (typeTarget is ArrayType && value.Type is MemorySpan)
                return new MarshalMemorySpanIntoArray(value, value.ErrorReportedElement);
            if(typeTarget is IntegerType && value.Type is ArrayType)
                return new ArrayOperator(ArrayOperator.ArrayOperation.GetArrayLength, value, value.ErrorReportedElement);
            if (typeTarget is Primitive primitive)
                return PrimitiveCast(value, primitive);

            //type conversions that could potentially fail
            if (value.Type is EnumType enumType1 && enumType1.SupportsType(typeTarget))
                return new UnwrapEnumValue(value, typeTarget, irBuilder, value.ErrorReportedElement);
            if (typeTarget is TupleType tupleTarget && value.Type is TupleType)
                return new MarshalIntoLowerTuple(tupleTarget, value, value.ErrorReportedElement);
            if(typeTarget is EnumType enumType)
                return new MarshalIntoEnum(enumType, value, irBuilder, value.ErrorReportedElement);
            if (typeTarget is InterfaceType interfaceType)
                return new MarshalIntoInterface(interfaceType, value, value.ErrorReportedElement);
            if (typeTarget is RecordType recordType)
                return new AllocRecord(recordType, new List<IRValue>() { value }, irBuilder, value.ErrorReportedElement);

            throw new UnexpectedTypeException(typeTarget, value.Type, value.ErrorReportedElement);
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
            if (!inputTypes[operation].IsCompatibleWith(input.Type))
                throw new UnexpectedTypeException(inputTypes[operation], input.Type, errorReportedElement);

            Operation = operation;
            Input = input;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class HandleCast : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TargetHandleType;
        public bool IsTruey => Input.IsTruey;
        public bool IsFalsey => Input.IsFalsey;

        public HandleType TargetHandleType { get; private set; }
        public IRValue Input { get; private set; }

        public HandleCast(HandleType targetHandleType, IRValue input, IAstElement errorReportedElement)
        {
            TargetHandleType = targetHandleType;
            Input = input;
            ErrorReportedElement = errorReportedElement;
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

        public static IRValue ComposeArithmeticOperation(ArithmeticOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (GetPropertyValue.HasMessageReceiver(left, operatorOverloadIdentifiers[operation], irBuilder))
                return AnonymousProcedureCall.SendMessage(left, operatorOverloadIdentifiers[operation], null, new List<IRValue>() { right }, irBuilder, errorReportedElement);
            if (OperationIsCommunicative(operation) && GetPropertyValue.HasMessageReceiver(right, operatorOverloadIdentifiers[operation], irBuilder))
                return AnonymousProcedureCall.SendMessage(right, operatorOverloadIdentifiers[operation], null, new List<IRValue>() { left }, irBuilder, errorReportedElement);
            if(operation == ArithmeticOperation.Subtract && GetPropertyValue.HasMessageReceiver(right, "negate", irBuilder))
            {
                IRValue negativeRight = AnonymousProcedureCall.SendMessage(right, "negate", null, new(), irBuilder, errorReportedElement);
                if (left.IsFalsey)
                    return negativeRight;
                else
                    return ComposeArithmeticOperation(ArithmeticOperation.Add, left, negativeRight, irBuilder, errorReportedElement);
            }
            if (operation == ArithmeticOperation.Divide)
            {
                if (GetPropertyValue.HasMessageReceiver(right, "inverse", irBuilder))
                {
                    IRValue invertedRight = AnonymousProcedureCall.SendMessage(right, "inverse", null, new(), irBuilder, errorReportedElement);
                    if (left is IntegerLiteral integerLiteral && integerLiteral.Number == 1)
                        return invertedRight;
                    else
                        return ComposeArithmeticOperation(ArithmeticOperation.Multiply, left, invertedRight, irBuilder, errorReportedElement);
                }
                if(GetPropertyValue.HasMessageReceiver(right, "exponentiate", irBuilder))
                {
                    IRValue invertedRight = AnonymousProcedureCall.SendMessage(right, "exponentiate", null, new() { new IntegerLiteral(-1, errorReportedElement) }, irBuilder, errorReportedElement);
                    if (left is IntegerLiteral integerLiteral && integerLiteral.Number == 1)
                        return invertedRight;
                    else
                        return ComposeArithmeticOperation(ArithmeticOperation.Multiply, left, invertedRight, irBuilder, errorReportedElement);
                }
            }

            return (left.Type is HandleType && operation == ArithmeticOperation.Add)
                ? new PointerAddOperator(left, right, irBuilder, errorReportedElement)
                : (right.Type is HandleType && operation == ArithmeticOperation.Add)
                ? new PointerAddOperator(right, left, irBuilder, errorReportedElement)
                : new ArithmeticOperator(operation, left, right, irBuilder, errorReportedElement);
        }

        public ArithmeticOperation Operation { get; private set; }
        public override IType Type { get; }

        private ArithmeticOperator(ArithmeticOperation operation, IRValue left, IRValue right, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
        {
            Operation = operation;

            if (left.Type is DecimalType)
            {
                Type = Primitive.Decimal;
                Right = ArithmeticCast.CastTo(right, Primitive.Decimal, irBuilder);
            }
            else if (right.Type is DecimalType)
            {
                Type = Primitive.Decimal;
                Left = ArithmeticCast.CastTo(left, Primitive.Decimal, irBuilder);
            }
            else if (left.Type is IntegerType)
            {
                Type = Primitive.Integer;
                Right = ArithmeticCast.CastTo(right, Primitive.Integer, irBuilder);
            }
            else if (right.Type is IntegerType)
            {
                Type = Primitive.Integer;
                Left = ArithmeticCast.CastTo(left, Primitive.Integer, irBuilder);
            }
            else
                throw new UnexpectedTypeException(left.Type, errorReportedElement);
        }

        private ArithmeticOperator(IType type, ArithmeticOperation operation, IRValue left, IRValue right, IAstElement errorReportedElement) : base(left, right, errorReportedElement)
        {
            Type = type;
            Operation = operation;
        }
    }

    public sealed partial class PointerAddOperator : BinaryOperator
    {
        public override IType Type => Address.Type;

        public IRValue Address => Left;
        public IRValue Offset => Right;

        public PointerAddOperator(IRValue address, IRValue offset, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(address, ArithmeticCast.CastTo(offset, Primitive.Integer, irBuilder), errorReportedElement)
        {
            if (address.Type is not HandleType)
                throw new UnexpectedTypeException(address.Type, errorReportedElement);
        }

        private PointerAddOperator(IRValue address, IRValue offset, IAstElement errorReportedElement) : base(address, offset, errorReportedElement)
        {

        }
    }

    public sealed partial class ArrayOperator : IRValue
    {
        public enum ArrayOperation
        {
            GetArrayLength,
            GetArrayHandle,
            GetSpanHandle
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public ArrayOperation Operation { get; private set; }

#pragma warning disable CS8524 //All possible ArrayOperation values are covered. 
        public IType Type => Operation switch
        {
            ArrayOperation.GetArrayLength => Primitive.Integer,
            ArrayOperation.GetArrayHandle => new HandleType(ElementType),
            ArrayOperation.GetSpanHandle => new HandleType(ElementType)
        };
#pragma warning restore CS8524

        public bool IsTruey => false;
        public bool IsFalsey => false;

        public IRValue ArrayValue { get; private set; }
        public IType ElementType { get; private set; }

        public ArrayOperator(ArrayOperation arrayOperation, IRValue arrayValue, IAstElement errorReportedElement)
        {
            Operation = arrayOperation;
            ArrayValue = arrayValue;
            ErrorReportedElement = errorReportedElement;

            if (ArrayValue.Type is ArrayType arrayType)
                ElementType = arrayType.ElementType;
            else if (ArrayValue.Type is MemorySpan memorySpan)
                ElementType = memorySpan.ElementType;
            else
                throw new UnexpectedTypeException(ArrayValue.Type, errorReportedElement);
        }
    }
}
