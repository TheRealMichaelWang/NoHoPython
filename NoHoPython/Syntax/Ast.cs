namespace NoHoPython.Syntax
{
    public interface IAstValue : ISourceLocatable
    {
        string ToString();
        //public IRValue GenerateIntermediateRepresentation();
    }

    public interface IAstStatement : ISourceLocatable
    {
        public static string Indent(int indent) => new string('\t', indent);
        public static string BlockToString(int indent, List<IAstStatement> statements) => string.Join('\n', statements.Select((IAstStatement statement) => $"{statement.SourceLocation.Row}:{statement.ToString(indent + 1)}"));

        string ToString(int indent);
        //public IRStatement GenerateIntermediateRepresentation();
    }
    
    public struct SourceLocation
    {
        public int Row { get; private set; }
        public int Column { get; private set; }
        public string File { get; private set; }

        public SourceLocation(int row, int column, string file)
        {
            Row = row;
            Column = column;
            File = file;
        }
    }

    public interface ISourceLocatable
    {
        public SourceLocation SourceLocation { get; }
    }
}
