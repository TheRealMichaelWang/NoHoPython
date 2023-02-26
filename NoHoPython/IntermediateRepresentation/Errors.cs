using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    public abstract class IRGenerationError : Exception
    {
        public IAstElement ErrorReportedElement { get;private set; }

        public IRGenerationError(IAstElement errorReportedElement, string message) : base(message)
        {
            ErrorReportedElement = errorReportedElement;
        }

        public void Print()
        {
            Console.WriteLine($"IR Generation Error: {Message}");

            Console.WriteLine($"\nIn {ErrorReportedElement.SourceLocation}:\n");

            if (ErrorReportedElement is IAstValue astValue)
            {
                Console.WriteLine($"\t{astValue}");
            }
            else if (ErrorReportedElement is IAstStatement astStatement)
                Console.WriteLine(astStatement.ToString(0));
        }
    }

    public sealed class PropertyNotImplementedError : IRGenerationError
    {
        public InterfaceDeclaration.InterfaceProperty RequiredProperty { get; private set; }
        public RecordDeclaration Record { get; private set; }

        public PropertyNotImplementedError(InterfaceDeclaration.InterfaceProperty requiredProperty, RecordDeclaration record, IAstElement astElement) : base(astElement, $"{record.Name} doesn't implement required property {requiredProperty}.")
        {
            RequiredProperty = requiredProperty;
            Record = record;
        }
    }

    public sealed class PropertyAlreadyDefinedError : IRGenerationError
    {
        public string Property;

        public PropertyAlreadyDefinedError(string property, IAstElement errorReportedElement) : base(errorReportedElement, $"Property {property} already defined.")
        {
            Property = property;
        }
    }

    public sealed class InterfaceMustRequireProperties : IRGenerationError
    {
        public InterfaceDeclaration InterfaceDeclaration { get; private set; }

        public InterfaceMustRequireProperties(InterfaceDeclaration interfaceDeclaration, IAstElement errorReportedElement) : base(errorReportedElement, $"Interface {interfaceDeclaration.Name} must specify at least one required-implemented property.")
        {
            InterfaceDeclaration = interfaceDeclaration;
        }
    }

    public sealed class UnexpectedArgumentsException : IRGenerationError
    {
        public readonly List<IType> ArgumentTypes;
        public readonly List<Variable> Parameters;

        public UnexpectedArgumentsException(List<IType> argumentTypes, List<Variable> parameters, IAstElement astElement) : base(astElement, $"Procedure expected ({string.Join(", ", parameters.Select((Variable param) => param.Type.TypeName + " " + param.Name))}), but got ({string.Join(", ", argumentTypes.Select((IType argument) => argument.TypeName))}) instead.")
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

        public NotAProcedureException(IScopeSymbol scopeSymbol, IAstElement errorReportedElement) : base(errorReportedElement, $"{scopeSymbol.Name} is not a procedure. Rather it is a(n) {scopeSymbol}.")
        {
            ScopeSymbol = scopeSymbol;
        }
    }

    public sealed class NotAVariableException : IRGenerationError
    {
        public IScopeSymbol ScopeSymbol { get; private set; }

        public NotAVariableException(IScopeSymbol scopeSymbol, IAstElement errorReportedElement) : base(errorReportedElement, $"{scopeSymbol.Name} is not a variable. Rather it is a {scopeSymbol}.")
        {
            ScopeSymbol = scopeSymbol;
        }
    }

    public sealed class CannotMutateVaraible : IRGenerationError
    {
        public Variable Variable { get; private set; }

        public CannotMutateVaraible(Variable capturedVariable, IAstElement errorReportedElement) : base(errorReportedElement, $"Cannot mutate captured variable or parameter {capturedVariable.Name}.")
        {
            Variable = capturedVariable;
        }
    }

    public sealed class NoDefaultValueError : IRGenerationError
    {
        public IType Type { get; private set; }

        public NoDefaultValueError(IType type, IAstElement errorReportedElement) : base(errorReportedElement, $"Unable to get default value for {type.TypeName}.")
        {
            Type = type;
        }
    }

    public sealed class NotAllCodePathsReturnError : IRGenerationError
    {
        public NotAllCodePathsReturnError(IAstElement errorReportedElement) : base(errorReportedElement, $"Not all code paths return a value.")
        {

        }
    }

    public sealed class CannotUseUninitializedProperty : IRGenerationError
    {
        public Property Property { get; private set; }

        public CannotUseUninitializedProperty(Property property, IAstElement errorReportedElement) : base(errorReportedElement, $"Unable to use unitialized property {property.Name} of type {property.Type.TypeName}.")
        {
            Property = property;
        }
    }

    public sealed class CannotUseUninitializedSelf : IRGenerationError
    {
        public CannotUseUninitializedSelf(IAstElement errorReportedElement) : base(errorReportedElement, "Cannot use variable self until all properties are initialized.")
        {

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

    public sealed class RecordMustDefineCopierError : IRGenerationError
    {
        public RecordDeclaration RecordDeclaration { get; private set; }

        public RecordMustDefineCopierError(RecordDeclaration recordDeclaration) : base(recordDeclaration.ErrorReportedElement, $"Because {recordDeclaration.Name} implements a destructor, it must also implement a copier (do so using __copy__).")
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

    public sealed class UnexpectedLoopStatementException : IRGenerationError
    {
        public UnexpectedLoopStatementException(IAstElement errorReportedElement) : base(errorReportedElement, $"Continues and breaks may only be used inside of loops.")
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

        public SymbolAlreadyExistsException(IScopeSymbol existingSymbol, IAstElement astElement) : base(astElement, $"Symbol {existingSymbol.Name} already exists.")
        {
            ExistingSymbol = existingSymbol;
        }
    }

    public sealed class SymbolNotModuleException : IRGenerationError
    {
        public IScopeSymbol Symbol;

        public SymbolNotModuleException(IScopeSymbol symbol, IAstElement astElement) : base(astElement, $"Symbol {symbol.Name} isn't a module.")
        {
            Symbol = symbol;
        }
    }
}