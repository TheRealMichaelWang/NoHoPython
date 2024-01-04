using NoHoPython.Syntax;
using NoHoPython.Typing;
using System.Diagnostics;
using System.Text;

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class Emitter : IDisposable
    {
        public delegate void SetPromise(Promise valueEmitPromise);
        public delegate void Promise(Emitter emitter);

        public static string GetPromiseSource(Promise promise)
        {
            using(Emitter e = new())
            {
                promise(e);
                return e.GetBuffered();
            }
        }

        public static Promise NullPromise = (emitter) => emitter.Append("NULL");

        private TextWriter writer;
        private Stream stream;
        private StringBuilder currentLineBuilder;

        public SourceLocation? LastSourceLocation { get; set; }
        public bool EmitLineDirectives { get; private set; }
        private int Indirection;
        private Stack<Promise> resourceDestructors;
        private Stack<int> blockDestructionFrames;
        private Stack<int> loopDestructorFrames;
        private Stack<int> functionDestructorFrames;
        private SortedSet<int> destructionExemptions;

        public bool BufferMode { get; private set; }

        public Emitter(string outputPath, bool emitLineDirectives)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            BufferMode = false;
            EmitLineDirectives = emitLineDirectives;
            Indirection = 0;
            stream = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write);
            writer = new StreamWriter(stream, Encoding.UTF8);
            currentLineBuilder = new();
            resourceDestructors = new();
            blockDestructionFrames = new();
            loopDestructorFrames = new();
            functionDestructorFrames = new();
            destructionExemptions = new();
        }

        public Emitter()
        {
            BufferMode = true;
            EmitLineDirectives = false;
            Indirection = 0;
            stream = new MemoryStream();
            writer = new StreamWriter(stream, new UTF8Encoding(false));
            currentLineBuilder = new();
            resourceDestructors = new();
            blockDestructionFrames = new();
            loopDestructorFrames = new();
            functionDestructorFrames = new();
            destructionExemptions = new();
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
                writer.WriteLine(LastSourceLocation.Value.GetLineDirective());
                //LastSourceLocation = null;
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

            blockDestructionFrames.Push(resourceDestructors.Count);
            
            return Indirection;
        }

        public void AppendEndBlock(bool endWithSemicolon=false)
        {
            if (currentLineBuilder.Length > 0)
                AppendLine();

            DestroyResources(blockDestructionFrames.Peek(), blockDestructionFrames.Pop());
            currentLineBuilder.Append('}');
            if (endWithSemicolon)
                currentLineBuilder.Append(';');
            Indirection--;
            AppendLine();
        }

        public void DeclareLoopBlock() => loopDestructorFrames.Push(resourceDestructors.Count);
        public void DeclareFunctionBlock() => functionDestructorFrames.Push(resourceDestructors.Count);
        public void EndLoopBlock() => DestroyResources(loopDestructorFrames.Peek(), loopDestructorFrames.Pop());
        public void EndFunctionBlock() => DestroyResources(functionDestructorFrames.Peek(), functionDestructorFrames.Pop());

        public void DestroyBlockResources() => DestroyResources(blockDestructionFrames.Peek(), blockDestructionFrames.Peek());
        public void DestroyLoopResources() => DestroyResources(loopDestructorFrames.Peek(), blockDestructionFrames.Peek());
        public void DestroyFunctionResources() => DestroyResources(functionDestructorFrames.Peek(), blockDestructionFrames.Peek());
        public void AssumeBlockResources()
        {
            while(resourceDestructors.Count > blockDestructionFrames.Peek())
                resourceDestructors.Pop();
        }

        private void DestroyResources(int depth, int frameLimit)
        {
            Stack<Promise> recoveredPromises = new();
            int exemption_id;
            while ((exemption_id = resourceDestructors.Count) > depth)
            {
                Promise p = resourceDestructors.Pop();
                if (destructionExemptions.Contains(exemption_id))
                    destructionExemptions.Remove(exemption_id);
                else
                    p(this);
                if (resourceDestructors.Count < frameLimit)
                    recoveredPromises.Push(p);
            }
            foreach (Promise promise in recoveredPromises)
                resourceDestructors.Push(promise);
        }

        public void SetArgument(IRValue irValue, string location, IRProgram irProgram, Dictionary<Typing.TypeParameter, IType> typeargs, bool isTemporary, Promise? responsibleDestroyer = null)
        {
            responsibleDestroyer = responsibleDestroyer ?? NullPromise;
            irValue.Emit(irProgram, this, typeargs, (promise) =>
            {
                Append(location);
                Append(" = ");
                promise(this);
                AppendLine(';');
            }, responsibleDestroyer, isTemporary);

            if (!irValue.RequiresDisposal(irProgram, typeargs, isTemporary))
                return;

            if(irValue.RequiresDisposal(irProgram, typeargs, isTemporary))
                AddResourceDestructor(emitter => irValue.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, (e) => e.Append(location), responsibleDestroyer));
        }

        public void ExemptResourceFromDestruction(int exemptionId)
        {
            Debug.Assert(exemptionId <= resourceDestructors.Count && exemptionId >= 0);
            destructionExemptions.Add(exemptionId);
        }

        public int AddResourceDestructor(Promise resourceDestructor)
        {
            resourceDestructors.Push(resourceDestructor);
            return resourceDestructors.Count;
        }

        public string GetBuffered()
        {
            DestroyResources(0, 0);
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
                DestroyResources(0, 0);
                Flush();
                writer.Close();
                writer.Dispose();
            }

            isDisposed = true;
        }
        #endregion
    }
}
