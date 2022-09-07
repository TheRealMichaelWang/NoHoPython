using NoHoPython.IntermediateRepresentation;

namespace NoHoPython.Syntax
{
    public interface IAstValue : ISourceLocatable
    {
        string ToString();

        public IRValue GenerateIntermediateRepresentation(IRProgramBuilder irBuilder);
    }

    public interface IAstStatement : ISourceLocatable
    {
        public static string Indent(int indent) => new string('\t', indent);
        public static string BlockToString(int indent, List<IAstStatement> statements) => string.Join('\n', statements.Select((IAstStatement statement) => $"{statement.SourceLocation.Row}:{statement.ToString(indent + 1)}"));

        public static void ForwardDeclareBlock(IRProgramBuilder irBuilder, List<IAstStatement> statements) => statements.ForEach((statement) => statement.ForwardDeclare(irBuilder));

        string ToString(int indent);

        public void ForwardTypeDeclare(IRProgramBuilder irBuilder);
        public void ForwardDeclare(IRProgramBuilder irBuilder);
        public IRStatement GenerateIntermediateRepresentation(IRProgramBuilder irBuilder);
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
