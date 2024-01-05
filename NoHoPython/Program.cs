using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;
using System.Diagnostics;
using System.Reflection;

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
            Console.WriteLine("Written by Michael Wang, 2022-24");
            return 0;
        }

        bool mainFunction = !args.Contains("-nomain");
        bool runForMe = args.Contains("-runforme");
        try
        {
            DateTime compileStart = DateTime.Now;

            Console.WriteLine("Parsing...");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            string execDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            AstParser sourceParser = new AstParser(new Scanner(args[0], $"{execDir}/stdlib"));
            List<IAstStatement> statements = sourceParser.ParseAll();

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

            for (int i = 1; i < args.Length; i++)
                flags.Add(args[i]);

            Console.WriteLine("Generating IR...");
            AstIRProgramBuilder astIRProgramBuilder = new(statements, flags);
            IRProgram program = astIRProgramBuilder.ToIRProgram(!args.Contains("-nobounds"), args.Contains("-noassert"), args.Contains("-callstack") || args.Contains("-stacktrace"), args.Contains("-namert"), args.Contains("-linedir") || args.Contains("-ggdb"), mainFunction, memoryAnalyzer);
            sourceParser.IncludeCFiles(program);

            string outputFile;
            if (runForMe)
                outputFile = $"{execDir}/out.c";
            else if (args.Length >= 2)
                outputFile = args[1];
            else
                outputFile = "out.c";

            Console.WriteLine("Compiling...");
            if (args.Contains("-header"))
            {
                string headerName = outputFile.EndsWith(".c") ? outputFile.Replace(".c", ".h") : outputFile + ".h";
                program.IncludeCFile((headerName, null));
                program.Emit(outputFile, headerName);
            }
            else
                program.Emit(outputFile, null);

            Console.WriteLine($"Compilation succesfully finished, taking {DateTime.Now - compileStart}. Output is in {outputFile}.");
            if (runForMe)
            {
                Console.WriteLine("Invoking gcc...");
                using (Process gcc = Process.Start("GCC", $"\"{outputFile}\" -o \"{execDir}/nhptempexec\" {(program.EmitLineDirectives ? "-ggdb" : string.Empty)}"))
                {
                    gcc.WaitForExit();
                    if(gcc.ExitCode != 0)
                        Console.WriteLine("C compilation failed; this indicates either a bug in the compiler, or a bug in the foreign inline C of your code (or another NHP library).");
                    else
                    {
                        Console.Clear();
                        Process.Start($"{execDir}/nhptempexec").WaitForExit();
                    }
                }
            }
            else if (program.EmitLineDirectives)
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
        catch (CodegenError codegenError)
        {
            codegenError.Print();
        }
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

        if (runForMe)
            Console.ReadKey();

        return 0;
    }
} 