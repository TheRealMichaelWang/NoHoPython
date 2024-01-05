using NoHoPython.IntermediateRepresentation;

namespace NoHoPython.Compilation
{
    public class MemoryAnalyzer
    {
        public enum AnalysisMode
        {
            None,
            LeakSanityCheck,
            UsageMagnitudeCheck
        }

        public AnalysisMode Mode { get; private set; }
        public bool ProtectAllocFailure { get; private set; }

        public string Allocate(string size) => $"{((Mode == AnalysisMode.None && !ProtectAllocFailure) ? "malloc" : "nhp_malloc")}({size})";
        public string Dealloc(string ptr, string size) => $"{(Mode == AnalysisMode.None ? "free" : "nhp_free")}({ptr}{(Mode >= AnalysisMode.UsageMagnitudeCheck ? ", " + size : string.Empty)});";
        
        public string Realloc(string ptr, string oldSize, string newSize)
        {
            if (Mode == AnalysisMode.None && !ProtectAllocFailure)
                return $"realloc({ptr}, {newSize})";

            if (Mode >= AnalysisMode.UsageMagnitudeCheck)
                return $"nhp_realloc({ptr}, {oldSize}, {newSize})";
            return $"nhp_realloc({ptr}, {newSize})";
        }

        public void EmitAllocate(Emitter emitter, Emitter.Promise sizePromise)
        {
            emitter.Append((Mode == AnalysisMode.None && !ProtectAllocFailure) ? "malloc" : "nhp_malloc");
            emitter.Append('(');
            sizePromise(emitter);
            emitter.Append(')');
        }

        public MemoryAnalyzer(AnalysisMode analysisMode, bool protectAllocFailure)
        {
            Mode = analysisMode;
            ProtectAllocFailure = protectAllocFailure;
        }

        public void EmitAnalyzers(Emitter emitter)
        {
            if (Mode == AnalysisMode.None && !ProtectAllocFailure)
                return;

            if (Mode >= AnalysisMode.LeakSanityCheck)
            {
                emitter.AppendLine("static int active_allocs = 0;");
                emitter.AppendLine("static int peak_allocs = 0;");
            }
            if (Mode >= AnalysisMode.UsageMagnitudeCheck)
            {
                emitter.AppendLine("static int active_memory_usage = 0;");
                emitter.AppendLine("static int peak_memory_usage = 0;");
            }

            #region emitReporter
            if (Mode > AnalysisMode.None)
            {
                emitter.AppendLine("static void memoryReport() {");
                emitter.AppendLine("\tputs(\"NHP Memory Analysis Report\");");

                if (Mode >= AnalysisMode.LeakSanityCheck)
                    emitter.AppendLine("\tprintf(\"Active Memory Allocations: %i\\nPeak Memory Allocations: %i\\n\", active_allocs, peak_allocs);");
                if (Mode >= AnalysisMode.UsageMagnitudeCheck)
                    emitter.AppendLine("\tprintf(\"Active Memory Usage: %ib\\nPeak Memory Usage: %ib\\n\", active_memory_usage, peak_memory_usage);");
                emitter.AppendLine("}");
            }
            #endregion
            
            #region emitAllocator
            emitter.AppendLine("static void* nhp_malloc(int size) {");

            if (ProtectAllocFailure)
            {
                emitter.AppendLine("\tvoid* buffer = malloc(size);");
                emitter.AppendLine("\tif(!buffer) {");
                emitter.AppendLine("\t\tputs(\"Memory Allocation Faliure (malloc returned NULL)\");");
                emitter.AppendLine("\t\tmemoryReport();");
                emitter.AppendLine("\t\tabort();");
                emitter.AppendLine("\t}");
            }

            if (Mode >= AnalysisMode.LeakSanityCheck) 
            {
                emitter.AppendLine("\tactive_allocs++;");
                emitter.AppendLine("\tpeak_allocs = (active_allocs > peak_allocs) ? active_allocs : peak_allocs;");
            }
            if (Mode >= AnalysisMode.UsageMagnitudeCheck) 
            {
                emitter.AppendLine("\tactive_memory_usage += size;");
                emitter.AppendLine("\tpeak_memory_usage = (active_memory_usage > peak_memory_usage) ? active_memory_usage : peak_memory_usage;");
            }

            if (ProtectAllocFailure)
                emitter.AppendLine("\treturn buffer;");
            else
                emitter.AppendLine("\treturn malloc(size);");
            emitter.AppendLine("}");
            #endregion

            #region emitReallocator
            if(Mode >= AnalysisMode.None)
            {
                if (Mode >= AnalysisMode.UsageMagnitudeCheck)
                {
                    emitter.AppendStartBlock("static void* nhp_realloc(void* original, int ogSize, int newSize)");
                    emitter.AppendLine("active_memory_usage += (newSize - ogSize);");
                    emitter.AppendLine("peak_memory_usage = (active_memory_usage > peak_memory_usage) ? active_memory_usage : peak_memory_usage;");
                }
                else
                    emitter.AppendStartBlock("static void* nhp_realloc(void* original, int newSize)");

                if (ProtectAllocFailure)
                {
                    emitter.AppendLine("void* buffer = realloc(original, newSize);");
                    emitter.AppendStartBlock("if(!buffer)");
                    emitter.AppendLine("puts(\"Memory Allocation Faliure (realloc returned NULL)\");");
                    emitter.AppendLine("memoryReport();");
                    emitter.AppendLine("abort();");
                    emitter.AppendEndBlock();
                    emitter.AppendLine("return buffer;");
                }
                else
                    emitter.AppendLine("return realloc(original, newSize);");
                emitter.AppendEndBlock();
            }
            #endregion

            #region emitDestructor
            if (Mode > AnalysisMode.None)
            {
                if(Mode >= AnalysisMode.UsageMagnitudeCheck)
                    emitter.AppendLine("static void nhp_free(void* buf, int size) {");
                else
                    emitter.AppendLine("static void nhp_free(void* buf) {");
                
                if (Mode >= AnalysisMode.LeakSanityCheck)
                    emitter.AppendLine("\tactive_allocs--;");
                if (Mode >= AnalysisMode.UsageMagnitudeCheck)
                    emitter.AppendLine("\tactive_memory_usage -= size;");
                
                emitter.AppendLine("\tfree(buf);");
                emitter.AppendLine("}");
            }
            #endregion
        }
    }
}
