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

            public override string ToString() => $"{Type} {Identifier}";
        }

        public SourceLocation SourceLocation { get; private set; }

        public readonly string Name;
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<ProcedureParameter> Parameters;
        public readonly List<IAstStatement> Statements;

        public AstType? AnnotatedReturnType { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProcedureDeclaration(string name, List<TypeParameter> typeParameters, List<ProcedureParameter> parameters, List<IAstStatement> statements, AstType? annotatedReturnType, SourceLocation sourceLocation)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Name = name;
            Statements = statements;
            SourceLocation = sourceLocation;
            Parameters = parameters;
            AnnotatedReturnType = annotatedReturnType;
            TypeParameters = typeParameters;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}def {Name}({string.Join(", ", Parameters)}){(AnnotatedReturnType != null ? " " + AnnotatedReturnType.ToString() : "")}:\n{IAstStatement.BlockToString(indent, Statements)}";
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

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}return {ReturnValue}";
    }
}

namespace NoHoPython.Syntax.Values
{
    public sealed partial class NamedFunctionCall : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string Name;
        public readonly List<IAstValue> Arguments;

        public NamedFunctionCall(string name, List<IAstValue> arguments, SourceLocation sourceLocation)
        {
            Name = name;
            Arguments = arguments;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"{Name}({string.Join(", ", Arguments)})";

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{this}";
    }

    public sealed partial class AnonymousFunctionCall : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue ProcedureValue { get; private set; }
        public readonly List<IAstValue> Arguments;

        public AnonymousFunctionCall(IAstValue procedureValue, List<IAstValue> arguments, SourceLocation sourceLocation)
        {
            ProcedureValue = procedureValue;
            Arguments = arguments;
            SourceLocation = sourceLocation;
        }

        public override string ToString() => $"{ProcedureValue}({string.Join(", ", Arguments)})";
        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{this}";
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private ProcedureDeclaration ParseProcedureDeclaration()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Define);

            MatchToken(TokenType.Identifier);
            string identifer = scanner.LastToken.Identifier;
            scanner.ScanToken();

            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();

            MatchAndScanToken(TokenType.OpenParen);

            List<ProcedureDeclaration.ProcedureParameter> parameters = new();
            while (scanner.LastToken.Type != TokenType.CloseParen)
            {
                AstType paramType = ParseType();
                MatchToken(TokenType.Identifier);
                parameters.Add(new ProcedureDeclaration.ProcedureParameter(scanner.LastToken.Identifier, paramType));
                scanner.ScanToken();

                if (scanner.LastToken.Type != TokenType.CloseParen)
                    MatchAndScanToken(TokenType.Comma);
            }

            scanner.ScanToken();

            AstType? returnType = null;
            if (scanner.LastToken.Type != TokenType.Colon)
            {
                returnType = ParseType();
                MatchAndScanToken(TokenType.Colon);
            }
            else
                scanner.ScanToken();
            
            MatchAndScanToken(TokenType.Newline);
            return new ProcedureDeclaration(identifer, typeParameters, parameters, ParseCodeBlock(), returnType, location);
        }
    }
}