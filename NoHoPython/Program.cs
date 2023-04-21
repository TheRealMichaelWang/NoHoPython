using NoHoPython.Compilation;
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

            List<string> flags = new();
            MemoryAnalyzer.AnalysisMode requestedAnalysisMode()
            {
                if (args.Contains("-leaksan") || args.Contains("-meman1"))
                {
                    flags.Add("mem1");
                    return MemoryAnalyzer.AnalysisMode.LeakSanityCheck;
                }
                else if (args.Contains("-leaksize") || args.Contains("-meman2"))
                {
                    flags.Add("mem2");
                    return MemoryAnalyzer.AnalysisMode.UsageMagnitudeCheck;
                }
                flags.Add("mem0");
                return MemoryAnalyzer.AnalysisMode.None;
            }

            MemoryAnalyzer memoryAnalyzer = new(requestedAnalysisMode(), args.Contains("-memfail"));
            if (OperatingSystem.IsWindows())
                flags.Add("windows");
            else if (OperatingSystem.IsLinux())
                flags.Add("linux");

            for (int i = 2; i < args.Length; i++)
                flags.Add(args[i]);
            AstIRProgramBuilder astIRProgramBuilder = new(statements, flags);
            IRProgram program = astIRProgramBuilder.ToIRProgram(!args.Contains("-nobounds"), args.Contains("-noassert"), !args.Contains("-nogcc"), args.Contains("-callstack") || args.Contains("-stacktrace"), args.Contains("-namert"), args.Contains("-linedir") || args.Contains("-ggdb"), memoryAnalyzer);
            parser.IncludeCFiles(program);

            string outputFile;
            if (args.Length >= 2)
                outputFile = args[1];
            else
                outputFile = "out.c";

            if (args.Contains("-header"))
            {
                string headerName = outputFile.EndsWith(".c") ? outputFile.Replace(".c", ".h") : outputFile + ".h";
                program.IncludeCFile(headerName);
                program.Emit(outputFile, headerName);
            }
            else
                program.Emit(outputFile, null);

            Console.WriteLine($"Compilation succesfully finished, taking {DateTime.Now - compileStart}. Output is in {outputFile}.");
            if (program.EmitLineDirectives)
            {
                Console.WriteLine($"GCC line directives have been enabled; please use the -ggdb flag while compiling {outputFile}, and gdb to debug it. Please not that this feature doesn't work very well at the moment, and is still experimental. In addition, unless you want to debug internal gdb code, type \"skip file {outputFile}\", then run.");
            }
        }
        catch (SyntaxError syntaxError)
        {
            syntaxError.Print();
        }
        catch (IRGenerationError compilerError)
        {
            compilerError.Print();
        }
        //catch (CodegenError codegenError)
        //{
        //    codegenError.Print();
        //}
        catch (FileNotFoundException f)
        {
            Console.WriteLine($"File not found: {f.Message}");
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine("An internal compiler error has occured; please report the following stack trace to https://github.com/TheRealMichaelWang/NoHoPython/issues/new.");
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

        return 0;
    }
} 