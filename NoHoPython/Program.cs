using NoHoPython.IntermediateRepresentation;
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

            AstIRProgramBuilder astIRProgramBuilder = new AstIRProgramBuilder(statements);
            IRProgram program = astIRProgramBuilder.ToIRProgram();
            parser.IncludeCFiles(program);

            StringBuilder output = new StringBuilder();
            program.Emit(output);
            File.WriteAllText(args[1], output.ToString());

            Console.WriteLine($"Compilation succesfully finished, taking {DateTime.Now - compileStart}. Output is in {args[1]}.");
        }
        catch (SyntaxError syntaxError)
        {
            syntaxError.Print();
        }
        catch (IRGenerationError compilerError)
        {
            compilerError.Print();
        }

        return 0;
    }
}