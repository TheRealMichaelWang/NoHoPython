using NoHoPython.Syntax.Statements;

namespace NoHoPython.Syntax.Statements
{
    public sealed partial class ProcedureDeclaration : IAstStatement
    {
        public sealed class ProcedureParameter
        {
            public readonly string Identifier;
            public AstType Type { get; private set; }

            public ProcedureParameter(string identifier, AstType type)
            {
                Identifier = identifier;
                Type = type;
            }
        }

        public SourceLocation SourceLocation { get; private set; }

        public readonly string Name;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<ProcedureParameter> Parameters;
        public readonly List<IAstStatement> Statements;

        public ProcedureDeclaration(string name, List<TypeParameter> typeParameters, List<ProcedureParameter> parameters, List<IAstStatement> statements, SourceLocation sourceLocation)
        {
            Name = name;
            Statements = statements;
            SourceLocation = sourceLocation;
            Parameters = parameters;
            TypeParameters = typeParameters;
        }
    }

    public sealed partial class ReturnStatement : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue ReturnValue { get; private set; }

        public ReturnStatement(IAstValue returnValue, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            ReturnValue = returnValue;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    public sealed partial class NamedFunctionCall : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Name;
        public readonly List<AstType> TypeArguments;
        public readonly List<IAstValue> Arguments;

        public NamedFunctionCall(string name, List<AstType> typeArguments, List<IAstValue> arguments, SourceLocation sourceLocation)
        {
            Name = name;
            TypeArguments = typeArguments;
            Arguments = arguments;
            SourceLocation = sourceLocation;
        }
    }

    public sealed partial class AnonymousFunctionCall : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue ProcedureValue { get; private set; }
        public readonly List<IAstValue> Arguments;

        public AnonymousFunctionCall(IAstValue procedureValue, List<IAstValue> arguments, SourceLocation sourceLocation)
        {
            ProcedureValue = procedureValue;
            Arguments = arguments;
            SourceLocation= sourceLocation;
        }
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private ProcedureDeclaration parseProcedureDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Define);

            MatchToken(TokenType.Identifier);
            string identifer = scanner.LastToken.Identifier;
            scanner.ScanToken();

            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.OpenBrace) ? parseTypeParameters() : new List<TypeParameter>();
            
            MatchAndScanToken(TokenType.OpenParen);

            List<ProcedureDeclaration.ProcedureParameter> parameters = new List<ProcedureDeclaration.ProcedureParameter>();
            while(scanner.LastToken.Type != TokenType.CloseParen)
            {
                AstType paramType = parseType();
                MatchToken(TokenType.Identifier);
                parameters.Add(new ProcedureDeclaration.ProcedureParameter(scanner.LastToken.Identifier, paramType));
                scanner.ScanToken();

                if (scanner.LastToken.Type != TokenType.CloseParen)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);
            return new ProcedureDeclaration(identifer, typeParameters, parameters, parseCodeBlock(), location);
        }
    }
}