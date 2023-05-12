using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;

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

        public override string ToString() => Identifier + (RequiredImplementedType == null ? string.Empty : $": {RequiredImplementedType}");

        public Typing.TypeParameter ToIRTypeParameter(AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (RequiredImplementedType == null)
                return new(Identifier, null, irBuilder.CurrentMasterScope, errorReportedElement);
            else
            {
                IType type = RequiredImplementedType.ToIRType(irBuilder, errorReportedElement);
                if (type is InterfaceType interfaceType)
                    return new(Identifier, interfaceType, irBuilder.CurrentMasterScope, errorReportedElement);
                else
                    throw new UnexpectedTypeException(type, errorReportedElement);
            }
        }
    }

    public sealed partial class AstType
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
            return TypeArguments.Count == 0 ? Identifier : $"{Identifier}<{string.Join(", ", TypeArguments)}>";
        }

        public void MatchTypeArgCount(int expected, IAstElement errorReportedElement)
        {
            if (TypeArguments.Count != expected)
                throw new UnexpectedTypeArgumentsException(expected, TypeArguments.Count, errorReportedElement);
        }

        public IType ToIRType(AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            List<IType> typeArguments = TypeArguments.ConvertAll((AstType argument) => argument.ToIRType(irBuilder, errorReportedElement));

            switch (Identifier)
            {
                case "char":
                case "chr":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new CharacterType();
                case "bool":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new BooleanType();
                case "int":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new IntegerType();
                case "dec":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new DecimalType();
                case "array":
                    MatchTypeArgCount(1, errorReportedElement);
                    return new ArrayType(typeArguments[0]);
                case "tuple":
                    return typeArguments.Count < 2
                        ? throw new UnexpectedTypeArgumentsException(typeArguments.Count, errorReportedElement)
                        : new TupleType(typeArguments);
                case "handle":
                case "ptr":
                case "pointer":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new HandleType();
                case "nothing":
                case "void":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new NothingType();
                case "fn":
                case "proc":
                    return typeArguments.Count < 1
                        ? throw new UnexpectedTypeArgumentsException(typeArguments.Count, errorReportedElement)
                        : new ProcedureType(typeArguments[0], typeArguments.GetRange(1, typeArguments.Count - 1));
                default:
                    {
                        IScopeSymbol typeSymbol = irBuilder.SymbolMarshaller.FindSymbol(Identifier, errorReportedElement);
                        if (typeSymbol is Typing.TypeParameter typeParameter)
                        {
                            MatchTypeArgCount(0, errorReportedElement);
                            if (irBuilder.ScopedProcedures.Count > 0)
                                irBuilder.ScopedProcedures.Peek().SanitizeTypeParameter(typeParameter);
                            return new TypeParameterReference(typeParameter);
                        }
                        else if (typeSymbol is IntermediateRepresentation.Statements.RecordDeclaration recordDeclaration)
                            return new RecordType(recordDeclaration, typeArguments, errorReportedElement);
                        else if (typeSymbol is IntermediateRepresentation.Statements.InterfaceDeclaration interfaceDeclaration)
                            return new InterfaceType(interfaceDeclaration, typeArguments, errorReportedElement);
                        else if (typeSymbol is IntermediateRepresentation.Statements.EnumDeclaration enumDeclaration)
                            return new EnumType(enumDeclaration, typeArguments, errorReportedElement);
                        else if (typeSymbol is IntermediateRepresentation.Statements.TypedefDeclaration typedefDeclaration)
                            return TypedefToIRType(typedefDeclaration, typeArguments, irBuilder, errorReportedElement);
                        else if (typeSymbol is IType symbolType)
                            return symbolType;
                        throw new NotATypeException(typeSymbol, errorReportedElement);
                    }
            }
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    public sealed class ExplicitCast : IAstValue
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

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) 
        {
            IType targetType = TargetType.ToIRType(irBuilder, this);
            return ArithmeticCast.CastTo(ToCast.GenerateIntermediateRepresentationForValue(irBuilder, targetType, willRevaluate), targetType);
        }
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private List<TypeParameter> ParseTypeParameters()
        {
            TypeParameter ParseTypeParameter()
            {
                string identifier;
                if (scanner.LastToken.Type == TokenType.Nothing)
                    identifier = "nothing";
                else
                {
                    MatchToken(TokenType.Identifier);
                    identifier = scanner.LastToken.Identifier;
                }
                scanner.ScanToken();

                if (scanner.LastToken.Type == TokenType.Colon)
                {
                    scanner.ScanToken();
                    return new TypeParameter(identifier, ParseType());
                }
                else
                    return new TypeParameter(identifier, null);
            }

            MatchToken(TokenType.Less);

            List<TypeParameter> typeParameters = new();
            while (true)
            {
                scanner.ScanToken();
                typeParameters.Add(ParseTypeParameter());

                if (scanner.LastToken.Type == TokenType.More)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeParameters;
        }

        private List<AstType> ParseTypeArguments()
        {
            MatchToken(TokenType.Less);

            List<AstType> typeArguments = new();
            while (true)
            {
                scanner.ScanToken();
                typeArguments.Add(ParseType());
                if (scanner.LastToken.Type == TokenType.More)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeArguments;
        }

        private AstType ParseType()
        {
            string identifier;
            if (scanner.LastToken.Type == TokenType.Nothing)
            {
                identifier = "nothing";
                scanner.ScanToken();
            }
            else
                identifier = ParseIdentifier();

            return scanner.LastToken.Type == TokenType.Less
                ? new AstType(identifier, ParseTypeArguments())
                : new AstType(identifier, new List<AstType>());
        }
    }
}
