namespace NoHoPython.Syntax
{
    public sealed class TypeParameter
    {
        public readonly string Identifier;
        public readonly List<AstType> RequiredImplementedTypes;

        public TypeParameter(string identifier, List<AstType> requiredImplementedTypes)
        {
            Identifier = identifier;
            RequiredImplementedTypes = requiredImplementedTypes;
        }
    }

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
    partial class AstParser
    {
        private List<TypeParameter> parseTypeParameters()
        {
            TypeParameter parseTypeParameter()
            {
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();

                if (scanner.LastToken.Type == TokenType.Colon)
                {
                    List<AstType> requiredImplementedTypes = new List<AstType>();
                    do
                    {
                        scanner.ScanToken();
                        requiredImplementedTypes.Add(parseType());
                    } while (scanner.LastToken.Type == TokenType.Comma);
                    return new TypeParameter(identifier, requiredImplementedTypes);
                }
                else
                    return new TypeParameter(identifier, new List<AstType>());
            }

            MatchToken(TokenType.OpenBrace);

            List<TypeParameter> typeParameters = new List<TypeParameter>();
            while (true)
            {
                scanner.ScanToken();
                typeParameters.Add(parseTypeParameter());

                if (scanner.LastToken.Type == TokenType.CloseBrace)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeParameters;
        }

        private List<AstType> parseTypeArguments()
        {
            MatchToken(TokenType.OpenBrace);

            List<AstType> typeArguments = new List<AstType>();
            while (true)
            {
                scanner.ScanToken();
                typeArguments.Add(parseType());
                if (scanner.LastToken.Type == TokenType.CloseBrace)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeArguments;
        }

        private AstType parseType()
        {
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;
            scanner.ScanToken();

            if (scanner.LastToken.Type == TokenType.OpenBrace)
                return new AstType(identifier, parseTypeArguments());
            else
                return new AstType(identifier, new List<AstType>());
        }
    }
}
