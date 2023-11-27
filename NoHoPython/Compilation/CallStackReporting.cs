using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;

namespace NoHoPython.Compilation
{
    public static class CallStackReporting
    {
        public static readonly int StackLimit = 1000;

        public static void EmitReporter(Emitter emitter)
        {
            emitter.AppendLine($"static const char* nhp_call_stack_src_locs[{StackLimit}];");
            emitter.AppendLine($"static const char* nhp_call_stack_src[{StackLimit}];");
            emitter.AppendLine("static int nhp_stack_size = 0;");

            emitter.AppendLine("static void nhp_print_stack_trace() {");
            emitter.AppendLine("\tputs(\"Traceback (most recent call last):\");");
            emitter.AppendLine("\tfor(int i = 0; i <= nhp_stack_size; i++) {");
            emitter.AppendLine("\t\tputchar('\\t');");
            emitter.AppendLine("\t\tputs(nhp_call_stack_src_locs[i]);");
            emitter.AppendLine("\t\tputchar('\\t');");
            emitter.AppendLine("\t\tputchar('\\t');");
            emitter.AppendLine("\t\tputs(nhp_call_stack_src[i]);");
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");

            emitter.AppendLine("static void nhp_set_errloc(const char* src_loc, const char* src) {");
            emitter.AppendLine("\tnhp_call_stack_src_locs[nhp_stack_size] = src_loc;");
            emitter.AppendLine("\tnhp_call_stack_src[nhp_stack_size] = src;");
            emitter.AppendLine("}");

            emitter.AppendLine("static void nhp_santize_call(const char* src_loc, const char* src) {");
            emitter.AppendLine("\tnhp_set_errloc(src_loc, src);");
            emitter.AppendLine($"\tif(nhp_stack_size == {StackLimit - 1}) {{");
            emitter.AppendLine("\t\tnhp_print_stack_trace();");
            emitter.AppendLine("\t\tputs(\"Stackoverflow Error\");");
            emitter.AppendLine("\t\tabort();");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\t++nhp_stack_size;");
            emitter.AppendLine("}");
        }

        public static void EmitReportCall(Emitter emitter, IAstElement errorReportedElement)
        {
            emitter.Append("nhp_santize_call(");
            CharacterLiteral.EmitCString(emitter, errorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            errorReportedElement.EmitSrcAsCString(emitter);
            emitter.AppendLine(");");
        }

        public static void EmitReportReturn(Emitter emitter) => emitter.AppendLine("--nhp_stack_size;");

        public static void EmitErrorLoc(Emitter emitter, IAstElement errorReportedElement)
        {
            emitter.Append("nhp_set_errloc(");
            CharacterLiteral.EmitCString(emitter, errorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            errorReportedElement.EmitSrcAsCString(emitter);
            emitter.AppendLine(");");
        }

        public static void EmitErrorLoc(Emitter emitter, string locationSrc, string src) => emitter.AppendLine($"nhp_set_errloc({locationSrc}, {src});");

        public static void EmitPrintStackTrace(Emitter emitter) => emitter.AppendLine("nhp_print_stack_trace();");
    }
}
