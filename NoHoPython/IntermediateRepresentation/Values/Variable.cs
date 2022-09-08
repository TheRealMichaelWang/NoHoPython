using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class NotAVariableException : Exception
    {
        public IScopeSymbol ScopeSymbol { get; private set; }

        public NotAVariableException(IScopeSymbol scopeSymbol) : base($"{scopeSymbol.Name} is not a variable. Rather it is a {scopeSymbol}.")
        {
            ScopeSymbol = scopeSymbol;
        }
    }
}

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
            irBuilder.SymbolMarshaller.DeclareSymbol(Variable = new Variable(setValue.Type, name, irBuilder.ScopedProcedures.Peek()));
            InitialValue = setValue;
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

        public IntermediateRepresentation.Statements.ProcedureDeclaration ParentProcedure { get; private set; }

        public Variable(IType type, string name, IntermediateRepresentation.Statements.ProcedureDeclaration parentProcedure)
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

namespace NoHoPython.Syntax.Values
{
    partial class VariableReference
    {
        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder)
        {
            IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name);
            if (valueSymbol is Variable variable)
                return new IntermediateRepresentation.Values.VariableReference(variable);
            else if (valueSymbol is IntermediateRepresentation.Statements.ProcedureDeclaration procedureDeclaration)
                return new AnonymizeProcedure(procedureDeclaration);
            else
                throw new NotAVariableException(valueSymbol);
        }
    }

    partial class SetVariable
    {
        public void ForwardTypeDeclare(IRProgramBuilder irBuilder) { }
        public void ForwardDeclare(IRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(IRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder);

        public IRValue GenerateIntermediateRepresentationForValue(IRProgramBuilder irBuilder)
        {
            try
            {
                IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name);
                if (valueSymbol is Variable variable)
                    return new IntermediateRepresentation.Values.SetVariable(variable, SetValue.GenerateIntermediateRepresentationForValue(irBuilder));
                else
                    throw new NotAVariableException(valueSymbol);
            }
            catch (SymbolNotFoundException)
            {
                return new VariableDeclaration(Name, SetValue.GenerateIntermediateRepresentationForValue(irBuilder), irBuilder);
            }
        }
    }
}