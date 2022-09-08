using NoHoPython.Syntax.Statements;
using System.Text;

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

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            _ = builder.Append($"{IAstStatement.Indent(indent)}enum {Identifier}");
            if (TypeParameters.Count > 0)
                _ = builder.Append($"<{string.Join(", ", TypeParameters)}>");
            _ = builder.Append(':');

            foreach (AstType option in Options)
                _ = builder.Append($"\n{IAstStatement.Indent(indent + 1)}{option}");
            return builder.ToString();
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

            public override string ToString() => $"{Type} {Identifier}";
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

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            _ = builder.Append($"{IAstStatement.Indent(indent)}interface {Identifier}");
            if (TypeParameters.Count > 0)
                _ = builder.Append($"<{string.Join(", ", TypeParameters)}>");
            _ = builder.Append(':');

            foreach (InterfaceProperty property in Properties)
                _ = builder.Append($"\n{IAstStatement.Indent(indent + 1)}{property}");
            return builder.ToString();
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

            public override string ToString()
            {
                StringBuilder builder = new();
                if (IsReadOnly)
                    _ = builder.Append("readonly ");

                _ = builder.Append(Type);
                _ = builder.Append(' ');
                _ = builder.Append(Identifier);

                if (DefaultValue != null)
                {
                    _ = builder.Append(" = ");
                    _ = builder.Append(DefaultValue.ToString());
                }
                return builder.ToString();
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

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            _ = builder.Append($"{IAstStatement.Indent(indent)}class {Identifier}");
            if (TypeParameters.Count > 0)
                _ = builder.Append($"<{string.Join(", ", TypeParameters)}>");
            _ = builder.Append(':');
            foreach (RecordProperty property in Properties)
                _ = builder.Append($"\n{IAstStatement.Indent(indent + 1)}{property}");
            return builder.ToString();
        }
    }

    public sealed partial class ModuleContainer : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<IAstStatement> Statements;

        public ModuleContainer(string identifier, List<IAstStatement> statements, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            Statements = statements;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}module {Identifier}\n{IAstStatement.BlockToString(indent, Statements)}";
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

            _ = scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? parseTypeParameters() : new List<TypeParameter>();

            List<AstType> Options = parseBlock(parseType);
            return new EnumDeclaration(identifier, typeParameters, Options, location);
        }

        private InterfaceDeclaration parseInterfaceDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Interface);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            _ = scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? parseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<InterfaceDeclaration.InterfaceProperty> interfaceProperties = parseBlock(() =>
            {
                AstType type = parseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                _ = scanner.ScanToken();
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

            _ = scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? parseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<ProcedureDeclaration> procedures = new();
            List<RecordDeclaration.RecordProperty> properties = parseBlock(() =>
            {
                if (scanner.LastToken.Type == TokenType.Define)
                {
                    procedures.Add(parseProcedureDeclaration());
                    return null;
                }

                bool isReadonly = false;
                if (scanner.LastToken.Type == TokenType.Readonly)
                {
                    isReadonly = true;
                    _ = scanner.ScanToken();
                }

                AstType type = parseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                _ = scanner.ScanToken();

                if (scanner.LastToken.Type == TokenType.Set)
                {
                    _ = scanner.ScanToken();
                    return new RecordDeclaration.RecordProperty(type, identifier, isReadonly, parseExpression());
                }
                else
                    return new RecordDeclaration.RecordProperty(type, identifier, isReadonly, null);
            });
            return new RecordDeclaration(identifier, typeParameters, properties, procedures, location);
        }

        private ModuleContainer parseModule()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Module);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;
            _ = scanner.ScanToken();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<IAstStatement> statements = parseBlock(parseTopLevel);
            return new ModuleContainer(identifier, statements, location);
        }
    }
}