using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class VariableReference : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get => Variable.Type; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public Variable Variable { get; private set; }

        public VariableReference(Variable variable, IAstElement errorReportedElement)
        {
            Variable = variable;
            ErrorReportedElement = errorReportedElement;
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }

    public sealed partial class VariableDeclaration : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get => InitialValue.Type; }
        public bool IsTruey => InitialValue.IsTruey;
        public bool IsFalsey => InitialValue.IsFalsey;

        public Variable Variable { get; private set; }
        public IRValue InitialValue { get; private set; }

        public VariableDeclaration(string name, IRValue setValue, AstIRProgramBuilder irBuilder, IAstElement errorReportedELement)
        {
            irBuilder.SymbolMarshaller.DeclareSymbol(Variable = new Variable(setValue.Type, name, irBuilder.ScopedProcedures.Peek(), false), errorReportedELement);
            irBuilder.SymbolMarshaller.CurrentCodeBlock.DeclaredVariables.Add(Variable);
            InitialValue = setValue;
            ErrorReportedElement = errorReportedELement;
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }

    public sealed partial class SetVariable : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => SetValue.Type;
        public bool IsTruey => SetValue.IsTruey;
        public bool IsFalsey => SetValue.IsFalsey;

        public Variable Variable { get; private set; }
        public IRValue SetValue { get; private set; }

        public SetVariable(Variable variable, IRValue value, IAstElement errorReportedElement)
        {
            Variable = variable;
            ErrorReportedElement = errorReportedElement;
            SetValue = ArithmeticCast.CastTo(value, Variable.Type);
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }
}

namespace NoHoPython.Scoping
{
    public sealed class Variable : IScopeSymbol
    {
        public bool IsGloballyNavigable => false;

        public bool IsRecordSelf { get; private set; }

        public IType Type { get; private set; }
        public string Name { get; private set; }

        public ProcedureDeclaration ParentProcedure;
        public SymbolContainer? ParentContainer => ParentProcedure;

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_var_{Name}";

        public Variable(IType type, string name, ProcedureDeclaration parentProcedure, bool isRecordSelf)
        {
            Type = type;
            Name = name;
            ParentProcedure = parentProcedure;
            IsRecordSelf = isRecordSelf;
        }
    }

    public class VariableContainer : SymbolContainer
    {
        protected SymbolContainer parentContainer;

        public VariableContainer(SymbolContainer parentContainer) : base()
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
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
            return valueSymbol is Variable variable
                ? new IntermediateRepresentation.Values.VariableReference(irBuilder.ScopedProcedures.Peek().SanitizeVariable(variable, false, this), this)
                : valueSymbol is ProcedureDeclaration procedureDeclaration
                ? (IRValue)new AnonymizeProcedure(procedureDeclaration, this, irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek())
                : throw new NotAVariableException(valueSymbol, this);
        }
    }

    partial class SetVariable
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            try
            {
                IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
                return valueSymbol is Variable variable
                    ? (IRValue)new IntermediateRepresentation.Values.SetVariable(irBuilder.ScopedProcedures.Peek().SanitizeVariable(variable, true, this), SetValue.GenerateIntermediateRepresentationForValue(irBuilder, variable.Type), this)
                    : throw new NotAVariableException(valueSymbol, this);
            }
            catch (SymbolNotFoundException)
            {
                return new VariableDeclaration(Name, SetValue.GenerateIntermediateRepresentationForValue(irBuilder, expectedType), irBuilder, this);
            }
        }
    }
}