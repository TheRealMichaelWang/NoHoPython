using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    public abstract class IRGenerationError : Exception
    {
        public IAstElement AstElement { get;private set; }

        public IRGenerationError(IAstElement astElement, string message) : base(message)
        {
            AstElement = astElement;
        }

        public void Print()
        {
            Console.WriteLine($"IR Generation Error: {Message}");

            Console.WriteLine($"\nIn {AstElement.SourceLocation}:\n");

            if (AstElement is IAstValue astValue)
            {
                Console.WriteLine($"\t{astValue}");
            }
            else if (AstElement is IAstStatement astStatement)
                Console.WriteLine(astStatement.ToString(0));
        }
    }

    public abstract class CCodegenError : Exception
    {
        public IRElement? IRElement { get; private set; }

        public CCodegenError(IRElement? iRElement, string message) : base(message)
        {
            IRElement = iRElement;
        }

        public void Print()
        {
            Console.WriteLine($"Codegen(to C) Error: {Message}");

            if (IRElement == null)
                return;

            IAstElement AstElement = IRElement.ErrorReportedElement;
            Console.WriteLine($"\nIn {AstElement.SourceLocation}:\n");

            if (AstElement is IAstValue astValue)
            {
                Console.WriteLine($"\t{astValue}");
            }
            else if (AstElement is IAstStatement astStatement)
                Console.WriteLine(astStatement.ToString(0));
        }
    }

    public sealed class PropertyNotImplementedException : IRGenerationError
    {
        public InterfaceDeclaration.InterfaceProperty RequiredProperty { get; private set; }
        public RecordDeclaration Record { get; private set; }

        public PropertyNotImplementedException(InterfaceDeclaration.InterfaceProperty requiredProperty, RecordDeclaration record, IAstElement astElement) : base(astElement, $"{record.Name} doesn't implement required property {requiredProperty}.")
        {
            RequiredProperty = requiredProperty;
            Record = record;
        }
    }

    public sealed class UnexpectedArgumentsException : IRGenerationError
    {
        public readonly List<IType> ArgumentTypes;
        public readonly List<Variable> Parameters;

        public UnexpectedArgumentsException(List<IType> argumentTypes, List<Variable> parameters, IAstElement astElement) : base(astElement, $"Procedure expected ({string.Join(", ", parameters.Select((Variable param) => param.Type + " " + param.Name))}), but got ({string.Join(", ", argumentTypes.Select((IType argument) => argument.TypeName))}) instead.")
        {
            ArgumentTypes = argumentTypes;
            Parameters = parameters;
        }
    }

    public sealed class UnexpectedReturnStatement : IRGenerationError
    {
        public readonly Syntax.Statements.ReturnStatement ReturnStatement;

        public UnexpectedReturnStatement(Syntax.Statements.ReturnStatement returnStatement) : base(returnStatement, $"Unexpected return statement.")
        {
            ReturnStatement = returnStatement;
        }
    }

    public sealed class NotAProcedureException : IRGenerationError
    {
        public IScopeSymbol ScopeSymbol { get; private set; }

        public NotAProcedureException(IScopeSymbol scopeSymbol, IAstElement astElement) : base(astElement, $"{scopeSymbol.Name} is not a procedure. Rather it is a(n) {scopeSymbol}.")
        {
            ScopeSymbol = scopeSymbol;
        }
    }

    public sealed class NotAVariableException : IRGenerationError
    {
        public IScopeSymbol ScopeSymbol { get; private set; }

        public NotAVariableException(IScopeSymbol scopeSymbol, IAstElement astElement) : base(astElement, $"{scopeSymbol.Name} is not a variable. Rather it is a {scopeSymbol}.")
        {
            ScopeSymbol = scopeSymbol;
        }
    }

    public sealed class CannotMutateCapturedVaraible : IRGenerationError
    {
        public Variable CapturedVariable { get; private set; }

        public CannotMutateCapturedVaraible(Variable capturedVariable, IAstElement astElement) : base(astElement, $"Cannot mutate captured variable {capturedVariable.Name}.")
        {
            CapturedVariable = capturedVariable;
        }
    }

    public sealed class NoDefaultValueError : IRGenerationError
    {
        public IType Type { get; private set; }

        public NoDefaultValueError(IType type, IAstElement astElement) : base(astElement, $"Unable to get default value for {type.TypeName}.")
        {
            Type = type;
        }
    }

    public sealed class NotAllCodePathsReturnError : IRGenerationError
    {
        public NotAllCodePathsReturnError(IAstElement astElement) : base(astElement, $"Not all code paths return a value.")
        {

        }
    }

    public sealed class CannotUseUninitializedProperty : IRGenerationError
    {
        public Property Property { get; private set; }

        public CannotUseUninitializedProperty(Property property, IAstElement astElement) : base(astElement, $"Unable to use unitialized property {property.Name} of type {property.Type.TypeName}.")
        {
            Property = property;
        }
    }

    public sealed class PropertyNotInitialized : IRGenerationError
    {
        public Property Property { get; private set; }

        public PropertyNotInitialized(Property property, IAstElement astElement) : base(astElement, $"Not all constructor code paths initialize property {property.Name} of type {property.Type.TypeName}.")
        {
            Property = property;
        }
    }

    public sealed class RecordMustDefineConstructorError : IRGenerationError
    {
        public RecordDeclaration RecordDeclaration { get; private set; }

        public RecordMustDefineConstructorError(RecordDeclaration recordDeclaration) : base(recordDeclaration.ErrorReportedElement, $"Record {recordDeclaration.Name} doesn't define a constructor (do so using __init__).")
        {
            RecordDeclaration = recordDeclaration;
        }
    }

    public sealed class InsufficientEnumOptions : IRGenerationError
    {
        public InsufficientEnumOptions(IAstElement astElement) : base(astElement, "Enum/Variant must have at least two type options.")
        {

        }
    }

    public sealed class UnhandledMatchOption : IRGenerationError
    {
        public EnumType EnumType { get; private set; }
        public IType UnhandledType { get; private set; }

        public UnhandledMatchOption(EnumType enumType, IType unhandledType, IAstStatement astStatement) : base(astStatement, $"Match statement doesn't implement handler for type {unhandledType.TypeName}, which is implemented by matched enum {enumType.TypeName}.")
        {
            EnumType = enumType;
            UnhandledType = unhandledType;
        }
    }

    public sealed class NoPostEvalPureValue : IRGenerationError
    {
        public IRValue Value { get; private set; }

        public NoPostEvalPureValue(IRValue value) : base(value.ErrorReportedElement, $"Value must be evaluated twice; no pure evaluation can be generated.")
        {
            Value = value;
        }
    }

    public sealed class CannotEmitDestructorError : CCodegenError
    {
        public IRValue Value { get; private set; }

        public CannotEmitDestructorError(IRValue value) : base(value, "Cannot emit destructor for value. Please move to a variable.")
        {
            Value = value;
        }
    }


    public sealed class CannotCompileNothingError : CCodegenError
    {
        public CannotCompileNothingError(IRElement? errorReportedElement) : base(errorReportedElement, "(Internal Error)Cannot actually compile/emit a nothing literal nor scope a nothing type.")
        {

        }
    }

    public sealed class UnexpectedTypeParameterError : CCodegenError
    {
        public UnexpectedTypeParameterError(Typing.TypeParameter typeParameter, IRElement? errorReportedElement) : base(errorReportedElement, $"(Internal Error)Could not scope or compile/emit the type parameter {typeParameter.Name}.")
        {

        }
    }

    public sealed class CircularDependentTypesError : CCodegenError
    {
        public CircularDependentTypesError(List<IType> dependecyChain, IType circularDependentType) : base(null, $"Type {dependecyChain[0].TypeName} is circularly dependent; {string.Join(" -> ", dependecyChain.ConvertAll((type) => type.TypeName))}, and depends on {circularDependentType.TypeName} again. Please note that the size of {dependecyChain[0].TypeName} has to be known during compilation)")
        {

        }
    }
}

namespace NoHoPython.Scoping
{
    public sealed class SymbolNotFoundException : IRGenerationError
    {
        public string Identifier { get; private set; }
        public SymbolContainer ParentContainer;

        public SymbolNotFoundException(string identifier, SymbolContainer parentContainer, IAstElement astElement) : base(astElement, $"Symbol {identifier} not found.")
        {
            Identifier = identifier;
            ParentContainer = parentContainer;
        }
    }

    public sealed class SymbolAlreadyExistsException : IRGenerationError
    {
        public IScopeSymbol ExistingSymbol;
        public SymbolContainer ParentContainer;

        public SymbolAlreadyExistsException(IScopeSymbol existingSymbol, SymbolContainer parentContainer, IAstElement astElement) : base(astElement, $"Symbol {existingSymbol.Name} already exists.")
        {
            ExistingSymbol = existingSymbol;
            ParentContainer = parentContainer;
        }
    }

    public sealed class SymbolNotModuleException : IRGenerationError
    {
        public IScopeSymbol Symbol;
        public SymbolContainer ParentContainer;

        public SymbolNotModuleException(IScopeSymbol symbol, SymbolContainer parentContainer, IAstElement astElement) : base(astElement, $"Symbol {symbol.Name} isn't a module.")
        {
            Symbol = symbol;
            ParentContainer = parentContainer;
        }
    }
}