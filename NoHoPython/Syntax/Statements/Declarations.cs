using NoHoPython.Syntax.Statements;

namespace NoHoPython.Syntax.Statements
{
    public sealed partial class EnumDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<AstType> Options;

        public EnumDeclaration(string identifier, List<TypeParameter> typeParameters, List<AstType> options, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            TypeParameters = typeParameters;
            Options = options;
        }
    }

    public sealed partial class InterfaceDeclaration : IAstStatement
    {
        public sealed class InterfaceProperty
        {
            public AstType Type { get; private set; }
            public readonly string Identifier;

            public InterfaceProperty(AstType type, string identifier)
            {
                Type = type;
                Identifier = identifier;
            }
        }

        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<InterfaceProperty> Properties;

        public InterfaceDeclaration(string identifier, List<TypeParameter> typeParameters, List<InterfaceProperty> properties, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            TypeParameters = typeParameters;
            Properties = properties;
        }
    }

    public sealed partial class RecordDeclaration : IAstStatement
    {
        public sealed class RecordProperty
        {
            public AstType Type { get; private set; }
            public readonly string Identifier;

            public bool IsReadOnly { get; private set; }
            public IAstValue? DefaultValue { get; private set; }

            public RecordProperty(AstType type, string identifier, bool isReadOnly, IAstValue? defaultValue)
            {
                Type = type;
                Identifier = identifier;
                IsReadOnly = isReadOnly;
                DefaultValue = defaultValue;
            }
        }

        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<RecordProperty> Properties;
        public readonly List<ProcedureDeclaration> MessageRecievers;

        public RecordDeclaration(string identifier, List<TypeParameter> typeParameters, List<RecordProperty> properties, List<ProcedureDeclaration> messageRecievers, SourceLocation sourceLocation)
        {
            Identifier = identifier;
            TypeParameters = typeParameters;
            Properties = properties;
            MessageRecievers = messageRecievers;
            SourceLocation = sourceLocation;
        }
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private EnumDeclaration parseEnumDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Enum);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.OpenBrace) ? parseTypeParameters() : new List<TypeParameter>();

            List<AstType> Options = parseBlock(parseType);
            return new EnumDeclaration(identifier, typeParameters, Options, location);
        }

        private InterfaceDeclaration parseInterfaceDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Interface);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.OpenBrace) ? parseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<InterfaceDeclaration.InterfaceProperty> interfaceProperties = parseBlock(() =>
            {
                AstType type = parseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();
                return new InterfaceDeclaration.InterfaceProperty(type, identifier);
            });
            return new InterfaceDeclaration(identifier, typeParameters, interfaceProperties, location);
        }

        private RecordDeclaration parseRecordDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Record);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.OpenBrace) ? parseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<ProcedureDeclaration> procedures = new List<ProcedureDeclaration>();
            List<RecordDeclaration.RecordProperty> properties = parseBlock(() =>
            {
                if(scanner.LastToken.Type == TokenType.Define)
                {
                    procedures.Add(parseProcedureDeclaration());
                    return null;
                }
                
                bool isReadonly = false;
                if (scanner.LastToken.Type == TokenType.Readonly)
                {
                    isReadonly = true;
                    scanner.ScanToken();
                }

                AstType type = parseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();

                if (scanner.LastToken.Type == TokenType.Set)
                {
                    scanner.ScanToken();
                    return new RecordDeclaration.RecordProperty(type, identifier, isReadonly, parseExpression());
                }
                else
                    return new RecordDeclaration.RecordProperty(type, identifier, isReadonly, null);
            });
            return new RecordDeclaration(identifier, typeParameters, properties, procedures, location);
        }
    }
}