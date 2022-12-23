using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;
using System.Text;

public static class Program
{
    public static int Main(string[] args)
    {
        MemoryAnalyzer.AnalysisMode requestedAnalysisMode()
        {
            if (args.Contains("-leaksan") || args.Contains("-meman1"))
                return MemoryAnalyzer.AnalysisMode.LeakSanityCheck;
            else if (args.Contains("-leaksize") || args.Contains("-meman2"))
                return MemoryAnalyzer.AnalysisMode.UsageMagnitudeCheck;
            return MemoryAnalyzer.AnalysisMode.None;
        }

        Console.Title = "North-Hollywood Python Compiler";

        if (args.Length == 0)
        {
            Console.WriteLine("No input file supplied; aborting program.");
            return 0;
        }
        else if (args.Contains("-credits"))
        {
            Console.WriteLine("North Hollywood Python Compiler");
            Console.WriteLine("Written by Michael Wang, 2022, for team 10515K");
            return 0;
        }

        try
        {
            DateTime compileStart = DateTime.Now;

            AstParser parser = new(new Scanner(args[0], $"{Environment.CurrentDirectory}/stdlib"));

            List<IAstStatement> statements = parser.ParseAll();

            MemoryAnalyzer memoryAnalyzer = new MemoryAnalyzer(requestedAnalysisMode(), args.Contains("-memfail"));

            AstIRProgramBuilder astIRProgramBuilder = new(statements);
            IRProgram program = astIRProgramBuilder.ToIRProgram(args.Contains("-nobounds"), args.Contains("-noassert"), !args.Contains("-nogcc"), args.Contains("-callstack") || args.Contains("-stacktrace"), memoryAnalyzer);
            parser.IncludeCFiles(program);

            string outputFile;
            if (args.Length >= 2)
                outputFile = args[1];
            else
                outputFile = "out.c";

            StringBuilder output = new();
            if (args.Contains("-header"))
            {
                string headerName = outputFile.EndsWith(".c") ? outputFile.Replace(".c", ".h") : outputFile + ".h";
                StringBuilder headerBuilder = new();
                program.IncludeCFile(headerName);
                program.Emit(output, headerBuilder);
                File.WriteAllText(headerName, headerBuilder.ToString());
            }
            else
                program.Emit(output, output);

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
        //catch (CCodegenError codegenError)
        //{
        //    codegenError.Print();
        //}
        catch (FileNotFoundException f)
        {
            Console.WriteLine($"File not found: {f.Message}");
        }

        return 0;
    }
} 