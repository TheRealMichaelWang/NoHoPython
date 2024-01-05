using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class ForeignInlineCError : CodegenError
    {
        public string InlineCSource { get; private set; }
        public int Index { get; private set; }
        public string InlineMessage { get; private set; }

        public ForeignInlineCError(string inlineCSource, int index, string inlineMessage, IRElement errorReportedElement) : base(errorReportedElement, $"An error occured with your inline C code: {inlineMessage}.")
        {
            InlineCSource = inlineCSource;
            Index = index;
            InlineMessage = inlineMessage;
        }

        public override void Print()
        {
            base.Print();
            Console.WriteLine(InlineCSource);
            for(int i = 0; i < Index - 1; i++)
                Console.Write(' ');
            Console.Write('^');
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ForeignCType
    {
        public static void ReplaceFunction(string templateSource, IRProgram irProgram, string name, int expectedArgs, Action<IRProgram, Emitter, string[]> replacer, IRElement errorReportedElement)
        {
            int index;
            while ((index = templateSource.IndexOf(name)) != -1)
            {
                List<string> arguments = new(expectedArgs);

                StringBuilder builder = new();
                int parens = 0;
                int i = index + name.Length;
                for (; i < templateSource.Length; i++)
                {
                    if (templateSource[i] == '(')
                        parens++;
                    else
                    {
                        if (i == index + name.Length)
                            throw new ForeignInlineCError(templateSource, i, $"Call to {name} missing open paren.", errorReportedElement);

                        if (templateSource[i] == ')')
                        {
                            parens--;
                            if (parens == 0)
                            {
                                arguments.Add(builder.ToString());
                                break;
                            }
                        }
                    }

                    if (parens == 1 && templateSource[i] == ',')
                    {
                        arguments.Add(builder.ToString());
                        builder.Clear();
                    }
                    else
                        builder.Append(templateSource[i]);
                }

                if (parens != 0)
                    throw new ForeignInlineCError(templateSource, i, "Expected close paren instead of EOF.", errorReportedElement);
                if (arguments.Count != expectedArgs)
                    throw new ForeignInlineCError(templateSource, index, $"Got {arguments.Count} arguments, expected {expectedArgs}.", errorReportedElement);

                using (Emitter emitBuffer = new())
                {
                    replacer(irProgram, emitBuffer, arguments.ToArray());
                    templateSource = templateSource.Remove(index, i - index).Insert(index, emitBuffer.GetBuffered());
                }
            }
        }

        public string GetSource(string templateSource, IRProgram irProgram, Emitter.Promise? value = null, Emitter.Promise? agent = null)
        {   
            templateSource = templateSource.Replace("##ID", GetStandardIdentifier(irProgram));

            for (int i = 0; i < Declaration.TypeParameters.Count; i++)
            {
                templateSource = templateSource.Replace($"##{Declaration.TypeParameters[i].Name}_ID", TypeArguments[i].GetStandardIdentifier(irProgram));
                templateSource = templateSource.Replace($"##{Declaration.TypeParameters[i].Name}_CSRC", TypeArguments[i].GetCName(irProgram));
            }

            for (int i = 0; i < Declaration.TypeParameters.Count; i++)
            {
                ReplaceFunction(templateSource, irProgram, $"##COPY_{Declaration.TypeParameters[i].Name}", 2, (irProgram, emitter, args) => TypeArguments[i].EmitCopyValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1]), Declaration), Declaration);
                ReplaceFunction(templateSource, irProgram, $"##DESTROY_{Declaration.TypeParameters[i].Name}", 2, (irProgram, emitter, args) => TypeArguments[i].EmitFreeValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1])), Declaration);
                ReplaceFunction(templateSource, irProgram, $"##BORROW_{Declaration.TypeParameters[i].Name}", 2, (irProgram, emitter, args) => TypeArguments[i].EmitClosureBorrowValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1])), Declaration);
            }

            if (value != null)
                templateSource = templateSource.Replace("##VALUE", Emitter.GetPromiseSource(value));
            if (agent != null)
                templateSource = templateSource.Replace("##AGENT", Emitter.GetPromiseSource(agent));

            return templateSource;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ForeignFunctionCall
    {
        public string GetSource(string templateSource, IRProgram irProgram, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            foreach(TypeParameter typeParameter in ForeignCProcedure.TypeParameters)
            {
                templateSource = templateSource.Replace($"##{typeParameter.Name}_ID", typeArguments[typeParameter].GetStandardIdentifier(irProgram));
                templateSource = templateSource.Replace($"##{typeParameter.Name}_CSRC", typeArguments[typeParameter].GetCName(irProgram));
                ForeignCType.ReplaceFunction(templateSource, irProgram, $"##COPY_{typeParameter.Name}", 2, (irProgram, emitter, args) => typeArguments[typeParameter].EmitCopyValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1]), this), this);
                ForeignCType.ReplaceFunction(templateSource, irProgram, $"##DESTROY_{typeParameter.Name}", 2, (irProgram, emitter, args) => typeArguments[typeParameter].EmitFreeValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1])), this);
                ForeignCType.ReplaceFunction(templateSource, irProgram, $"##BORROW_{typeParameter.Name}", 2, (irProgram, emitter, args) => typeArguments[typeParameter].EmitClosureBorrowValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1])), this);
            }
            for(int i = 0; i < Arguments.Count; i++)
            {
                templateSource = templateSource.Replace($"##ARG{i}_TYPE_ID", Arguments[i].Type.GetStandardIdentifier(irProgram));
                templateSource = templateSource.Replace($"##ARG{i}_TYPE_CSRC", Arguments[i].Type.GetCName(irProgram));
                templateSource = templateSource.Replace($"##ARG{i}_VALUE_CSRC", Emitter.GetPromiseSource(argPromises[i]));
                ForeignCType.ReplaceFunction(templateSource, irProgram, $"##COPY_ARG{i}_TYPE", 2, (irProgram, emitter, args) => Arguments[i].Type.EmitCopyValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1]), this), this);
                ForeignCType.ReplaceFunction(templateSource, irProgram, $"##DESTROY_ARG{i}_TYPE", 2, (irProgram, emitter, args) => Arguments[i].Type.EmitFreeValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1])), this);
                ForeignCType.ReplaceFunction(templateSource, irProgram, $"##BORROW_ARG{i}_TYPE", 2, (irProgram, emitter, args) => Arguments[i].Type.EmitClosureBorrowValue(irProgram, emitter, (e) => e.Append(args[0]), (e) => e.Append(args[1])), this);
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            templateSource = templateSource.Replace("##AGENT", Emitter.GetPromiseSource(responsibleDestroyer));
            return templateSource;
        }
    }
}