using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
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

        public VariableDeclaration(string name, IRValue setValue, Syntax.AstIRProgramBuilder irBuilder, Syntax.IAstElement errorReportedElement)
        {
            irBuilder.SymbolMarshaller.DeclareSymbol(Variable = new Variable(setValue.Type, name, irBuilder.ScopedProcedures.Peek()), errorReportedElement);
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

        public override IScopeSymbol? FindSymbol(string identifier, Syntax.IAstElement errorReportedElement)
        {
            IScopeSymbol? result = base.FindSymbol(identifier, errorReportedElement);
            return result ?? (parentContainer == null ? null : parentContainer.FindSymbol(identifier, errorReportedElement));
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class VariableReference
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder)
        {
            IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
            return valueSymbol is Variable variable
                ? new IntermediateRepresentation.Values.VariableReference(variable)
                : valueSymbol is IntermediateRepresentation.Statements.ProcedureDeclaration procedureDeclaration
                ? (IRValue)new AnonymizeProcedure(procedureDeclaration)
                : throw new NotAVariableException(valueSymbol, this);
        }
    }

    partial class SetVariable
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder)
        {
            try
            {
                IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
                return valueSymbol is Variable variable
                    ? (IRValue)new IntermediateRepresentation.Values.SetVariable(variable, SetValue.GenerateIntermediateRepresentationForValue(irBuilder))
                    : throw new NotAVariableException(valueSymbol, this);
            }
            catch (SymbolNotFoundException)
            {
                return new VariableDeclaration(Name, SetValue.GenerateIntermediateRepresentationForValue(irBuilder), irBuilder, this);
            }
        }
    }
}