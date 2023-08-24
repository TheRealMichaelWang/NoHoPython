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

        public ForeignInlineCError(string inlineCSource, int index, string inlineMessage, ForeignCType foreignCType) : base(foreignCType.Declaration, "An error occured with your inline C code.")
        {
            InlineCSource = inlineCSource;
            Index = index;
            InlineMessage = inlineMessage;
        }

        public override void Print()
        {
            base.Print();

        }
    }
}

namespace NoHoPython.Typing
{
    partial class ForeignCType
    {
        public string GetSource(string templateSource, IRProgram irProgram, string? value = null, string? agent = null)
        {
            void replaceFunction(string name, int expectedArgs, Action<IRProgram, IEmitter, string[]> replacer)
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
                                throw new ForeignInlineCError(templateSource, i, $"Call to {name} missing open paren.", this);

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
                        throw new ForeignInlineCError(templateSource, i, "Expected close paren instead of EOF.", this);
                    if (arguments.Count != expectedArgs)
                        throw new ForeignInlineCError(templateSource, index, $"Got {arguments.Count} arguments, expected {expectedArgs}.", this);

                    BufferedEmitter bufferedEmitter = new();
                    replacer(irProgram, bufferedEmitter, arguments.ToArray());

                    templateSource = templateSource.Remove(index, i - index).Insert(index, bufferedEmitter.ToString());
                }
            }

            templateSource = templateSource.Replace("##ID", GetStandardIdentifier(irProgram));

            for (int i = 0; i < Declaration.TypeParameters.Count; i++)
            {
                templateSource = templateSource.Replace($"##{Declaration.TypeParameters[i].Name}_ID", TypeArguments[i].GetStandardIdentifier(irProgram));
                templateSource = templateSource.Replace($"##{Declaration.TypeParameters[i].Name}_CSRC", TypeArguments[i].GetCName(irProgram));
            }

            for (int i = 0; i < Declaration.TypeParameters.Count; i++)
            {
                replaceFunction($"##COPY_{Declaration.TypeParameters[i].Name}", 2, (irProgram, emitter, args) => TypeArguments[i].EmitCopyValue(irProgram, emitter, args[0], args[1]));
                replaceFunction($"##DESTROY_{Declaration.TypeParameters[i].Name}", 2, (irProgram, emitter, args) => TypeArguments[i].EmitFreeValue(irProgram, emitter, args[0], args[1]));
                replaceFunction($"##MOVE_{Declaration.TypeParameters[i].Name}", 3, (irProgram, emitter, args) => TypeArguments[i].EmitMoveValue(irProgram, emitter, args[0], args[1], args[3]));
                replaceFunction($"##BORROW_{Declaration.TypeParameters[i].Name}", 2, (irProgram, emitter, args) => TypeArguments[i].EmitClosureBorrowValue(irProgram, emitter, args[0], args[1]));
            }

            if (value != null)
                templateSource = templateSource.Replace("##VALUE", value);
            if (agent != null)
                templateSource = templateSource.Replace("##AGENT", agent);

            return templateSource;
        }
    }
}
