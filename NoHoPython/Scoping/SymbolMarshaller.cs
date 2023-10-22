using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.Scoping
{
    public abstract class SymbolContainer
    {
        public bool IsHeadContainer { get; private set; }
        public abstract bool IsGloballyNavigable { get; }

        private Dictionary<string, IScopeSymbol> symbols;

        public SymbolContainer(bool isHeadContainer = false)
        {
            symbols = new Dictionary<string, IScopeSymbol>();
            IsHeadContainer = isHeadContainer;
        }

        public virtual IScopeSymbol? FindSymbol(string identifier) => symbols.ContainsKey(identifier) ? symbols[identifier] : null;

        public virtual void DeclareSymbol(IScopeSymbol symbol, IAstElement errorReportElement)
        {
            IScopeSymbol? existingSymbol = FindSymbol(symbol.Name);
            if (existingSymbol == null)
            {
                symbols.Add(symbol.Name, symbol);
                return;
            }
            throw new SymbolAlreadyExistsException(existingSymbol, errorReportElement);
        }
    }

    public sealed class SymbolMarshaller
    {
        public sealed class Module : SymbolContainer, IScopeSymbol, IRStatement
        {
            public IAstElement ErrorReportedElement { get; private set; }
            public SymbolContainer ParentContainer { get; private set; }

            public override bool IsGloballyNavigable => true;
            public string Name { get; private set; }

            private List<IRStatement> statements;

#pragma warning disable CS8618 // Only occurs for program head
            public Module(string name, SymbolContainer? parentContainer, IAstElement errorReportedElement) : base(parentContainer == null)
#pragma warning restore CS8618 
            {
                Name = name;
                ErrorReportedElement = errorReportedElement;
                statements = new List<IRStatement>();
#pragma warning disable CS8601 // Only occurs for program head
                ParentContainer = parentContainer;
#pragma warning restore CS8601
            }

            public void DelayedLinkSetStatements(List<IRStatement> statements) => this.statements.AddRange(statements);

            public void ScopeForUsedTypes(Dictionary<Typing.TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();
            public void Emit(IRProgram irProgram, Emitter emitter, Dictionary<Typing.TypeParameter, IType> typeargs) => throw new InvalidOperationException();
            public bool AllCodePathsReturn() => throw new InvalidOperationException();

            public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
            public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
            public bool SomeCodePathsBreak() => throw new InvalidOperationException();
        }

        public Module CurrentModule => usedModuleStack.Peek();
        public CodeBlock CurrentCodeBlock => (CodeBlock)scopeStack.Peek();
        public SymbolContainer CurrentScope => scopeStack.Peek();

        private Stack<Module> usedModuleStack;
        private Stack<SymbolContainer> scopeStack;

        public SymbolMarshaller()
        {
            usedModuleStack = new Stack<Module>();
            scopeStack = new Stack<SymbolContainer>();
#pragma warning disable CS8625 // default module must be here
            NavigateToScope(new Module(string.Empty, null, null));
#pragma warning restore CS8625
        }

        public IScopeSymbol FindSymbol(string identifier, IAstElement errorReportedElement)
        {
            static IScopeSymbol FindSymbolFromContainer(string identifier, SymbolContainer currentContainer, bool fromGlobalStack, IAstElement errorReportedElement)
            {
                if (identifier.Contains(' '))
                    throw new ArgumentException("Identifier cannot contain spaces.");

                SymbolContainer scopedContainer = currentContainer;
                string[] parts = identifier.Split(':', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    IScopeSymbol? symbol = scopedContainer.FindSymbol(parts[i]);
                    if (symbol == null)
                        throw new SymbolNotFoundException(parts[i], scopedContainer, errorReportedElement);
                    else if (symbol is SymbolContainer symbolContainer)
                    {
                        if (fromGlobalStack && !symbolContainer.IsGloballyNavigable)
                            throw new SymbolNotFoundException(parts[i], symbolContainer, errorReportedElement);
                        scopedContainer = symbolContainer;
                    }
                    else
                        throw new SymbolNotModuleException(symbol, errorReportedElement);

                    //scopedContainer = symbol == null || (fromGlobalStack && !symbol.IsGloballyNavigable)
                    //    ? throw new SymbolNotFoundException(parts[i], scopedContainer, errorReportedElement)
                    //    : symbol is SymbolContainer symbolContainer ? symbolContainer : throw new SymbolNotModuleException(symbol, errorReportedElement);
                }

                string finalIdentifier = parts.Last();
                IScopeSymbol? result = scopedContainer.FindSymbol(finalIdentifier);
                return result ?? throw new SymbolNotFoundException(finalIdentifier, scopedContainer, errorReportedElement);
            }

            try
            {
                return FindSymbolFromContainer(identifier, scopeStack.Peek(), false, errorReportedElement);
            }
            catch (SymbolNotFoundException)
            {

                foreach (Module symbolContainer in usedModuleStack)
                    try
                    {
                        return FindSymbolFromContainer(identifier, symbolContainer, true, errorReportedElement);
                    }
                    catch (SymbolNotFoundException)
                    {
                        continue;
                    }
            }
            throw new SymbolNotFoundException(identifier, scopeStack.Peek(), errorReportedElement);
        }

        public IScopeSymbol? FindSymbol(string identifier)
        {
            static IScopeSymbol? FindSymbolFromContainer(string identifier, SymbolContainer currentContainer, bool fromGlobalStack)
            {
                if (identifier.Contains(' '))
                    throw new ArgumentException("Identifier cannot contain spaces.");

                SymbolContainer scopedContainer = currentContainer;
                string[] parts = identifier.Split(':', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    IScopeSymbol? symbol = scopedContainer.FindSymbol(parts[i]);
                    if (symbol == null)
                        return null;
                    else if (symbol is SymbolContainer symbolContainer)
                    {
                        if (fromGlobalStack && !symbolContainer.IsGloballyNavigable)
                            return null;
                        scopedContainer = symbolContainer;
                    }
                    else
                        return null;
                }

                string finalIdentifier = parts.Last();
                return scopedContainer.FindSymbol(finalIdentifier);
            }

            IScopeSymbol? result = FindSymbolFromContainer(identifier, scopeStack.Peek(), false);
            if(result == null)
            {
                foreach(Module usedModule in usedModuleStack)
                {
                    result = FindSymbolFromContainer(identifier, usedModule, true);
                    if (result != null)
                        break;
                }
            }

            return result;
        }

        public void DeclareSymbol(IScopeSymbol symbol, IAstElement errorReportedElement) => scopeStack.Peek().DeclareSymbol(symbol, errorReportedElement);

        public void NavigateToScope(SymbolContainer symbolContainer)
        {
            if (symbolContainer is Module module)
                usedModuleStack.Push(module);
            scopeStack.Push(symbolContainer);
        }

        public CodeBlock NewCodeBlock(bool isLoop, SourceLocation blockBeginLocation)
        {
            CodeBlock codeBlock = new(scopeStack.Peek(), isLoop, blockBeginLocation);
            NavigateToScope(codeBlock);
            return codeBlock;
        }

        public Module NewModule(string name, bool allowPartial, IAstElement errorReportedElement)
        {
            try
            {
                IScopeSymbol existingSymbol = FindSymbol(name, errorReportedElement);
                if (existingSymbol is Module module)
                {
                    if (allowPartial)
                    {
                        NavigateToScope(module);
                        return module;
                    }
                    else
                        throw new SymbolAlreadyExistsException(existingSymbol, errorReportedElement);
                }
                else
                    throw new SymbolNotModuleException(existingSymbol, errorReportedElement);
            }
            catch(SymbolNotFoundException)
            {
                Module module = new(name, scopeStack.Peek(), errorReportedElement);
                DeclareSymbol(module, errorReportedElement);
                NavigateToScope(module);
                return module;
            }
        }

        public void GoBack()
        {
            SymbolContainer symbolContainer = scopeStack.Pop();
            if (symbolContainer is Module) 
            {
                Module popped = usedModuleStack.Pop();
                Debug.Assert(popped == symbolContainer); 
            }
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class ModuleContainer
    {
        private SymbolMarshaller.Module IRModule;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            IRModule = irBuilder.SymbolMarshaller.NewModule(Identifier, true, this);
            Statements.ForEach((IAstStatement statement) => statement.ForwardTypeDeclare(irBuilder));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRModule);
            Statements.ForEach((IAstStatement statement) => statement.ForwardDeclare(irBuilder));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRModule);
            IRModule.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, Statements));
            irBuilder.SymbolMarshaller.GoBack();
            return IRModule;
        }
    }
}