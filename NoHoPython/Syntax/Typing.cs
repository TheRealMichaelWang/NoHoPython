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
        public static readonly Dictionary<string, int> KnownTypeParameterCounts = new()
        {
            {"char", 0},
            {"chr", 0},
            {"bool", 0},
            {"int", 0},
            {"dec", 0},
            {"array", 1},
            {"None", 0},
            {"nothing", 0},
            {"void", 0}
        };

        public static readonly Dictionary<string, int> KnownTypeParameterMinimumCounts = new()
        {
            {"tuple", 2},
            {"handle", 0},
            {"ptr", 0},
            {"pointer", 0},
            {"purefn", 1},
            {"pure", 1},
            {"impure", 1},
            {"global_impure", 1},
            {"global", 1},
            {"fn", 1},
            {"proc", 1},
            {"affects_args", 1},
            {"affects_captured", 1},
        };

        public static readonly Dictionary<string, int> KnownTypeParameterMaximumCounts = new()
        {
            {"array", 1},
            {"handle", 1},
            {"ptr", 1},
            {"pointer", 1}
        };

        public static int GetMinTypeargs(string identifier)
        {
            if (KnownTypeParameterMinimumCounts.ContainsKey(identifier))
                return KnownTypeParameterMinimumCounts[identifier];
            else if (KnownTypeParameterCounts.ContainsKey(identifier))
                return KnownTypeParameterCounts[identifier];
            return 0;
        }

        public static int GetMaxTypeargs(string identifier)
        {
            if (KnownTypeParameterMaximumCounts.ContainsKey(identifier))
                return KnownTypeParameterMaximumCounts[identifier];
            else if (KnownTypeParameterCounts.ContainsKey(identifier))
                return KnownTypeParameterCounts[identifier];
            return int.MaxValue;
        }

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
                case "nothing":
                case "void":
                case "None":
                    MatchTypeArgCount(0, errorReportedElement);
                    return new NothingType();
                case "handle":
                case "ptr":
                case "pointer":
                    if (typeArguments.Count == 0)
                        return Primitive.Handle;

                    MatchTypeArgCount(1, errorReportedElement);
                    return new HandleType(typeArguments[0]);
                case "ref":
                    MatchTypeArgCount(1, errorReportedElement);
                    return new ReferenceType(typeArguments[0], ReferenceType.ReferenceMode.UnreleasedCanRelease);
                case "norelease_ref":
                case "noReleaseRef":
                    MatchTypeArgCount(1, errorReportedElement);
                    return new ReferenceType(typeArguments[0], ReferenceType.ReferenceMode.UnreleasedCannotRelease);
                case "purefn":
                case "pure":
                    return typeArguments.Count < 1
                        ? throw new UnexpectedTypeArgumentsException(typeArguments.Count, errorReportedElement)
                        : new ProcedureType(typeArguments[0], typeArguments.GetRange(1, typeArguments.Count - 1), IntermediateRepresentation.Statements.Purity.Pure);
                case "impure":
                case "global":
                case "global_impure":
                    return typeArguments.Count < 1
                        ? throw new UnexpectedTypeArgumentsException(typeArguments.Count, errorReportedElement)
                        : new ProcedureType(typeArguments[0], typeArguments.GetRange(1, typeArguments.Count - 1), IntermediateRepresentation.Statements.Purity.AffectsGlobals);
                case "fn":
                case "proc":
                case "affects_args":
                case "affectsArgs":
                    return typeArguments.Count < 1
                        ? throw new UnexpectedTypeArgumentsException(typeArguments.Count, errorReportedElement)
                        : new ProcedureType(typeArguments[0], typeArguments.GetRange(1, typeArguments.Count - 1), IntermediateRepresentation.Statements.Purity.OnlyAffectsArguments);
                case "affects_captured":
                case "affectsCaptured":
                    return typeArguments.Count < 1
                        ? throw new UnexpectedTypeArgumentsException(typeArguments.Count, errorReportedElement)
                        : new ProcedureType(typeArguments[0], typeArguments.GetRange(1, typeArguments.Count - 1), IntermediateRepresentation.Statements.Purity.OnlyAffectsArgumentsAndCaptured);
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
                        else if (typeSymbol is IntermediateRepresentation.Statements.ForeignCDeclaration foreignDeclaration)
                            return new ForeignCType(foreignDeclaration, typeArguments, errorReportedElement);
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
            return ArithmeticCast.CastTo(ToCast.GenerateIntermediateRepresentationForValue(irBuilder, targetType, willRevaluate), targetType, irBuilder, true);
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
                scanner.ScanToken(true);

                if (scanner.LastToken.Type == TokenType.Colon)
                {
                    scanner.ScanToken(true);
                    return new TypeParameter(identifier, ParseType(true));
                }
                else
                    return new TypeParameter(identifier, null);
            }

            MatchToken(TokenType.Less);

            List<TypeParameter> typeParameters = new();
            while (true)
            {
                scanner.ScanToken(true);
                typeParameters.Add(ParseTypeParameter());

                if (scanner.LastToken.Type == TokenType.More)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeParameters;
        }

        private List<AstType> ParseTypeArguments(int minTypeargs, int maxTypeargs, bool isTypeArgument = false)
        {
            if (maxTypeargs == 0)
                return new();

            MatchToken(TokenType.Less);
            scanner.ScanToken(true);
            List<AstType> typeArguments = new();
            while(true)
            {
                typeArguments.Add(ParseType(true));

                if (typeArguments.Count == maxTypeargs)
                {
                    MatchToken(TokenType.More);
                    scanner.ScanToken(isTypeArgument);
                    break;
                }
                else if(typeArguments.Count < minTypeargs)
                {
                    MatchToken(TokenType.Comma);
                    scanner.ScanToken(true);
                }
                else if(scanner.LastToken.Type == TokenType.More)
                {
                    scanner.ScanToken(isTypeArgument);
                    break;
                }
                else
                {
                    MatchToken(TokenType.Comma);
                    scanner.ScanToken(true);
                }
            }
            return typeArguments;
        }

        private AstType ParseType(bool isTypeArgument=false)
        {
            string identifier;
            switch(scanner.LastToken.Type) 
            {
                case TokenType.Nothing:
                case TokenType.AffectsArgs:
                case TokenType.AffectsCaptured:
                case TokenType.Pure:
                case TokenType.Impure:
                    identifier = scanner.LastToken.Identifier;
                    scanner.ScanToken(isTypeArgument);
                    break;
                case TokenType.Reference:
                    identifier = "ref";
                    scanner.ScanToken(isTypeArgument);
                    break;
                default:
                    identifier = ParseIdentifier(isTypeArgument);
                    break;
            }

            return scanner.LastToken.Type == TokenType.Less
                ? new AstType(identifier, ParseTypeArguments(AstType.GetMinTypeargs(identifier), AstType.GetMaxTypeargs(identifier), isTypeArgument))
                : new AstType(identifier, new List<AstType>());
        }
    }
}
