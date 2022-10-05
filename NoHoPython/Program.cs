using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;
using System.Text;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.Title = "North-Hollywood Python Compiler";

        if (args.Length == 0)
        {
            Console.WriteLine("No input file supplied;aborting program.");
            return 0;
        }

        try
        {
            DateTime compileStart = DateTime.Now;

            AstParser parser = new(new Scanner(args[0], Environment.CurrentDirectory));

            List<IAstStatement> statements = parser.ParseAll();

            AstIRProgramBuilder astIRProgramBuilder = new(statements);
            IRProgram program = astIRProgramBuilder.ToIRProgram();
            parser.IncludeCFiles(program);

            StringBuilder output = new();
            program.Emit(output);
            File.WriteAllText(args[1], output.ToString());

            Console.WriteLine($"Compilation succesfully finished, taking {DateTime.Now - compileStart}. Output is in {args[1]}.");
        }
        catch (SyntaxError syntaxError)
        {
            syntaxError.Print();
        }
        //catch (IRGenerationError compilerError)
        //{
        //    compilerError.Print();
        //}
        catch (CCodegenError codegenError)
        {
            codegenError.Print();
        }
        catch (FileNotFoundException f)
        {
            Console.WriteLine(f.Message);
        }

        return 0;
    }
} 