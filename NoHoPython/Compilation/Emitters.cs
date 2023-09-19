using NoHoPython.Syntax;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class Emitter : IDisposable
    {
        public delegate void SetPromise(Promise valueEmitPromise);
        public delegate void Promise(Emitter emitter);

        public static Promise NullPromise = (emitter) => emitter.Append("NULL");

        private TextWriter writer;
        private Stream stream;
        private StringBuilder currentLineBuilder;

        public SourceLocation? LastSourceLocation { get; set; }
        public bool EmitLineDirectives { get; private set; }
        private int Indirection;

        public bool BufferMode { get; private set; }

        public Emitter(string outputPath, bool emitLineDirectives)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            BufferMode = false;
            EmitLineDirectives = emitLineDirectives;
            Indirection = 0;
            stream = new FileStream(outputPath, FileMode.OpenOrCreate, FileAccess.Write);
            writer = new StreamWriter(stream, Encoding.UTF8);
            currentLineBuilder = new();
        }

        public Emitter()
        {
            BufferMode = true;
            EmitLineDirectives = false;
            Indirection = 0;
            stream = new MemoryStream();
            writer = new StreamWriter(stream, new UTF8Encoding(false));
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

        public void AppendLine(char c)
        {
            currentLineBuilder.Append(c);
            AppendLine();
        }

        public void AppendLine(string str)
        {
            currentLineBuilder.Append(str);
            AppendLine();
        }

        public void AppendLine()
        {
            if (BufferMode)
                throw new InvalidOperationException();

            if (EmitLineDirectives && LastSourceLocation.HasValue)
            {
                LastSourceLocation.Value.EmitLineDirective(this);
                writer.WriteLine();
            }

            Flush(true);
            writer.WriteLine();
        }

        public int AppendStartBlock(string str)
        {
            currentLineBuilder.Append(str);
            return AppendStartBlock();
        }

        public int AppendStartBlock()
        {
            if (currentLineBuilder.Length > 0)
                currentLineBuilder.Append(' ');
            currentLineBuilder.Append('{');
            AppendLine();
            Indirection++;

            return Indirection;
        }

        public void AppendEndBlock()
        {
            Debug.Assert(currentLineBuilder.Length == 0);
            currentLineBuilder.Append('}');
            Indirection--;
            AppendLine();
        }

        public string GetBuffered()
        {
            Flush();
            MemoryStream memoryStream = (MemoryStream)stream;
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        #region disposing
        private bool isDisposed = false;

        public void Flush(bool suppressFlush=false)
        {
            if (currentLineBuilder.Length == 0)
                return;

            for (int i = 0; i < Indirection; i++)
                writer.Write('\t');

            writer.Write(currentLineBuilder.ToString());
            if(!suppressFlush)
                writer.Flush();
            currentLineBuilder.Clear();
        }

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
                Flush();
                writer.Close();
                writer.Dispose();
            }

            isDisposed = true;
        }
        #endregion
    }
}
