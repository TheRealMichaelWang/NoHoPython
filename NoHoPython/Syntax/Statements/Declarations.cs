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

#pragma warning disable CS8618 // IREnumDeclaration initialized upon IR generation
        public EnumDeclaration(string identifier, List<TypeParameter> typeParameters, List<AstType> options, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            TypeParameters = typeParameters;
            Options = options;
        }

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            builder.Append($"{IAstStatement.Indent(indent)}enum {Identifier}");
            if (TypeParameters.Count > 0)
                builder.Append($"<{string.Join(", ", TypeParameters)}>");
            builder.Append(':');

            foreach (AstType option in Options)
                builder.Append($"\n{IAstStatement.Indent(indent + 1)}{option}");
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

#pragma warning disable CS8618 //IRInterfaceDeclaration initialized upon generating IR 
        public InterfaceDeclaration(string identifier, List<TypeParameter> typeParameters, List<InterfaceProperty> properties, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            TypeParameters = typeParameters;
            Properties = properties;
        }

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            builder.Append($"{IAstStatement.Indent(indent)}interface {Identifier}");
            if (TypeParameters.Count > 0)
                builder.Append($"<{string.Join(", ", TypeParameters)}>");
            builder.Append(':');

            foreach (InterfaceProperty property in Properties)
                builder.Append($"\n{IAstStatement.Indent(indent + 1)}{property}");
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
                    builder.Append("readonly ");

                builder.Append(Type);
                builder.Append(' ');
                builder.Append(Identifier);

                if (DefaultValue != null)
                {
                    builder.Append(" = ");
                    builder.Append(DefaultValue.ToString());
                }
                return builder.ToString();
            }
        }

        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<RecordProperty> Properties;
        public readonly List<ProcedureDeclaration> MessageRecievers;

#pragma warning disable CS8618 // IRRecordDeclaration is initialized upon generating IR
        public RecordDeclaration(string identifier, List<TypeParameter> typeParameters, List<RecordProperty> properties, List<ProcedureDeclaration> messageRecievers, SourceLocation sourceLocation)
#pragma warning restore CS8618
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
            builder.Append($"{IAstStatement.Indent(indent)}class {Identifier}");
            if (TypeParameters.Count > 0)
                builder.Append($"<{string.Join(", ", TypeParameters)}>");
            builder.Append(':');
            foreach (RecordProperty property in Properties)
                builder.Append($"\n{IAstStatement.Indent(indent + 1)}{property}");

            if (MessageRecievers.Count > 0)
                builder.Append($"\n{IAstStatement.Indent(indent + 1)}...({MessageRecievers.Count} message recievers in {Identifier})...");

            return builder.ToString();
        }
    }

    public sealed partial class ModuleContainer : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<IAstStatement> Statements;

#pragma warning disable CS8618 // IRModule initialized upon generating IR
        public ModuleContainer(string identifier, List<IAstStatement> statements, SourceLocation sourceLocation)
#pragma warning restore CS8618
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
        private EnumDeclaration ParseEnumDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Enum);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<AstType> Options = ParseBlock(ParseType);
            return new EnumDeclaration(identifier, typeParameters, Options, location);
        }

        private InterfaceDeclaration ParseInterfaceDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Interface);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<InterfaceDeclaration.InterfaceProperty> interfaceProperties = ParseBlock(() =>
            {
                AstType type = ParseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();
                return new InterfaceDeclaration.InterfaceProperty(type, identifier);
            });
            return new InterfaceDeclaration(identifier, typeParameters, interfaceProperties, location);
        }

        private RecordDeclaration ParseRecordDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Record);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;

            scanner.ScanToken();
            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<ProcedureDeclaration> procedures = new();
            List<RecordDeclaration.RecordProperty> properties = ParseBlock(() =>
            {
                if (scanner.LastToken.Type == TokenType.Define)
                {
                    procedures.Add(ParseProcedureDeclaration());
                    return null;
                }

                bool isReadonly = false;
                if (scanner.LastToken.Type == TokenType.Readonly)
                {
                    isReadonly = true;
                    scanner.ScanToken();
                }

                AstType type = ParseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();

                if (scanner.LastToken.Type == TokenType.Set)
                {
                    scanner.ScanToken();
                    return new RecordDeclaration.RecordProperty(type, identifier, isReadonly, ParseExpression());
                }
                else
                    return new RecordDeclaration.RecordProperty(type, identifier, isReadonly, null);
            });
            return new RecordDeclaration(identifier, typeParameters, properties, procedures, location);
        }

        private ModuleContainer ParseModule()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Module);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;
            scanner.ScanToken();
            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            List<IAstStatement> statements = ParseBlock(ParseTopLevel);
            return new ModuleContainer(identifier, statements, location);
        }
    }
}