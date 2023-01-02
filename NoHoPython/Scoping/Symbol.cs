using System.Diagnostics;
using System.Text;

namespace NoHoPython.Scoping
{
    public interface IScopeSymbol
    {
        public string Name { get; }

        public SymbolContainer ParentContainer { get; }

        public static string GetAbsolouteName(IScopeSymbol scopeSymbol, IScopeSymbol? masterScope = null)
        {
            StringBuilder id = new();
            id.Append(scopeSymbol.Name);

            SymbolContainer? current = scopeSymbol.ParentContainer;
            while (current != null)
            {
                if (current is IScopeSymbol containerSymbol)
                {
                    if (containerSymbol.Name == string.Empty)
                        break;

                    id.Insert(0, '_');
                    id.Insert(0, containerSymbol.Name);
                    current = containerSymbol.ParentContainer;
                }
                else if (masterScope != null)
                {
                    Debug.Assert(masterScope.Name != string.Empty);
                    id.Insert(0, '_');
                    id.Insert(0, masterScope.Name);
                    break;
                }
                else
                    throw new InvalidOperationException();
            }
            return id.ToString();
        }
    }
}
