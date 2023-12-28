using NoHoPython.Syntax.Statements;
using System.Text;

namespace NoHoPython.Syntax.Statements
{
    public static class AttributeTableExtensions
    {
        public static string ToString(this Dictionary<string, string?> attributeTable, int indent)
        {
            if (attributeTable.Count == 0)
                return string.Empty;

            StringBuilder builder = new();

            builder.AppendLine($"\n{IAstStatement.Indent(indent)}attributes:");
            foreach (KeyValuePair<string, string?> attribute in attributeTable)
            {
                if (attribute.Value != null)
                    builder.AppendLine($"{IAstStatement.Indent(indent + 1)}{attribute.Key} : {attribute.Value}");
                else
                    builder.AppendLine($"{IAstStatement.Indent(indent + 1)}{attribute.Key}");
            }

            return builder.ToString();
        }
    }

    public sealed partial class EnumDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<AstType> RequiredImplementedInterfaces;
        public readonly List<AstType> Options;

        public readonly Dictionary<string, string?> Attributes;

#pragma warning disable CS8618 // IREnumDeclaration initialized upon IR generation
        public EnumDeclaration(string identifier, List<TypeParameter> typeParameters, List<AstType> requiredImplementedInterfaces, List<AstType> options, Dictionary<string, string?> attributes, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            TypeParameters = typeParameters;
            RequiredImplementedInterfaces = requiredImplementedInterfaces;
            Options = options;
            Attributes = attributes;
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

            builder.Append(Attributes.ToString(indent));

            return builder.ToString();
        }
    }

    public sealed partial class InterfaceDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<(AstType, string)> Properties;

#pragma warning disable CS8618 //IRInterfaceDeclaration initialized upon generating IR 
        public InterfaceDeclaration(string identifier, List<TypeParameter> typeParameters, List<(AstType, string)> properties, SourceLocation sourceLocation)
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

            foreach ((AstType, string) property in Properties)
                builder.Append($"\n{IAstStatement.Indent(indent + 1)}{property.Item1.ToString()} {property.Item2}");
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

        public string Identifier { get; private set; }
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<RecordProperty> Properties;
        public readonly List<ProcedureDeclaration> MessageRecievers;
        public bool PassByReference { get; private set; }

#pragma warning disable CS8618 // IRRecordDeclaration is initialized upon generating IR
        public RecordDeclaration(string identifier, List<TypeParameter> typeParameters, List<RecordProperty> properties, List<ProcedureDeclaration> messageRecievers, bool passByReference, SourceLocation sourceLocation)
#pragma warning restore CS8618
        {
            Identifier = identifier;
            TypeParameters = typeParameters;
            Properties = properties;
            MessageRecievers = messageRecievers;
            PassByReference = passByReference;
            SourceLocation = sourceLocation;
        }

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            builder.Append(IAstStatement.Indent(indent));
            if (PassByReference)
                builder.Append("ref ");
            builder.Append("class ");
            builder.Append(Identifier);
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

    public sealed partial class ForeignCDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public string Identifier { get; private set; }
        public string CSource { get; private set; }
        public readonly List<TypeParameter> TypeParameters;
        public readonly Dictionary<string, string?> Attributes;
        public List<(AstType, string, string?)> Properties;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ForeignCDeclaration(string identifier, string cSource, List<TypeParameter> typeParameters, Dictionary<string, string?> attributes, List<(AstType, string, string?)> properties, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            Identifier = identifier;
            CSource = cSource;
            TypeParameters = typeParameters;
            Attributes = attributes;
            Properties = properties;
            SourceLocation = sourceLocation;
        }

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            builder.Append($"{IAstStatement.Indent(indent)}cdef {Identifier}");
            if (TypeParameters.Count > 0)
                builder.Append($"<{string.Join(", ", TypeParameters)}>");
            builder.Append($" \"{CSource}\":");

            foreach ((AstType, string, string?) property in Properties)
            {
                builder.Append($"\n{IAstStatement.Indent(indent + 1)}{property.Item1.ToString()} {property.Item2}");
                if (property.Item3 != null)
                    builder.Append($" \"{property.Item3}\"");
            }

            builder.AppendLine(Attributes.ToString(indent));

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

    public sealed partial class TypedefDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Identifier;
        public readonly List<TypeParameter> TypeParameters;

        public AstType DefinedType { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TypedefDeclaration(string identifier, List<TypeParameter> typeParameters, AstType definedType, SourceLocation sourceLocation)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Identifier = identifier;
            TypeParameters = typeParameters;
            DefinedType = definedType;
            SourceLocation = sourceLocation;
        }

        public string ToString(int indent)
        {
            StringBuilder builder = new();
            builder.Append($"{IAstStatement.Indent(indent)}def {Identifier}");
            if (TypeParameters.Count > 0)
                builder.Append($"<{string.Join(", ", TypeParameters)}>");
            builder.Append(' ');
            builder.AppendLine(DefinedType.ToString());
            return builder.ToString();
        }
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

            List<AstType> requiredImplementedInterfaces = new();
            if (scanner.LastToken.Type != TokenType.Newline)
                while(true)
                {
                    requiredImplementedInterfaces.Add(ParseType());
                    if (scanner.LastToken.Type != TokenType.Comma)
                    {
                        MatchAndScanToken(TokenType.Colon);
                        MatchAndScanToken(TokenType.Newline);
                        break;
                    }
                    else
                        scanner.ScanToken();
                }
            else
                scanner.ScanToken();

            List<AstType> Options = ParseBlock(() => ParseType(false));
            return new EnumDeclaration(identifier, typeParameters, requiredImplementedInterfaces, Options, ParseAttributesTable(), location);
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

            List<(AstType, string)> properties = ParseBlock(() =>
            {
                AstType type = ParseType();
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();
                return (type, identifier);
            });
            return new InterfaceDeclaration(identifier, typeParameters, properties, location);
        }

        private RecordDeclaration ParseRecordDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            bool passByReference = false;
            if(scanner.LastToken.Type == TokenType.Reference)
            {
                passByReference = true;
                scanner.ScanToken();
            }

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
                bool isReadonly = false;
                switch (scanner.LastToken.Type)
                {
                    case TokenType.Define:
                    case TokenType.Pure:
                    case TokenType.Impure:
                        procedures.Add((ProcedureDeclaration)ParseProcedureDeclaration(true));
                        return null;
                    case TokenType.AffectsArgs:
                    case TokenType.AffectsCaptured:
                        if (scanner.PeekToken().Type == TokenType.Less)
                            break;
                        else
                        {
                            procedures.Add((ProcedureDeclaration)ParseProcedureDeclaration(true));
                            return null;
                        }
                    case TokenType.Readonly:
                        isReadonly = true;
                        scanner.ScanToken();
                        break;
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
            return new RecordDeclaration(identifier, typeParameters, properties, procedures, passByReference, location);
        }

        private ForeignCDeclaration ParseForeignCDeclaration(string identifier, List<TypeParameter> typeParameters, SourceLocation sourceLocation)
        {
            MatchToken(TokenType.StringLiteral);
            string csource = scanner.LastToken.Identifier;
            scanner.ScanToken();

            if (scanner.LastToken.Type == TokenType.Colon)
            {
                scanner.ScanToken();
                MatchAndScanToken(TokenType.Newline);

                List<(AstType, string, string?)> properties = ParseBlock(() =>
                {
                    AstType type = ParseType();
                    MatchToken(TokenType.Identifier);
                    string identifier = scanner.LastToken.Identifier;
                    scanner.ScanToken();

                    string? accessSource = null;
                    if (scanner.LastToken.Type == TokenType.StringLiteral)
                    {
                        accessSource = scanner.LastToken.Identifier;
                        scanner.ScanToken();
                    }
                    return (type, identifier, accessSource);
                });
                
                return new ForeignCDeclaration(identifier, csource, typeParameters, ParseAttributesTable(), properties, sourceLocation);
            }
            else
            {
                return new ForeignCDeclaration(identifier, csource, typeParameters, ParseAttributesTable(), new(), sourceLocation);
            }
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

        private Dictionary<string, string?> ParseAttributesTable()
        {
            if (!NextLine(TokenType.Attributes))
                return new();

            scanner.ScanToken();

            MatchAndScanToken(TokenType.Colon);
            MatchAndScanToken(TokenType.Newline);

            return new(ParseBlock(() =>
            {
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();

                if (scanner.LastToken.Type != TokenType.Colon)
                    return new KeyValuePair<string, string?>(identifier, null);
                scanner.ScanToken();

                MatchToken(TokenType.StringLiteral);
                string value = scanner.LastToken.Identifier;
                scanner.ScanToken();
                return new KeyValuePair<string, string?>(identifier, value);
            }));
        }
    }
}