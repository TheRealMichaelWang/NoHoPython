﻿using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
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
            Console.WriteLine($"Compilation Error: {Message}");
            Console.WriteLine($"\nIn file \"{AstElement.SourceLocation.File}\", row {AstElement.SourceLocation.Row}, col {AstElement.SourceLocation.Column}:\n");

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