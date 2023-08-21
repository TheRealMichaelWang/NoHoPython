using NoHoPython.Syntax.Statements;
using NoHoPython.Syntax.Values;
using static NoHoPython.Syntax.Statements.ProcedureDeclaration;

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

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}def {Name}{(TypeParameters.Count > 0 ? $"<{string.Join(", ", TypeParameters)}>" : string.Empty)}({string.Join(", ", Parameters)}){(AnnotatedReturnType != null ? " " + AnnotatedReturnType.ToString() : "")}:\n{IAstStatement.BlockToString(indent, Statements)}";
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

    public sealed partial class AbortStatement : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }

        public IAstValue? AbortMessage { get; private set; }

        public AbortStatement(IAstValue? abortMessage, SourceLocation sourceLocation)
        {
            AbortMessage = abortMessage;
            SourceLocation = sourceLocation;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}abort{(AbortMessage == null ? string.Empty : $" {AbortMessage}")}";
    }

    public sealed partial class ForeignCProcedureDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }
        
        public string Identifier { get; private set; }
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<AstType> ParameterTypes;
        public readonly AstType ReturnType;

#pragma warning disable CS8618 //IR Foreign set during forward declaration
        public ForeignCProcedureDeclaration(string identifier, List<TypeParameter> typeParameters, List<AstType> parameterTypes, AstType returnType, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            TypeParameters = typeParameters;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}cdef {Identifier}({string.Join(", ", ParameterTypes)}) {ReturnType}";
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

namespace NoHoPython.Syntax.Values
{
    public sealed partial class LambdaDeclaration : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly List<ProcedureParameter> Parameters;
        public IAstValue ReturnExpression { get; private set; }

        public LambdaDeclaration(List<ProcedureParameter> parameters, IAstValue returnExpression, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Parameters = parameters;
            ReturnExpression = returnExpression;
        }

        public override string ToString() => $"lambda{(Parameters.Count > 0 ? " " + string.Join(", ", Parameters) : "")}: {ReturnExpression}";
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private IAstStatement ParseProcedureDeclaration(bool mustBeProcedure = false)
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.Define);

            MatchToken(TokenType.Identifier);
            string identifer = scanner.LastToken.Identifier;
            scanner.ScanToken();

            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();

            if (mustBeProcedure)
                MatchAndScanToken(TokenType.OpenParen);
            else
            {
                if (scanner.LastToken.Type != TokenType.OpenParen)
                    return new TypedefDeclaration(identifer, typeParameters, ParseType(), location);
                scanner.ScanToken();
            }

            List<ProcedureParameter> parameters = new();
            while (scanner.LastToken.Type != TokenType.CloseParen)
            {
                AstType paramType = ParseType();
                MatchToken(TokenType.Identifier);
                parameters.Add(new(scanner.LastToken.Identifier, paramType));
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

        private IAstStatement ParseCDefine()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.CDefine);
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;
            scanner.ScanToken();

            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();

            if (scanner.LastToken.Type == TokenType.OpenParen)
            {
                MatchAndScanToken(TokenType.OpenParen);
                List<AstType> parameters = new();
                while (scanner.LastToken.Type != TokenType.CloseParen)
                {
                    parameters.Add(ParseType());
                    if (scanner.LastToken.Type != TokenType.CloseParen)
                        MatchAndScanToken(TokenType.Comma);
                }
                scanner.ScanToken();
                return new ForeignCProcedureDeclaration(identifier, typeParameters, parameters, ParseType(), location);
            }
            else if (scanner.LastToken.Type == TokenType.StringLiteral)
                return ParseForeignCDeclaration(identifier, typeParameters, location);
            else
            {
                if (scanner.LastToken.Type == TokenType.Newline)
                    return new CSymbolDeclaration(identifier, null, location);
                else
                    return new CSymbolDeclaration(identifier, ParseType(), location);
            }
        }

        private LambdaDeclaration ParseLambdaDeclaration(SourceLocation location)
        {
            MatchAndScanToken(TokenType.Lambda);
            
            List<ProcedureParameter> parameters = new();
            while(scanner.LastToken.Type != TokenType.Colon)
            {
                AstType paramType = ParseType();
                MatchToken(TokenType.Identifier);
                parameters.Add(new(scanner.LastToken.Identifier, paramType));
                scanner.ScanToken();

                if (scanner.LastToken.Type != TokenType.Colon)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();

            return new LambdaDeclaration(parameters, ParseExpression(), location);
        }
    }
}