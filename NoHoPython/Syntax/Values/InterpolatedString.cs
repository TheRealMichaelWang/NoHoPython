using NoHoPython.Syntax.Values;
using System.Text;

namespace NoHoPython.Syntax.Values
{
    public sealed partial class InterpolatedString : IAstValue
    {
        public SourceLocation SourceLocation { get; set; }

        public readonly List<IAstValue> InterpolatedValues; //only string or IAstValue

        public InterpolatedString(List<object> interpolatedValues, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;

            if (!interpolatedValues.TrueForAll((value) => value is string || value is IAstValue))
                throw new InvalidOperationException();
            InterpolatedValues = interpolatedValues.ConvertAll((interpolatedValue) => interpolatedValue is IAstValue astValue ? astValue : new StringLiteral((string)interpolatedValue, sourceLocation));
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append("$\"");
            foreach (IAstValue value in InterpolatedValues)
            {
                if (value is StringLiteral literal)
                    builder.Append(literal.String);
                else
                    builder.Append($"{{{value}}}");
            }
            builder.Append('\"');
            return builder.ToString();
        }
    }
}

namespace NoHoPython.Syntax.Parsing
{
    partial class AstParser
    {
        private InterpolatedString ParseInterpolated(SourceLocation location)
        {
            List<object> interpolatedValues = new();
            MatchToken(TokenType.InterpolatedStart);
            interpolatedValues.Add(scanner.LastToken.Identifier);

            scanner.ScanToken();
            while(scanner.LastToken.Type != TokenType.InterpolatedEnd)
            {
                if (scanner.LastToken.Type == TokenType.InterpolatedMiddle)
                {
                    interpolatedValues.Add(scanner.LastToken.Identifier);
                    scanner.ScanToken();
                }
                else
                    interpolatedValues.Add(ParseExpression());
            }

            MatchToken(TokenType.InterpolatedEnd);
            if(scanner.LastToken.Identifier != string.Empty)
                interpolatedValues.Add(scanner.LastToken.Identifier);
            scanner.ScanToken();
            return new InterpolatedString(interpolatedValues, location);
        }
    }
}
