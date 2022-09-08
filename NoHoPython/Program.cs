using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.Title = "North-Hollywood Python Compiler";

        try
        {
            AstParser parser = new(new Scanner(args[0], Environment.CurrentDirectory));

            List<IAstStatement> statements = parser.ParseAll();
            Console.WriteLine(IAstStatement.BlockToString(0, statements));
        }
        catch (SyntaxError syntaxError)
        {
            syntaxError.Print();
        }

        return 0;
    }
}