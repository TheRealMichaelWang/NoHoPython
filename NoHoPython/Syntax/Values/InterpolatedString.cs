using NoHoPython.Syntax.Values;
using System.Text;

namespace NoHoPython.Syntax.Values
{
    public sealed partial class InterpolatedString : IAstValue
    {
        public SourceLocation SourceLocation { get; set; }

        public readonly List<object> InterpolatedValues; //only string or IAstValue

        public InterpolatedString(List<object> interpolatedValues, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            InterpolatedValues = interpolatedValues;

            if (!InterpolatedValues.TrueForAll((value) => value is string || value is IAstValue))
                throw new InvalidOperationException();
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append("$\"");
            foreach (object value in InterpolatedValues)
            {
                if (value is IAstValue astValue)
                    builder.Append($"{{{astValue}}}");
                else
                    builder.Append(value as string);
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
