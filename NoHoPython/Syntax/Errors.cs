using NoHoPython.Syntax.Parsing;

namespace NoHoPython.Syntax
{
    public abstract class SyntaxError : Exception
    {
        public SourceLocation? SourceLocation { get; private set; }

        public SyntaxError(SourceLocation? sourceLocation, string message) : base(message)
        {
            SourceLocation = sourceLocation;
        }

        public void Print()
        {
            if(!SourceLocation.HasValue)
            {
                Console.WriteLine($"Syntax Error: {Message}");
                Console.WriteLine("No valid source location provided; error occured in top-level inclusion. Perhaps user specified file wasn't found, or std.nhp - a requried standard library file - wasn't found.");
                return;
            }

            string rawLine = File.ReadAllLines(SourceLocation.Value.File)[SourceLocation.Value.Row - 1];
            string errorLine = rawLine.TrimStart('\t');
            int trimmedTabs = rawLine.Length - errorLine.Length + 2;

            Console.WriteLine($"Syntax Error: {Message}");
            Console.WriteLine($"\nin file {SourceLocation.Value.File}:\n");

            Console.WriteLine($"{SourceLocation.Value.Row}:\t{errorLine}");

            Console.Write('\t');
            for (int i = trimmedTabs; i < SourceLocation.Value.Column - 1; i++)
                Console.Write(' ');
            Console.WriteLine('^');
        }
    }

    public sealed class UnexpectedTokenException : SyntaxError
    {
        public TokenType? ExpectedTokenType { get; private set; }
        public Token RecievedToken { get; private set; }

        public UnexpectedTokenException(TokenType expectedTokenType, Token recievedToken, SourceLocation sourceLocation) : base(sourceLocation, $"Expected {expectedTokenType} but got {recievedToken.Type} instead.")
        {
            ExpectedTokenType = expectedTokenType;
            RecievedToken = recievedToken;
        }

        public UnexpectedTokenException(Token recievedToken, SourceLocation sourceLocation) : base(sourceLocation, $"Unexpected token {recievedToken.Type}")
        {
            ExpectedTokenType = null;
            RecievedToken = recievedToken;
        }
    }

    public sealed class IndentationLevelException : SyntaxError
    {
        public int ExpectedIndentationLevel { get; private set; }
        public int ReceivedIndentationLevel { get; private set; }

        public IndentationLevelException(int expectedIndentationLevel, int recievedIndentationLevel, SourceLocation sourceLocation) : base(sourceLocation, $"Expected {expectedIndentationLevel} tabs/indents, but got {recievedIndentationLevel} instead.")
        {
            ExpectedIndentationLevel = expectedIndentationLevel;
            ReceivedIndentationLevel = recievedIndentationLevel;
        }
    }
}
