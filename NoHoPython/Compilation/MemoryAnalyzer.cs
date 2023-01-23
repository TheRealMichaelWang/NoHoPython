using NoHoPython.IntermediateRepresentation;
using System.Text;

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

        public string Allocate(string size) => $"{((Mode == AnalysisMode.None && !ProtectAllocFailure) ? "malloc" : "_nhp_malloc")}({size})";
        public string Dealloc(string ptr, string size) => $"{(Mode == AnalysisMode.None ? "free" : "_nhp_free")}({ptr}{(Mode >= AnalysisMode.UsageMagnitudeCheck ? ", " + size : string.Empty)})";

        public MemoryAnalyzer(AnalysisMode analysisMode, bool protectAllocFailure)
        {
            Mode = analysisMode;
            ProtectAllocFailure = protectAllocFailure;
        }

        public void EmitAnalyzers(StatementEmitter emitter)
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
            emitter.AppendLine("static void* _nhp_malloc(int size) {");

            if (ProtectAllocFailure)
            {
                emitter.AppendLine("\tvoid* buffer = malloc(size);");
                emitter.AppendLine("\tif(!buffer) {");
                emitter.AppendLine("\t\tputs(\"Memory Allocation Faliure (malloc returned NULL)\")");
                emitter.AppendLine("\t\tmemoryReport();");
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

            #region emit_destructor
            if (Mode > AnalysisMode.None)
            {
                if(Mode >= AnalysisMode.UsageMagnitudeCheck)
                    emitter.AppendLine("static void _nhp_free(void* buf, int size) {");
                else
                    emitter.AppendLine("static void _nhp_free(void* buf) {");
                
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
