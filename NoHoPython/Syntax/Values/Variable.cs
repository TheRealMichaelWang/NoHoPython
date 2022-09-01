namespace NoHoPython.Syntax.Values
{
    public sealed partial class VariableReference : IAstValue
    {
        public SourceLocation SourceLocation { get; private set; }
        public readonly string Name;

        public VariableReference(string name, SourceLocation sourceLocation)
        {
            Name = name;
            SourceLocation = sourceLocation;
        }
    }

    public sealed partial class SetVariable : IAstValue, IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }
        public readonly string Name;
        public IAstValue SetValue { get; private set; }

        public SetVariable(string name, IAstValue setValue, SourceLocation sourceLocation)
        {
            Name = name;
            SetValue = setValue;
            SourceLocation = sourceLocation;
        }
    }
}
