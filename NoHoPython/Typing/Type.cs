using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;

namespace NoHoPython.Typing
{
    public partial interface IType
    {
        public bool IsNativeCType { get; }
        public bool RequiresDisposal { get; }
        public bool MustSetResponsibleDestroyer { get; }

        public string TypeName { get; }
        public string Identifier { get; }
        public bool IsEmpty { get; }

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInformation);

        public string GetCName(IRProgram irProgram);
        public string GetStandardIdentifier(IRProgram irProgram);

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent);
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer);
        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent);
        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource);

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder);

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement);

        public bool IsCompatibleWith(IType type);
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement);
        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument);

        public string? ToString() => TypeName;
    }

    public sealed class ITypeComparer : IEqualityComparer<IType>, IComparer<IType>
    {
        public bool Equals(IType? a, IType? b)
        {
            if (a == null || b == null)
                return false;
            return a.IsCompatibleWith(b);
        }

        public int GetHashCode(IType? type)
        {
            if (type == null)
                return 0;
            return type.Identifier.GetHashCode();
        }

        public int Compare(IType? a, IType? b) => GetHashCode(a) - GetHashCode(b);
    }

    public sealed class UnexpectedTypeException : IRGenerationError
    {
        public IType? ExpectedType { get; private set; }
        public IType RecievedType { get; private set; }

        public UnexpectedTypeException(IType expectedType, IType recievedType, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Expected {expectedType.TypeName}, got {recievedType.TypeName} instead.")
        {
            ExpectedType = expectedType;
            RecievedType = recievedType;
        }

        public UnexpectedTypeException(IType recievedType, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Unexpected type {recievedType.TypeName} recieved.")
        {
            ExpectedType = null;
            RecievedType = recievedType;
        }
    }

    public sealed class UnexpectedTypeArgumentsException : IRGenerationError
    {
        public int? ExpectedArgumentCount { get; private set; }
        public int RecievedArgumentCount { get; private set; }

        public UnexpectedTypeArgumentsException(int expectedArgumentCount, int recievedArgumentCount, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Expected {expectedArgumentCount} type arguments, got {recievedArgumentCount} instead.")
        {
            ExpectedArgumentCount = expectedArgumentCount;
            RecievedArgumentCount = recievedArgumentCount;
        }

        public UnexpectedTypeArgumentsException(int recievedArgumentCount, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Did not expect {recievedArgumentCount} type arguments.")
        {
            ExpectedArgumentCount = null;
            RecievedArgumentCount = recievedArgumentCount;
        }
    }

    public sealed class UnsupportedInterfaceException : IRGenerationError
    {
        public EnumDeclaration EnumDeclaration { get; private set; }
        public InterfaceType Interface { get; private set; }

        public UnsupportedInterfaceException(EnumDeclaration enumDeclaration, InterfaceType @interface, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Enum {enumDeclaration.Name} does not support interface {@interface.TypeName}, which is annotated required supported interface.")
        {
            EnumDeclaration = enumDeclaration;
            Interface = @interface;
        }
    }

    public sealed class NotATypeException : IRGenerationError
    {
        public IScopeSymbol ScopeSymbol { get; private set; }

        public NotATypeException(IScopeSymbol scopeSymbol, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"{scopeSymbol.Name} is not a type parameter, record, interface, or enum. Rather it is a {scopeSymbol}.")
        {
            ScopeSymbol = scopeSymbol;
        }
    }
}
