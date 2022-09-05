using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;

public static class Program 
{
    public static int Main(string[] args)
    {
        AstParser parser = new AstParser(new Scanner(args[0], Environment.CurrentDirectory));

        List<IAstStatement> statements = parser.ParseAll();
        Console.WriteLine(IAstStatement.BlockToString(0, statements));

        return 0;
    }
}