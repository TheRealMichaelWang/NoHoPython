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

        public override string ToString() => Name;
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

        public override string ToString() => $"{Name} = {SetValue}";
        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}{this}";
    }

    public sealed partial class CSymbolDeclaration : IAstStatement
    {
        public SourceLocation SourceLocation { get; private set; }
        public readonly string Name;
        public AstType? Type { get; private set; }

        public CSymbolDeclaration(string name, AstType? type, SourceLocation sourceLocation)
        {
            SourceLocation = sourceLocation;
            Name = name;
            Type = type;
        }

        public string ToString(int indent) => $"{IAstStatement.Indent(indent)}cdef {Name}{(Type == null ? string.Empty : $" {Type}")}";
    }
}
