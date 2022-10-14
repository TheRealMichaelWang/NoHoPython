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

            AstParser parser = new(new Scanner(args[0], Environment.CurrentDirectory + "\\stdlib"));

            List<IAstStatement> statements = parser.ParseAll();

            AstIRProgramBuilder astIRProgramBuilder = new(statements);
            IRProgram program = astIRProgramBuilder.ToIRProgram(args.Contains("-nobounds"), args.Contains("-noassert"), !args.Contains("-nogcc"));
            parser.IncludeCFiles(program);

            StringBuilder output = new();
            program.Emit(output);

            string outputFile;
            if (args.Length >= 2)
                outputFile = args[1];
            else
                outputFile = "out.c";

            File.WriteAllText(outputFile, output.ToString());
            Console.WriteLine($"Compilation succesfully finished, taking {DateTime.Now - compileStart}. Output is in {outputFile}.");
        }
        catch (SyntaxError syntaxError)
        {
            syntaxError.Print();
        }
        catch (IRGenerationError compilerError)
        {
            compilerError.Print();
        }
        catch (CCodegenError codegenError)
        {
            codegenError.Print();
        }
        catch (FileNotFoundException f)
        {
            Console.WriteLine($"File not found: {f.Message}");
        }

        return 0;
    }
} 