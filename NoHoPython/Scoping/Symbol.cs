namespace NoHoPython.Scoping
{
    public interface IScopeSymbol
    {
        public bool IsGloballyNavigable { get; }
        public string Name { get; }

        public SymbolContainer? ParentContainer { get; }

        public static string GetAbsolouteName(IScopeSymbol scopeSymbol)
        {
            if (scopeSymbol.ParentContainer == null)
                return scopeSymbol.Name;
            else if (scopeSymbol.ParentContainer is IScopeSymbol parentSymbol)
                return GetAbsolouteName(parentSymbol) + "_" + scopeSymbol.Name;
            else
                throw new InvalidDataException();
        }
    }
}
