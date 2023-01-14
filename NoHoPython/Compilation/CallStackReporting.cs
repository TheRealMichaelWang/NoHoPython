using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using System.Text;

namespace NoHoPython.Compilation
{
    public static class CallStackReporting
    {
        public static readonly int StackLimit = 1000;

        public static void EmitReporter(StatementEmitter emitter)
        {
            emitter.AppendLine($"static const char* _nhp_call_stack_src_locs[{StackLimit}];");
            emitter.AppendLine($"static const char* _nhp_call_stack_src[{StackLimit}];");
            emitter.AppendLine("static int _nhp_stack_size = 0;");

            emitter.AppendLine("static void _nhp_print_stack_trace() {");
            emitter.AppendLine("\tputs(\"Traceback (most recent call last):\");");
            emitter.AppendLine("\tfor(int i = 0; i <= _nhp_stack_size; i++) {");
            emitter.AppendLine("\t\tputchar('\\t');");
            emitter.AppendLine("\t\tputs(_nhp_call_stack_src_locs[i]);");
            emitter.AppendLine("\t\tputchar('\\t');");
            emitter.AppendLine("\t\tputchar('\\t');");
            emitter.AppendLine("\t\tputs(_nhp_call_stack_src[i]);");
            emitter.AppendLine("\t}");
            emitter.AppendLine("}");

            emitter.AppendLine("static void _nhp_set_errloc(const char* src_loc, const char* src) {");
            emitter.AppendLine("\t_nhp_call_stack_src_locs[_nhp_stack_size] = src_loc;");
            emitter.AppendLine("\t_nhp_call_stack_src[_nhp_stack_size] = src;");
            emitter.AppendLine("}");

            emitter.AppendLine("static void _nhp_santize_call(const char* src_loc, const char* src) {");
            emitter.AppendLine("\t_nhp_set_errloc(src_loc, src);");
            emitter.AppendLine($"\tif(_nhp_stack_size == {StackLimit - 1}) {{");
            emitter.AppendLine("\t\t_nhp_print_stack_trace();");
            emitter.AppendLine("\t\tputs(\"Stackoverflow Error\");");
            emitter.AppendLine("\t\tabort();");
            emitter.AppendLine("\t}");
            emitter.AppendLine("\t++_nhp_stack_size;");
            emitter.AppendLine("}");
        }

        public static void EmitReportCall(StatementEmitter emitter, IAstElement errorReportedElement, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            EmitReportCall(emitter, errorReportedElement);
            emitter.AppendLine();
        }

        public static void EmitReportCall(IEmitter emitter, IAstElement errorReportedElement)
        {
            emitter.Append("_nhp_santize_call(");
            CharacterLiteral.EmitCString(emitter, errorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            errorReportedElement.EmitSrcAsCString(emitter);
            emitter.Append(");");
        }

        public static void EmitReportReturn(StatementEmitter emitter, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            EmitReportReturn(emitter);
            emitter.AppendLine();
        }

        public static void EmitReportReturn(IEmitter emitter) => emitter.Append("--_nhp_stack_size;");

        public static void EmitErrorLoc(StatementEmitter emitter, IAstElement errorReportedElement, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            EmitErrorLoc(emitter, errorReportedElement);
            emitter.AppendLine();
        }

        public static void EmitErrorLoc(IEmitter emitter, IAstElement errorReportedElement)
        {
            emitter.Append("_nhp_set_errloc(");
            CharacterLiteral.EmitCString(emitter, errorReportedElement.SourceLocation.ToString(), false, true);
            emitter.Append(", ");
            errorReportedElement.EmitSrcAsCString(emitter);
            emitter.Append(");");
        }

        public static void EmitErrorLoc(StatementEmitter emitter, string locationSrc, string src, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            emitter.AppendLine($"_nhp_set_errloc({locationSrc}, {src});");
        }

        public static void EmitPrintStackTrace(StatementEmitter emitter, int indent)
        {
            CodeBlock.CIndent(emitter, indent);
            EmitPrintStackTrace(emitter);
            emitter.AppendLine();
        }

        public static void EmitPrintStackTrace(IEmitter emitter) => emitter.Append("_nhp_print_stack_trace();");
    }
}
