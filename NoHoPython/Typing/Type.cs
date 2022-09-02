namespace NoHoPython.Typing
{
    public interface IType
    {
        public string TypeName { get; }

        public bool IsCompatibleWith(IType type);

        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
        public IType Clone();

        public string ToString() => TypeName;
    }

    public sealed class UnexpectedTypeException : Exception
    {
        public IType? ExpectedType { get; private set; }
        public IType RecievedType { get; private set; }

        public UnexpectedTypeException(IType expectedType, IType recievedType) : base($"Expected {expectedType.TypeName}, got {recievedType.TypeName} instead.")
        {
            ExpectedType = expectedType;
            RecievedType = recievedType;
        }

        public UnexpectedTypeException(IType recievedType) : base($"Unexpected type {recievedType.TypeName} recieved.")
        {
            ExpectedType = null;
            RecievedType = recievedType;
        }
    }

    public sealed class UnexpectedTypeArgumentsException : Exception
    {
        public int ExpectedArgumentCount { get;private set; }
        public int RecievedArgumentCount { get; private set; }

        public UnexpectedTypeArgumentsException(int expectedArgumentCount, int recievedArgumentCount) : base($"Expected {expectedArgumentCount} type arguments, got {recievedArgumentCount} instead.")
        {
            ExpectedArgumentCount = expectedArgumentCount;
            RecievedArgumentCount = recievedArgumentCount;
        }
    }
}
