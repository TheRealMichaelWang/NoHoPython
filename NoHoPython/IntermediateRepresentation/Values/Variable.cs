using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class VariableReference : IRValue
    {
        public IType Type { get => Variable.Type; }

        public Variable Variable { get; private set; }

        public VariableReference(Variable variable)
        {
            Variable = variable;
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }

    public sealed partial class VariableDeclaration : IRValue, IRStatement
    {
        public IType Type { get => InitialValue.Type; }

        public Variable Variable { get; private set; }
        public IRValue InitialValue { get; private set; }

        public VariableDeclaration(string name, IRValue setValue, IRProgramBuilder irBuilder)
        {
            if (irBuilder.SymbolMarshaller.CurrentContainer is ProcedureDeclaration procedureDeclaration)
            {
                irBuilder.SymbolMarshaller.DeclareSymbol(Variable = new Variable(setValue.Type, name, procedureDeclaration));
                InitialValue = setValue;
            }
            else
                throw new InvalidOperationException();
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }

    public sealed partial class SetVariable : IRValue, IRStatement
    {
        public IType Type { get => SetValue.Type; }

        public Variable Variable { get; private set; }
        public IRValue SetValue { get; private set; }

        public SetVariable(Variable variable, IRValue value)
        {
            Variable = variable;
            SetValue = ArithmeticCast.CastTo(value, Variable.Type);
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }
}

namespace NoHoPython.Scoping
{
    public sealed class Variable : IScopeSymbol
    {
        public bool IsGloballyNavigable => false;

        public IType Type { get; private set; }
        public string Name { get; private set; }

        public ProcedureDeclaration ParentProcedure { get; private set; }

        public Variable(IType type, string name, ProcedureDeclaration parentProcedure)
        {
            Type = type;
            Name = name;
            ParentProcedure = parentProcedure;
        }
    }

    public class VariableContainer : SymbolContainer
    {
        private SymbolContainer? parentContainer;

        public VariableContainer(SymbolContainer? parentContainer, List<Variable> scopeSymbols) : base(scopeSymbols.ConvertAll<IScopeSymbol>((Variable var) => var).ToList())
        {
            this.parentContainer = parentContainer;
        }

        public override IScopeSymbol? FindSymbol(string identifier)
        {
            IScopeSymbol? result = base.FindSymbol(identifier);
            if (result == null)
            {
                if (parentContainer == null)
                    return null;
                else
                    return parentContainer.FindSymbol(identifier);
            }
            return result;
        }
    }
}