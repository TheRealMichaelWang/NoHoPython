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

        public Typing.TypeParameter ToIRTypeParameter(IRProgramBuilder irBuilder) => new Typing.TypeParameter(Identifier, RequiredImplementedType == null ? null : RequiredImplementedType.ToIRType(irBuilder));
    }

    public sealed class AstType
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
            if (TypeArguments.Count == 0)
                return Identifier;
            else
                return $"{Identifier}<{string.Join(", ", TypeArguments)}>";
        }

        public IType ToIRType(IRProgramBuilder irBuilder)
        {
            List<IType> typeArguments = TypeArguments.ConvertAll((AstType argument) => argument.ToIRType(irBuilder));
            
            void MatchTypeArgCount(int expected)
            {
                if (typeArguments.Count != expected)
                    throw new UnexpectedTypeArgumentsException(expected, typeArguments.Count);
            }

            switch (Identifier)
            {
                case "char":
                case "chr":
                    MatchTypeArgCount(0);
                    return new CharacterType();
                case "bool":
                    MatchTypeArgCount(0);
                    return new BooleanType();
                case "int":
                    MatchTypeArgCount(0);
                    return new IntegerType();
                case "dec":
                    MatchTypeArgCount(0);
                    return new DecimalType();
                case "array":
                case "mem":
                    MatchTypeArgCount(1);
                    return new ArrayType(typeArguments[0]);
                case "fn":
                case "proc":
                    {
                        if (typeArguments.Count < 1)
                            throw new UnexpectedTypeArgumentsException(typeArguments.Count);
                        return new ProcedureType(typeArguments[0], typeArguments.GetRange(1, typeArguments.Count - 1));
                    }
                default:
                    {
                        IScopeSymbol typeSymbol = irBuilder.SymbolMarshaller.FindSymbol(Identifier);
                        if (typeSymbol is Typing.TypeParameter typeParameter)
                        {
                            MatchTypeArgCount(0);
                            return new TypeParameterReference(typeParameter);
                        }
                        else if (typeSymbol is IntermediateRepresentation.Statements.RecordDeclaration recordDeclaration)
                            return new RecordType(recordDeclaration, typeArguments);
                        else if (typeSymbol is IntermediateRepresentation.Statements.InterfaceDeclaration interfaceDeclaration)
                            return new InterfaceType(interfaceDeclaration, typeArguments);
                        else if (typeSymbol is IntermediateRepresentation.Statements.EnumDeclaration enumDeclaration)
                            return new EnumType(enumDeclaration, typeArguments);
                        throw new NotATypeException(typeSymbol);
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

        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder) => ArithmeticCast.CastTo(ToCast.GenerateIntermediateRepresentationForValue(irBuilder), TargetType.ToIRType(irBuilder));
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private List<TypeParameter> parseTypeParameters()
        {
            TypeParameter parseTypeParameter()
            {
                MatchToken(TokenType.Identifier);
                string identifier = scanner.LastToken.Identifier;
                scanner.ScanToken();

                if (scanner.LastToken.Type == TokenType.Colon)
                {
                    scanner.ScanToken();
                    return new TypeParameter(identifier, parseType());
                }
                else
                    return new TypeParameter(identifier, null);
            }

            MatchToken(TokenType.Less);

            List<TypeParameter> typeParameters = new List<TypeParameter>();
            while (true)
            {
                scanner.ScanToken();
                typeParameters.Add(parseTypeParameter());

                if (scanner.LastToken.Type == TokenType.More)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeParameters;
        }

        private List<AstType> parseTypeArguments()
        {
            MatchToken(TokenType.Less);

            List<AstType> typeArguments = new List<AstType>();
            while (true)
            {
                scanner.ScanToken();
                typeArguments.Add(parseType());
                if (scanner.LastToken.Type == TokenType.More)
                    break;
                else
                    MatchToken(TokenType.Comma);
            }
            scanner.ScanToken();
            return typeArguments;
        }

        private AstType parseType()
        {
            MatchToken(TokenType.Identifier);
            string identifier = scanner.LastToken.Identifier;
            scanner.ScanToken();

            if (scanner.LastToken.Type == TokenType.Less)
                return new AstType(identifier, parseTypeArguments());
            else
                return new AstType(identifier, new List<AstType>());
        }
    }
}
