using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;
using System.Text;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.Title = "North-Hollywood Python Compiler";

        //try
        //{
            AstParser parser = new(new Scanner(args[0], Environment.CurrentDirectory));

            List<IAstStatement> statements = parser.ParseAll();

            AstIRProgramBuilder astIRProgramBuilder = new AstIRProgramBuilder(statements);
            IRProgram program = astIRProgramBuilder.ToIRProgram();
            
            StringBuilder output = new StringBuilder();
            program.Emit(output);
            File.WriteAllText(args[1], output.ToString());
        //}
        //catch (SyntaxError syntaxError)
        //{
        //    syntaxError.Print();
        //}
        //catch (IRGenerationError compilerError)
        //{
        //    compilerError.Print();
        //}
        //catch (CCodegenError codegenError)
        //{
        //    codegenError.Print();
        //}

        return 0;
    }
}