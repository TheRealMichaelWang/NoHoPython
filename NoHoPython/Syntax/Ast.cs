using NoHoPython.IntermediateRepresentation;

namespace NoHoPython.Syntax
{
    public interface IAstValue : ISourceLocatable
    {
        //public IRValue GenerateIntermediateRepresentation();
    }

    public interface IAstStatement : ISourceLocatable
    {
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
