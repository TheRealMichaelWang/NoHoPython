using NoHoPython.Syntax;
using NoHoPython.Typing;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.IntermediateRepresentation
{
    public interface IEmitter
    {
        public void Append(string str);
        public void Append(char c);
    }

    public sealed class BufferedEmitter : IEmitter
    {
        public static string EmitBufferedValue(IRValue value, IRProgram irProgram, Dictionary<Typing.TypeParameter, IType> typeArgs, string responsibleDestroyer)
        {
            BufferedEmitter bufferedEmitter = new();
            value.Emit(irProgram, bufferedEmitter, typeArgs, responsibleDestroyer, false);
            return bufferedEmitter.ToString();
        }

        public static string EmittedBufferedMemorySafe(IRValue value, IRProgram irProgram, Dictionary<Typing.TypeParameter, IType> typeArgs)
        {
            BufferedEmitter bufferedEmitter = new();
            IRValue.EmitMemorySafe(value, irProgram, bufferedEmitter, typeArgs);
            return bufferedEmitter.ToString();
        }

        private StringBuilder builder;

        public BufferedEmitter()
        {
            builder = new();
        }

        public BufferedEmitter(int capacity)
        {
            builder = new(capacity);
        }

        public void Append(string str)
        {
            Debug.Assert(!str.Contains('\n'));
            builder.Append(str);
        }

        public void Append(char c)
        {
            Debug.Assert(c != '\n');
            builder.Append(c);
        }

        public override string ToString() => builder.ToString();
    }

    public sealed class StatementEmitter : IEmitter, IDisposable
    {
        private StreamWriter writer;
        private StringBuilder currentLineBuilder;
        private IRProgram irProgram;

        public SourceLocation? LastSourceLocation { get; set; }

        public StatementEmitter(string outputPath, IRProgram irProgram)
        {
            this.irProgram = irProgram;

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            writer = new(new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write));
            currentLineBuilder = new();
        }

        public void Append(string str)
        {
            Debug.Assert(!str.Contains('\n'));
            currentLineBuilder.Append(str);
        }

        public void Append(char c)
        {
            Debug.Assert(c != '\n');
            currentLineBuilder.Append(c);
        }

        public void AppendLine(string str)
        {
            currentLineBuilder.Append(str);
            AppendLine();
        }

        public void AppendLine()
        {
            if (irProgram.EmitLineDirectives && LastSourceLocation.HasValue)
            {
                BufferedEmitter bufferedEmitter = new();
                LastSourceLocation.Value.EmitLineDirective(bufferedEmitter);
                writer.WriteLine(bufferedEmitter.ToString());
            }

            writer.WriteLine(currentLineBuilder.ToString());
            currentLineBuilder.Clear();
        }

        #region disposing
        private bool isDisposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                // free managed resources
                writer.Close();
                writer.Dispose();
            }

            isDisposed = true;
        }
        #endregion
    }
}
