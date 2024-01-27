using NoHoPython.Syntax.Statements;
using NoHoPython.Syntax.Values;
using static NoHoPython.Syntax.Statements.ProcedureDeclaration;

namespace NoHoPython.Syntax.Statements
{
    public sealed partial class ProcedureDeclaration : IAstStatement
    {
        public enum Type
        {
            Constructor,
            MessageReceiver,
            Normal
        }

        public sealed partial class ProcedureParameter
        {
            public readonly string Identifier;
            public AstType Type { get; private set; }
            public bool IsReadOnly { get; private set; }

            public ProcedureParameter(string identifier, AstType type, bool isReadOnly)
            {
                Identifier = identifier;
                Type = type;
                IsReadOnly = isReadOnly;
            }

            public override string ToString() => $"{Type} {Identifier}";
        }

        public static string PurityToString(IntermediateRepresentation.Statements.Purity purity) => purity switch
        {
            IntermediateRepresentation.Statements.Purity.Pure => "pure",
            IntermediateRepresentation.Statements.Purity.OnlyAffectsArguments => "affectsArgs",
            IntermediateRepresentation.Statements.Purity.OnlyAffectsArgumentsAndCaptured => "affectsCaptured",
            IntermediateRepresentation.Statements.Purity.AffectsGlobals => "impure",
            _ => throw new InvalidOperationException()
        };

        public SourceLocation SourceLocation { get; private set; }

        public string Name { get; private set; }
        public Type DeclarationType { get; private set; }
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<ProcedureParameter> Parameters;
        public readonly List<IAstStatement> Statements;

        public AstType? AnnotatedReturnType { get; private set; }

        public IntermediateRepresentation.Statements.Purity Purity { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ProcedureDeclaration(string name, List<TypeParameter> typeParameters, List<ProcedureParameter> parameters, Type declarationType, IntermediateRepresentation.Statements.Purity purity, List<IAstStatement> statements, AstType? annotatedReturnType, SourceLocation sourceLocation)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Name = name;
            DeclarationType = declarationType;
            Statements = statements;
            SourceLocation = sourceLocation;
            Parameters = parameters;
            AnnotatedReturnType = annotatedReturnType;
            TypeParameters = typeParameters;
            Purity = purity;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{PurityToString(Purity)} {Name}{(TypeParameters.Count > 0 ? $"<{string.Join(", ", TypeParameters)}>" : string.Empty)}({string.Join(", ", Parameters)}){(AnnotatedReturnType != null ? " " + AnnotatedReturnType.ToString() : "")}:\n{IAstStatement.BlockToString(indent, Statements)}";
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
        public string? CFunctionName { get; private set; }
        public readonly List<TypeParameter> TypeParameters;
        public readonly List<AstType> ParameterTypes;
        public AstType ReturnType { get; private set; }
        public IntermediateRepresentation.Statements.Purity Purity { get; private set; }

#pragma warning disable CS8618 //IR Foreign set during forward declaration
        public ForeignCProcedureDeclaration(string identifier, string? cFunctionName, List<TypeParameter> typeParameters, List<AstType> parameterTypes, AstType returnType, IntermediateRepresentation.Statements.Purity purity, SourceLocation sourceLocation)
#pragma warning restore CS8618 
        {
            SourceLocation = sourceLocation;
            Identifier = identifier;
            CFunctionName = cFunctionName;
            TypeParameters = typeParameters;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;
            Purity = purity;
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

    public sealed partial class StartThread : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly string ToMultiThreadName;

        public StartThread(SourceLocation sourceLocation, string toMultiThreadName)
        {
            SourceLocation = sourceLocation;
            ToMultiThreadName = toMultiThreadName;
        }

        public override string ToString() => $"start {ToMultiThreadName}";
    }
}

namespace NoHoPython.Syntax.Values
{
    public sealed partial class LambdaDeclaration : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }

        public readonly List<ProcedureParameter> Parameters;
        public IAstValue ReturnExpression { get; private set; }
        public IntermediateRepresentation.Statements.Purity Purity { get; private set; }

        public LambdaDeclaration(List<ProcedureParameter> parameters, IAstValue returnExpression, IntermediateRepresentation.Statements.Purity purity, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Parameters = parameters;
            ReturnExpression = returnExpression;
            Purity = purity;
        }

        public override string ToString() => $"{PurityToString(Purity)}{(Parameters.Count > 0 ? " " + string.Join(", ", Parameters) : "")}: {ReturnExpression}";
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private IntermediateRepresentation.Statements.Purity ParsePurityToken(TokenType? defToken, IntermediateRepresentation.Statements.Purity defaultPurity = IntermediateRepresentation.Statements.Purity.OnlyAffectsArgumentsAndCaptured)
        {
            if(scanner.LastToken.Type == defToken)
            {
                scanner.ScanToken();
                return defaultPurity;
            }

            IntermediateRepresentation.Statements.Purity? purity = scanner.LastToken.Type switch
            {
                TokenType.Pure => IntermediateRepresentation.Statements.Purity.Pure,
                TokenType.AffectsArgs => IntermediateRepresentation.Statements.Purity.OnlyAffectsArguments,
                TokenType.AffectsCaptured => IntermediateRepresentation.Statements.Purity.OnlyAffectsArgumentsAndCaptured,
                TokenType.Impure => IntermediateRepresentation.Statements.Purity.AffectsGlobals,
                _ => defToken == null ? null : throw new UnexpectedTokenException(scanner.LastToken, scanner.CurrentLocation)
            };

            if (purity.HasValue)
            {
                scanner.ScanToken();
                return purity.Value;
            }
            else
                return defaultPurity;
        }

        private ProcedureParameter ParseProcedureParameter()
        {
            bool isReadOnly = false;
            if (scanner.LastToken.Type == TokenType.Readonly)
            {
                isReadOnly = true;
                scanner.ScanToken();
            }
            AstType paramType = ParseType();
            MatchToken(TokenType.Identifier);
            ProcedureParameter toret = new(scanner.LastToken.Identifier, paramType, isReadOnly);
            scanner.ScanToken();
            return toret;
        }

        private IAstStatement ParseProcedureDeclaration(bool isRecordMessageReceiver = false)
        {
            SourceLocation location = scanner.CurrentLocation;

            IntermediateRepresentation.Statements.Purity purity = ParsePurityToken(TokenType.Define, isRecordMessageReceiver ? IntermediateRepresentation.Statements.Purity.OnlyAffectsArgumentsAndCaptured : IntermediateRepresentation.Statements.Purity.OnlyAffectsArguments);

            MatchToken(TokenType.Identifier);
            string identifer = scanner.LastToken.Identifier;
            scanner.ScanToken();

            List<TypeParameter> typeParameters = (scanner.LastToken.Type == TokenType.Less) ? ParseTypeParameters() : new List<TypeParameter>();

            if (isRecordMessageReceiver)
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
                parameters.Add(ParseProcedureParameter());
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
            return new ProcedureDeclaration(identifer, typeParameters, parameters, isRecordMessageReceiver ? (identifer == "__init__" ? ProcedureDeclaration.Type.Constructor : ProcedureDeclaration.Type.MessageReceiver) : ProcedureDeclaration.Type.Normal, purity, ParseCodeBlock(), returnType, location);
        }

        private IAstStatement ParseCDefine()
        {
            SourceLocation location = scanner.CurrentLocation;

            MatchAndScanToken(TokenType.CDefine);
            IntermediateRepresentation.Statements.Purity purity = ParsePurityToken(null, IntermediateRepresentation.Statements.Purity.OnlyAffectsArguments);

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
                    if (scanner.LastToken.Type == TokenType.Identifier)
                        scanner.ScanToken();
                    if (scanner.LastToken.Type != TokenType.CloseParen)
                        MatchAndScanToken(TokenType.Comma);
                }
                scanner.ScanToken();
                AstType returnType = ParseType();

                if(scanner.LastToken.Type == TokenType.StringLiteral)
                {
                    string cFunctionName = scanner.LastToken.Identifier;
                    scanner.ScanToken();
                    return new ForeignCProcedureDeclaration(identifier, cFunctionName, typeParameters, parameters, returnType, purity, location);
                }
                return new ForeignCProcedureDeclaration(identifier, null, typeParameters, parameters, returnType, purity, location);
            }
            else if (scanner.LastToken.Type == TokenType.StringLiteral)
                return ParseForeignCDeclaration(identifier, typeParameters, location);
            else
            {
                if (scanner.LastToken.Type == TokenType.Newline)
                    return new CSymbolDeclaration(identifier, null, purity != IntermediateRepresentation.Statements.Purity.Pure, location);
                else
                    return new CSymbolDeclaration(identifier, ParseType(), purity != IntermediateRepresentation.Statements.Purity.Pure, location);
            }
        }

        private LambdaDeclaration ParseLambdaDeclaration(SourceLocation location)
        {
            IntermediateRepresentation.Statements.Purity purity = ParsePurityToken(TokenType.Lambda);
            
            List<ProcedureParameter> parameters = new();
            while(scanner.LastToken.Type != TokenType.Colon)
            {
                parameters.Add(ParseProcedureParameter());
                if (scanner.LastToken.Type != TokenType.Colon)
                    MatchAndScanToken(TokenType.Comma);
            }
            scanner.ScanToken();

            return new LambdaDeclaration(parameters, ParseExpression(), purity, location);
        }
    }
}