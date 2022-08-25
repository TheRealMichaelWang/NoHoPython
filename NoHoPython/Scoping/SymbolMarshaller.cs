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
            foreach(IScopeSymbol symbol in symbols)
                this.symbols.Add(symbol.Name, symbol);
        }

        public virtual IScopeSymbol? FindSymbol(string identifier)
        {
            if (symbols.ContainsKey(identifier))
                return symbols[identifier];
            return null;
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
        public sealed class Module : SymbolContainer, IScopeSymbol
        {
            public bool IsGloballyNavigable => true;
            public string Name { get; private set; }
            
            public Module(string name, List<IScopeSymbol> symbols) : base(symbols)
            {
                Name = name;
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
                    if (symbol == null || (fromGlobalStack && !symbol.IsGloballyNavigable))
                        throw new SymbolNotFoundException(parts[i], scopedContainer);
                    else if (symbol is SymbolContainer symbolContainer)
                        scopedContainer = symbolContainer;
                    else
                        throw new SymbolNotModuleException(symbol, scopedContainer);
                }

                string finalIdentifier = parts.Last();
                IScopeSymbol? result = scopedContainer.FindSymbol(finalIdentifier);
                if (result == null || (fromGlobalStack && !result.IsGloballyNavigable))
                    throw new SymbolNotFoundException(finalIdentifier, scopedContainer);

                return result;
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
            if(symbolContainer is Module module)
                usedModuleStack.Push(module);
            scopeStack.Push(symbolContainer);
        }

        public VariableContainer NewVariableContainer(bool hasParent, List<Variable> existingVariables)
        {
            VariableContainer variableContainer;
            if (hasParent)
                NavigateToScope(variableContainer = new VariableContainer(scopeStack.Peek(), existingVariables));
            else
                NavigateToScope(variableContainer = new VariableContainer(null, existingVariables));
            return variableContainer; 
        }

        public void GoBack()
        {
            SymbolContainer symbolContainer = scopeStack.Pop();
            if (symbolContainer is Module)
                Debug.Assert(usedModuleStack.Pop() == symbolContainer);
        }
    }
}