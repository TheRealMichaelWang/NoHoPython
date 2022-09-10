using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using System.Diagnostics;

namespace NoHoPython.Scoping
{
    public abstract class SymbolContainer
    {
        private Dictionary<string, IScopeSymbol> symbols;

        public SymbolContainer(List<IScopeSymbol> symbols)
        {
            this.symbols = new Dictionary<string, IScopeSymbol>(symbols.Count);
            foreach (IScopeSymbol symbol in symbols)
                this.symbols.Add(symbol.Name, symbol);
        }

        public virtual IScopeSymbol? FindSymbol(string identifier, Syntax.IAstElement errorReportedElement)
        {
            return symbols.ContainsKey(identifier) ? symbols[identifier] : null;
        }

        public virtual void DeclareSymbol(IScopeSymbol symbol, Syntax.IAstElement errorReportElement)
        {
            if (FindSymbol(symbol.Name, errorReportElement) == null)
            {
                symbols.Add(symbol.Name, symbol);
                return;
            }
            throw new SymbolAlreadyExistsException(symbols[symbol.Name], this, errorReportElement);
        }
    }

    public sealed class SymbolMarshaller
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

        public SymbolMarshaller(List<IScopeSymbol> symbols)
        {
            usedModuleStack = new Stack<Module>();
            scopeStack = new Stack<SymbolContainer>();
            NavigateToScope(new Module("__main__", new List<IScopeSymbol>()));
        }

        public IScopeSymbol FindSymbol(string identifier, Syntax.IAstElement errorReportedElement)
        {
            static IScopeSymbol FindSymbolFromContainer(string identifier, SymbolContainer currentContainer, bool fromGlobalStack, Syntax.IAstElement errorReportedElement)
            {
                if (identifier.Contains(' '))
                    throw new ArgumentException("Identifier cannot contain spaces.");

                SymbolContainer scopedContainer = currentContainer;
                string[] parts = identifier.Split(':', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    IScopeSymbol? symbol = scopedContainer.FindSymbol(parts[i], errorReportedElement);
                    scopedContainer = symbol == null || (fromGlobalStack && !symbol.IsGloballyNavigable)
                        ? throw new SymbolNotFoundException(parts[i], scopedContainer, errorReportedElement)
                        : symbol is SymbolContainer symbolContainer ? symbolContainer : throw new SymbolNotModuleException(symbol, scopedContainer, errorReportedElement);
                }

                string finalIdentifier = parts.Last();
                IScopeSymbol? result = scopedContainer.FindSymbol(finalIdentifier, errorReportedElement);
                return result == null
                    ? throw new SymbolNotFoundException(finalIdentifier, scopedContainer, errorReportedElement)
                    : result;
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

        public void DeclareSymbol(IScopeSymbol symbol, Syntax.IAstElement errorReportedElement) => scopeStack.Peek().DeclareSymbol(symbol, errorReportedElement);

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

        public Module NewModule(string name, Syntax.IAstElement errorReportedElement)
        {
            Module module = new(name, new List<IScopeSymbol>());
            DeclareSymbol(module, errorReportedElement);
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

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder)
        {
            IRModule = irBuilder.SymbolMarshaller.NewModule(Identifier, this);
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