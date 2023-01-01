namespace NoHoPython.Scoping
{
    public interface IScopeSymbol
    {
        public string Name { get; }

        public SymbolContainer ParentContainer { get; }

        public static string GetAbsolouteName(IScopeSymbol scopeSymbol)
        {
            if (scopeSymbol.ParentContainer == null)
                return scopeSymbol.Name;
            else if (scopeSymbol.ParentContainer is IScopeSymbol parentSymbol)
            {
                string parentId = GetAbsolouteName(parentSymbol);
                if (parentId == string.Empty)
                    return scopeSymbol.Name;
                else
                    return parentId + "_" + scopeSymbol.Name;
            }
            else
                throw new InvalidDataException();
        }
    }
}
