using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using System.Diagnostics;

namespace NoHoPython.Scoping
{
    public sealed class SymbolNotFoundException : Exception
    {
        public string Identifier { get; private set; }
        public SymbolContainer ParentContainer;

        public SymbolNotFoundException(string identifier, SymbolContainer parentContainer) : base($"Symbol {identifier} not found.")
        {
            Identifier = identifier;
            ParentContainer = parentContainer;
        }
    }

    public sealed class SymbolAlreadyExistsException : Exception
    {
        public IScopeSymbol ExistingSymbol;
        public SymbolContainer ParentContainer;

        public SymbolAlreadyExistsException(IScopeSymbol existingSymbol, SymbolContainer parentContainer) : base($"Symbol {existingSymbol.Name} already exists.")
        {
            ExistingSymbol = existingSymbol;
            ParentContainer = parentContainer;
        }
    }

    public sealed class SymbolNotModuleException : Exception
    {
        public IScopeSymbol Symbol;
        public SymbolContainer ParentContainer;

        public SymbolNotModuleException(IScopeSymbol symbol, SymbolContainer parentContainer) : base($"Symbol {symbol.Name} isn't a module.")
        {
            Symbol = symbol;
            ParentContainer = parentContainer;
        }
    }

    public abstract class SymbolContainer
    {
        private Dictionary<string, IScopeSymbol> symbols;

        public SymbolContainer(List<IScopeSymbol> symbols)
        {
            this.symbols = new Dictionary<string, IScopeSymbol>(symbols.Count);
            foreach (IScopeSymbol symbol in symbols)
                this.symbols.Add(symbol.Name, symbol);
        }

        public virtual IScopeSymbol? FindSymbol(string identifier)
        {
            return symbols.ContainsKey(identifier) ? symbols[identifier] : null;
        }

        public virtual void DeclareSymbol(IScopeSymbol symbol)
        {
            if (FindSymbol(symbol.Name) == null)
                symbols.Add(symbol.Name, symbol);
            throw new SymbolAlreadyExistsException(symbols[symbol.Name], this);
        }
    }

    public sealed class SymbolMarshaller : SymbolContainer
    {
        public sealed class Module : SymbolContainer, IScopeSymbol, IRStatement
        {
            public bool IsGloballyNavigable => true;
            public string Name { get; private set; }

            private List<IRStatement>? statements;

            public Module(string name, List<IScopeSymbol> symbols) : base(symbols)
            {
                Name = name;
                statements = null;
            }

            public void DelayedLinkSetStatements(List<IRStatement> statements)
            {
                if (this.statements != null)
                    throw new InvalidOperationException();
                this.statements = statements;
            }
        }

        private Stack<Module> usedModuleStack;
        private Stack<SymbolContainer> scopeStack;

        public SymbolMarshaller(List<IScopeSymbol> symbols) : base(symbols)
        {
            usedModuleStack = new Stack<Module>();
            scopeStack = new Stack<SymbolContainer>();
            scopeStack.Push(this);
        }

        public override IScopeSymbol FindSymbol(string identifier)
        {
            static IScopeSymbol FindSymbolFromContainer(string identifier, SymbolContainer currentContainer, bool fromGlobalStack)
            {
                if (identifier.Contains(' '))
                    throw new ArgumentException("Identifier cannot contain spaces.");

                SymbolContainer scopedContainer = currentContainer;
                string[] parts = identifier.Split(':', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    IScopeSymbol? symbol = scopedContainer.FindSymbol(parts[i]);
                    scopedContainer = symbol == null || (fromGlobalStack && !symbol.IsGloballyNavigable)
                        ? throw new SymbolNotFoundException(parts[i], scopedContainer)
                        : symbol is SymbolContainer symbolContainer ? symbolContainer : throw new SymbolNotModuleException(symbol, scopedContainer);
                }

                string finalIdentifier = parts.Last();
                IScopeSymbol? result = scopedContainer.FindSymbol(finalIdentifier);
                return result == null || (fromGlobalStack && !result.IsGloballyNavigable)
                    ? throw new SymbolNotFoundException(finalIdentifier, scopedContainer)
                    : result;
            }

            try
            {
                return FindSymbolFromContainer(identifier, scopeStack.Peek(), false);
            }
            catch (SymbolNotFoundException)
            {

                foreach (Module symbolContainer in usedModuleStack)
                    try
                    {
                        return FindSymbolFromContainer(identifier, symbolContainer, true);
                    }
                    catch (SymbolNotFoundException)
                    {
                        continue;
                    }
            }
            throw new SymbolNotFoundException(identifier, scopeStack.Peek());
        }

        public override void DeclareSymbol(IScopeSymbol symbol) => scopeStack.Peek().DeclareSymbol(symbol);

        public void DeclareGlobalSymbol(IScopeSymbol symbol) => base.DeclareSymbol(symbol);

        public void NavigateToScope(SymbolContainer symbolContainer)
        {
            if (symbolContainer is Module module)
                usedModuleStack.Push(module);
            scopeStack.Push(symbolContainer);
        }

        public CodeBlock NewCodeBlock()
        {
            CodeBlock codeBlock = new(scopeStack.Peek(), new List<Variable>());
            NavigateToScope(codeBlock);
            return codeBlock;
        }

        public Module NewModule(string name)
        {
            Module module = new(name, new List<IScopeSymbol>());
            DeclareGlobalSymbol(module);
            NavigateToScope(module);
            return module;
        }

        public void GoBack()
        {
            SymbolContainer symbolContainer = scopeStack.Pop();
            if (symbolContainer is Module)
                Debug.Assert(usedModuleStack.Pop() == symbolContainer);
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class ModuleContainer
    {
        private SymbolMarshaller.Module IRModule;

        public void ForwardTypeDeclare(IRProgramBuilder irBuilder)
        {
            IRModule = irBuilder.SymbolMarshaller.NewModule(Identifier);
            Statements.ForEach((IAstStatement statement) => statement.ForwardTypeDeclare(irBuilder));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public void ForwardDeclare(IRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRModule);
            Statements.ForEach((IAstStatement statement) => statement.ForwardDeclare(irBuilder));
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(IRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRModule);
            IRModule.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, Statements));
            irBuilder.SymbolMarshaller.GoBack();
            return IRModule;
        }
    }
}