using NoHoPython.IntermediateRepresentation.Statements;
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
            IntToDecimal,
            IntToChar,
            IntToBoolean
        }

        private static Dictionary<ArithmeticCastOperation, IType> outputTypes = new Dictionary<ArithmeticCastOperation, IType>()
        {
            {ArithmeticCastOperation.DecimalToInt, new IntegerType() },
            {ArithmeticCastOperation.CharToInt, new IntegerType() },
            {ArithmeticCastOperation.BooleanToInt, new IntegerType() },
            {ArithmeticCastOperation.IntToDecimal, new DecimalType() },
            {ArithmeticCastOperation.IntToChar, new CharacterType() },
            {ArithmeticCastOperation.IntToBoolean, new CharacterType() }
        };

        private static Dictionary<ArithmeticCastOperation, IType> inputTypes = new Dictionary<ArithmeticCastOperation, IType>()
        {
            {ArithmeticCastOperation.IntToDecimal, new IntegerType() },
            {ArithmeticCastOperation.IntToChar, new IntegerType() },
            {ArithmeticCastOperation.IntToBoolean, new IntegerType() },
            {ArithmeticCastOperation.DecimalToInt, new DecimalType() },
            {ArithmeticCastOperation.CharToInt, new CharacterType() },
            {ArithmeticCastOperation.BooleanToInt, new CharacterType() }
        };

        private static ArithmeticCastOperation[] toIntOperation = new ArithmeticCastOperation[]
        {
            ArithmeticCastOperation.DecimalToInt,
            ArithmeticCastOperation.CharToInt,
            ArithmeticCastOperation.BooleanToInt
        };

        private static ArithmeticCastOperation[] intToOperation = new ArithmeticCastOperation[]
        {
            ArithmeticCastOperation.IntToDecimal,
            ArithmeticCastOperation.IntToChar,
            ArithmeticCastOperation.IntToBoolean
        };

        public static IRValue CastTo(IRValue value, IType typeTarget)
        {
            if (typeTarget.IsCompatibleWith(value.Type))
                return value;
            else if (value.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty($"to{typeTarget.TypeName}"))
                return new AnonymousProcedureCall(new GetPropertyValue(value, $"to{typeTarget.TypeName}"), new List<IRValue>());
            else if(typeTarget is EnumType enumType)
                return new MarshalIntoEnum(enumType, value);
            else if(typeTarget is InterfaceType interfaceType)
                return new MarshalIntoInterface(interfaceType, value);
            else if(typeTarget is RecordType recordType)
            {
                return new AllocRecord(recordType, new List<IRValue>()
                {
                    value
                });
            }
            else if (typeTarget is Primitive primitive)
                return PrimitiveCast(value, primitive);
            else
                throw new UnexpectedTypeException(typeTarget, value.Type);
        }

        private static IRValue PrimitiveCast(IRValue primitive, Primitive targetType)
        {
            if (primitive.Type is not Primitive)
                throw new UnexpectedTypeException(primitive.Type);
            if (targetType.IsCompatibleWith(primitive.Type))
                throw new UnexpectedTypeException(primitive.Type);

            Primitive input = (Primitive)primitive;
            if (targetType is IntegerType)
                return new ArithmeticCast(toIntOperation[input.Id - 1], primitive);
            else if (primitive.Type is IntegerType)
                return new ArithmeticCast(intToOperation[targetType.Id - 1], primitive);
            else
                return new ArithmeticCast(intToOperation[targetType.Id - 1], new ArithmeticCast(toIntOperation[input.Id - 1], primitive));
        }

        public IType Type => outputTypes[Operation];

        public ArithmeticCastOperation Operation { get; private set; }
        public IRValue Input { get; private set; }

        private ArithmeticCast(ArithmeticCastOperation operation, IRValue input)
        {
            Operation = operation;
            Input = input;

            if (!inputTypes[Operation].IsCompatibleWith(input.Type))
                throw new UnexpectedTypeException(inputTypes[Operation], input.Type);
        }
    }

    public sealed partial class ArithmeticOperator : IRValue
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

        private static Dictionary<ArithmeticOperation, string> operatorOverloadIdentifiers = new()
        {
            {ArithmeticOperation.Add, "add"},
            { ArithmeticOperation.Subtract,"subtract" },
            { ArithmeticOperation.Divide,"divide" },
            {ArithmeticOperation.Multiply ,"multiply"},
            {ArithmeticOperation.Modulo ,"modulo"},
            {ArithmeticOperation.Exponentiate, "exponentiate"}
        };

        public static bool IsCommunicative(ArithmeticOperation operation) => ((int)operation % 2) == 0;

        public static IRValue ComposeArithmeticOperation(ArithmeticOperation operation, IRValue left, IRValue right)
        {
            if (left.Type is IPropertyContainer container && container.HasProperty(operatorOverloadIdentifiers[operation]))
            {
                return new AnonymousProcedureCall(new GetPropertyValue(left, operatorOverloadIdentifiers[operation]), new List<IRValue>()
                {
                    right
                });
            }
            else
                return new ArithmeticOperator(operation, left, right);
        }

        public ArithmeticOperation Operation { get; private set; }
        public IType Type { get; private set; }

        public IRValue Left { get; private set; }
        public IRValue Right { get; private set; }

        public ArithmeticOperator(ArithmeticOperation operation, IRValue left, IRValue right)
        {
            Operation = operation;
            Left = left;
            Right = right;

            if (left.Type is DecimalType)
            {
                Type = new DecimalType();
                Right = ArithmeticCast.CastTo(right, Primitive.Decimal);
            }
            else if (right.Type is DecimalType)
            {
                Type = new DecimalType();
                Left = ArithmeticCast.CastTo(left, Primitive.Decimal);
            }
            else if (left.Type is IntegerType)
            {
                Type = new IntegerType();
                Right = ArithmeticCast.CastTo(right, Primitive.Integer);
            }
            else if (right.Type is IntegerType)
            {
                Type = new IntegerType();
                Left = ArithmeticCast.CastTo(left, Primitive.Integer);
            }
            else
                throw new UnexpectedTypeException(left.Type);
        }
    }
}
