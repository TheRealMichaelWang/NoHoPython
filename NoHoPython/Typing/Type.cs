using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using System.Text;

namespace NoHoPython.Typing
{
    public partial interface IType
    {
        public static string GetIdentifier(string typeIdentifier, params IType[] typeargs)
        {
            StringBuilder builder = new();
            builder.Append(typeIdentifier);
            foreach(IType type in typeargs)
            {
                builder.Append('_');
                builder.Append(type.Identifier);
            }
            return builder.ToString();
        }

        public static string GetPrototypeIdentifier(string typeIdentifier, List<TypeParameter> typeParameters)
        {
            StringBuilder builder = new();
            builder.Append(typeIdentifier);
            foreach (TypeParameter typeParameter in typeParameters)
            {
                builder.Append('_');
                builder.Append(typeParameter.Name);
            }
            return builder.ToString();
        }

        public bool IsNativeCType { get; }
        public bool RequiresDisposal { get; }
        public bool MustSetResponsibleDestroyer { get; }
        public bool IsTypeDependency { get; }

        public string TypeName { get; }
        public string Identifier { get; }
        public string PrototypeIdentifier {get;}
        public bool IsEmpty { get; }

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInformation);

        public string GetCName(IRProgram irProgram);
        public string GetStandardIdentifier(IRProgram irProgram);

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent);
        public void EmitCopyValue(IRProgram irProgram, Emitter primaryEmitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer);
        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord);

        public void EmitCStruct(IRProgram irProgram, Emitter emitter);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder);

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement, Syntax.AstIRProgramBuilder irBuilder);

        public bool IsCompatibleWith(IType type);
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement);
        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder);

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

        public UnexpectedTypeException(IType recievedType, string comments, Syntax.IAstElement errorReportedElement) : base(errorReportedElement, $"Unexpected type {recievedType.TypeName} recieved. {comments}")
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
