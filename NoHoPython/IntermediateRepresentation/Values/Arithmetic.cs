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

        public static IRValue PrimitiveCast(IRValue primitive, Primitive targetType)
        {
            if (primitive.Type is not Primitive)
                throw new UnexpectedTypeException(primitive.Type);
            if (targetType.Equals(primitive.Type))
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

        public ArithmeticCast(ArithmeticCastOperation operation, IRValue input)
        {
            Operation = operation;
            Input = input;

            if (!inputTypes[Operation].Equals(input.Type))
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
            Exponentiate = 7,
            Factorial = 9
        }

        private static Dictionary<ArithmeticOperation, string> operatorOverloadIdentifiers = new()
        {
            {ArithmeticOperation.Add, "add"},
            { ArithmeticOperation.Subtract,"subtract" },
            { ArithmeticOperation.Divide,"divide" },
            {ArithmeticOperation.Multiply ,"multiply"},
            {ArithmeticOperation.Modulo ,"modulo"},
            {ArithmeticOperation.Exponentiate, "exponentiate"},
            {ArithmeticOperation.Factorial, "factorial"}
        };

        public static bool IsCommunicative(ArithmeticOperation operation) => ((int)operation % 2) == 0;

        public static IRValue ComposeArithmeticOperation(ArithmeticOperation operation, IRValue left, IRValue right)
        {
            if(left.Type is RecordType record)
            {
                RecordDeclaration.RecordProperty recordProperty =  
            }
            else

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
                if (right.Type is IntegerType)
                    Right = new ArithmeticCast(ArithmeticCast.ArithmeticCastOperation.IntToDecimal, right);
            }
            else if (right.Type is DecimalType)
            {
                Type = new DecimalType();
                if (left.Type is IntegerType)
                    Left = new ArithmeticCast(ArithmeticCast.ArithmeticCastOperation.IntToDecimal, left);
            }
            else
                Type = new IntegerType();
        }
    }
}
