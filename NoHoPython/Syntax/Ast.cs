using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;

namespace NoHoPython.Syntax
{
    public interface IAstElement : ISourceLocatable { }

    public interface IAstValue : IAstElement
    {
        string ToString();

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType);
    }

    public interface IAstStatement : IAstElement
    {
        public static string Indent(int indent) => new('\t', indent);
        public static string BlockToString(int indent, List<IAstStatement> statements) => string.Join('\n', statements.Select((IAstStatement statement) => $"{statement.SourceLocation.Row}:{statement.ToString(indent + 1)}"));

        public static void ForwardDeclareBlock(AstIRProgramBuilder irBuilder, List<IAstStatement> statements) => statements.ForEach((statement) => statement.ForwardDeclare(irBuilder));
        public static List<IRStatement> GenerateIntermediateRepresentationForBlock(AstIRProgramBuilder irBuilder, List<IAstStatement> statements) => statements.ConvertAll((IAstStatement statement) => statement.GenerateIntermediateRepresentationForStatement(irBuilder));

        string ToString(int indent);

        //forward declare type definitions (ie records, interfaces, and enums)
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder);

        //forward declare procuedure and symbol definitions
        public void ForwardDeclare(AstIRProgramBuilder irBuilder);

        //generate IR
        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder);
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
