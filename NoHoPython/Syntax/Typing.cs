namespace NoHoPython.Syntax
{
    public sealed class AstType
    {
        public readonly string Identifier;
        public readonly List<AstType> TypeArguments;

        public AstType(string identifier, List<AstType> typeArguments)
        {
            Identifier = identifier;
            TypeArguments = typeArguments;
        }
    }
}

namespace NoHoPython.Syntax.Parsing
{
    public partial class AstParser
    {
        private AstType parseType()
        {
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;
            scanner.ScanToken();

            List<AstType> typeArguments = new List<AstType>();
            if(scanner.LastToken.Type == TokenType.Less)
            {
                while (true) 
                { 
                    scanner.ScanToken();
                    typeArguments.Add(parseType());
                    if (scanner.LastToken.Type == TokenType.More)
                        break;
                    else
                        MatchToken(TokenType.Comma);
                }
            }
            scanner.ScanToken();

            return new AstType(identifier, typeArguments);
        }
    }
}
