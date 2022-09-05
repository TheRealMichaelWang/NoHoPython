namespace NoHoPython.Syntax
{
    public sealed class TypeParameter
    {
        public readonly string Identifier;
        public AstType? RequiredImplementedType { get; private set; }

        public TypeParameter(string identifier, AstType? requiredImplementedType)
        {
            Identifier = identifier;
            RequiredImplementedType = requiredImplementedType; 
        }

        public override string ToString() => Identifier + RequiredImplementedType == null ? string.Empty : $": {RequiredImplementedType}";
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

        public override string ToString()
        {
            if (TypeArguments.Count == 0)
                return Identifier;
            else
                return $"{Identifier}<{string.Join(", ", TypeArguments)}>";
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    public sealed partial class ExplicitCast : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }
        
        public IAstValue ToCast { get; private set; }
        public AstType TargetType { get; private set; }
        
        public ExplicitCast(IAstValue toCast, AstType targetType, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            ToCast = toCast;
            TargetType = targetType;
        }

        public override string ToString() => $"{ToCast} as {TargetType}";
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
                    scanner.ScanToken();
                    return new TypeParameter(identifier, parseType());
                }
                else
                    return new TypeParameter(identifier, null);
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
            MatchToken(TokenType.Less);

            List<AstType> typeArguments = new List<AstType>();
            while (true)
            {
                scanner.ScanToken();
                typeArguments.Add(parseType());
                if (scanner.LastToken.Type == TokenType.More)
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

            if (scanner.LastToken.Type == TokenType.Less)
                return new AstType(identifier, parseTypeArguments());
            else
                return new AstType(identifier, new List<AstType>());
        }
    }
}
