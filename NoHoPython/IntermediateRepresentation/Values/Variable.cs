using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.Scoping
{
    public sealed partial class Variable : IScopeSymbol
    {
        public bool IsRecordSelf { get; private set; }

        public IType Type { get; private set; }
        public string Name { get; private set; }

        public ProcedureDeclaration ParentProcedure;
        public SymbolContainer ParentContainer => ParentProcedure;
        public IAstElement ErrorReportedElement { get; private set; }

        public string GetStandardIdentifier() => $"_nhp_var_{Name}";

        public Variable(IType type, string name, ProcedureDeclaration parentProcedure, bool isRecordSelf, IAstElement errorReportedElement)
        {
            Type = type;
            Name = name;
            ParentProcedure = parentProcedure;
            IsRecordSelf = isRecordSelf;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed class CSymbol : IScopeSymbol
    {
        public SymbolContainer ParentContainer { get; private set; }
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type { get; private set; }
        public string Name { get; private set; }

        public CSymbol(IType type, string name, SymbolContainer parentContainer, IAstElement errorReportedElement)
        {
            Type = type;
            Name = name;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public class VariableContainer : SymbolContainer
    {
        protected SymbolContainer parentContainer;

        public override bool IsGloballyNavigable => false;

        public VariableContainer(SymbolContainer parentContainer) : base()
        {
            this.parentContainer = parentContainer;
        }

        public override IScopeSymbol? FindSymbol(string identifier, Syntax.IAstElement errorReportedElement)
        {
            IScopeSymbol? result = base.FindSymbol(identifier, errorReportedElement);
            return result ?? (parentContainer?.FindSymbol(identifier, errorReportedElement));
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class VariableReference : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Variable.Type;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public Variable Variable { get; private set; }

        public VariableReference(Variable variable, bool isConstant, IAstElement errorReportedElement)
        {
            Variable = variable;
            IsConstant = isConstant;
            ErrorReportedElement = errorReportedElement;
        }

        public VariableReference(Tuple<Variable, bool> santizeResult, IAstElement errorReportedElement) : this(santizeResult.Item1, santizeResult.Item2, errorReportedElement)
        {

        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => throw new InvalidOperationException();
    }

    public sealed partial class VariableDeclaration : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => InitialValue.Type;
        public bool IsTruey => InitialValue.IsTruey;
        public bool IsFalsey => InitialValue.IsFalsey;

        public Variable Variable { get; private set; }
        public IRValue InitialValue { get; private set; }
        public bool WillRevaluate { get; private set; }

        public VariableDeclaration(string name, IRValue setValue, bool willRevaluate, AstIRProgramBuilder irBuilder, IAstElement errorReportedELement)
        {
            irBuilder.SymbolMarshaller.DeclareSymbol(Variable = new Variable(setValue.Type, name, irBuilder.ScopedProcedures.Peek(), false, errorReportedELement), errorReportedELement);
            irBuilder.SymbolMarshaller.CurrentCodeBlock.AddVariableDeclaration(this);
            InitialValue = setValue;
            WillRevaluate = willRevaluate;
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

    public sealed partial class CSymbolReference : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => CSymbol.Type;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public CSymbol CSymbol { get; private set; }

        public CSymbolReference(CSymbol cSymbol, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            CSymbol = cSymbol;
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new CSymbolReference(CSymbol, ErrorReportedElement);
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class CSymbolDeclaration : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        
        public CSymbol CSymbol { get; private set; }

        public CSymbolDeclaration(CSymbol cSymbol, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            CSymbol = cSymbol;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class VariableReference
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
            return valueSymbol is Variable variable
                ? new IntermediateRepresentation.Values.VariableReference(irBuilder.ScopedProcedures.Peek().SanitizeVariable(variable, false, this), this)
                : valueSymbol is ProcedureDeclaration procedureDeclaration
                ? (IRValue)new AnonymizeProcedure(procedureDeclaration, expectedType == null ? false : expectedType is HandleType, this, irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek())
                : valueSymbol is CSymbol cSymbol
                ? new CSymbolReference(cSymbol, this)
                : valueSymbol is IType emptyType
                ? new EmptyTypeLiteral(emptyType, this)
                : throw new NotAVariableException(valueSymbol, this);
        }
    }

    partial class SetVariable
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            try
            {
                IScopeSymbol valueSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
                return valueSymbol is Variable variable
                    ? (IRValue)new IntermediateRepresentation.Values.SetVariable(irBuilder.ScopedProcedures.Peek().SanitizeVariable(variable, true, this).Item1, SetValue.GenerateIntermediateRepresentationForValue(irBuilder, variable.Type, willRevaluate), this)
                    : throw new NotAVariableException(valueSymbol, this);
            }
            catch (SymbolNotFoundException)
            {
                return new VariableDeclaration(Name, SetValue.GenerateIntermediateRepresentationForValue(irBuilder, expectedType, willRevaluate), willRevaluate, irBuilder, this);
            }
        }
    }

    partial class CSymbolDeclaration
    {
        private CSymbol CSymbol = null;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) 
        {
            IType type = Type == null ? Primitive.Integer : Type.ToIRType(irBuilder, this);
            irBuilder.SymbolMarshaller.DeclareSymbol(CSymbol = new CSymbol(type, Name, irBuilder.SymbolMarshaller.CurrentScope, this), this);
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => new IntermediateRepresentation.Statements.CSymbolDeclaration(CSymbol, this);
    }
}