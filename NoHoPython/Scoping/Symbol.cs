namespace NoHoPython.Scoping
{
    public interface IScopeSymbol
    {
        public bool IsGloballyNavigable { get; }
        public string Name { get; }
    }
}
