using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class ProcedureDeclaration : CodeBlock, IScopeSymbol, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer? ParentContainer { get; private set; }

        public bool IsGloballyNavigable => true;
        public string Name { get; private set; }

        public bool IsCompileHead => TypeParameters.Count == 0 && CapturedVariables.Count == 0 && isTopLevelStatement;
        private bool isTopLevelStatement;

        public readonly List<Typing.TypeParameter> TypeParameters;
        public List<Variable>? Parameters { get; private set; }
        public List<Variable> CapturedVariables { get; private set; }

        public IType ReturnType { get; private set; }

        public ProcedureDeclaration(string name, List<Typing.TypeParameter> typeParameters, IType returnType, SymbolContainer? parentContainer, bool isTopLevelStatement, IAstElement errorReportedElement) : base(parentContainer)
        {
            Name = name;
            TypeParameters = typeParameters;
            ReturnType = returnType;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
            CapturedVariables = new List<Variable>();
            this.isTopLevelStatement = isTopLevelStatement;
        }

        public void DelayedLinkSetParameters(List<Variable> parameters)
        {
            if (Parameters != null)
                throw new InvalidOperationException();
            Parameters = parameters;
        }

        public Variable SanitizeVariable(Variable variable, bool willStet, IAstElement errorReportedElement)
        {
            if (variable.ParentProcedure == this)
                return variable;
            if (!CapturedVariables.Contains(variable))
            {
                CapturedVariables.Add(variable);
                if (willStet)
                    throw new CannotMutateCapturedVaraible(variable, errorReportedElement);
            }
            return variable;
        }
    }

    public sealed partial class ProcedureReference
    {
        public IType ReturnType { get; private set; }
        private IAstElement errorReportedElement;

        public readonly List<IType> ParameterTypes;
        private Dictionary<Typing.TypeParameter, IType> typeArguments;

        public ProcedureDeclaration ProcedureDeclaration { get; private set; }

        public bool IsAnonymous { get; private set; }

        private static Dictionary<Typing.TypeParameter, IType> MatchTypeArguments(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments, IType? returnType, IAstElement errorReportedElement)
        {
            Dictionary<Typing.TypeParameter, IType> typeArguments = new();

            if (procedureDeclaration.Parameters == null)
                throw new InvalidOperationException();
            if (arguments.Count != procedureDeclaration.Parameters.Count)
                throw new UnexpectedArgumentsException(arguments.ConvertAll((arg) => arg.Type), procedureDeclaration.Parameters, errorReportedElement);

            for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                arguments[i] = procedureDeclaration.Parameters[i].Type.MatchTypeArgumentWithValue(typeArguments, arguments[i]);
            if (returnType != null)
                procedureDeclaration.ReturnType.MatchTypeArgumentWithType(typeArguments, returnType, errorReportedElement);

            return typeArguments;
        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments, IType? returnType, bool isAnonymous, IAstElement errorReportedElement) : this(MatchTypeArguments(procedureDeclaration, arguments, returnType, errorReportedElement), procedureDeclaration, isAnonymous, errorReportedElement)
        {

        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, bool isAnonymous, IAstElement errorReportedElement) : this(new Dictionary<Typing.TypeParameter, IType>(), procedureDeclaration, isAnonymous, errorReportedElement)
        {
            this.errorReportedElement = errorReportedElement;
        }

        private ProcedureReference(Dictionary<Typing.TypeParameter, IType> typeArguments, ProcedureDeclaration procedureDeclaration, bool isAnonymous, IAstElement errorReportedElement)
        {
            ProcedureDeclaration = procedureDeclaration;
            IsAnonymous = isAnonymous;
            this.typeArguments = typeArguments;
            this.errorReportedElement = errorReportedElement;

#pragma warning disable CS8604 // Panic should happen at runtime. Parameters might (but shouldn't unless bug) be null b/c of linking.
            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(typeArguments)).ToList();
#pragma warning restore CS8604 
            
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(typeArguments);
            anonProcedureType = new ProcedureType(ReturnType, ParameterTypes);
        }

        public ProcedureReference SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs)
        {
            Dictionary<Typing.TypeParameter, IType> newTypeargs = new(typeargs.Count);
            foreach (KeyValuePair<Typing.TypeParameter, IType> typearg in typeArguments)
                newTypeargs.Add(typearg.Key, typearg.Value.SubstituteWithTypearg(typeargs));
            foreach (KeyValuePair<Typing.TypeParameter, IType> typearg in typeargs)
                if (!typeArguments.ContainsKey(typearg.Key))
                    newTypeargs.Add(typearg.Key, typearg.Value);
            return new ProcedureReference(newTypeargs, ProcedureDeclaration, IsAnonymous, errorReportedElement);
        }

        public bool IsCompatibleWith(ProcedureReference procedureReference)
        {
            if (ProcedureDeclaration != procedureReference.ProcedureDeclaration)
                return false;
            if (IsAnonymous != procedureReference.IsAnonymous)
                return false;
            foreach (Typing.TypeParameter typeParameter in ProcedureDeclaration.TypeParameters)
                if (!procedureReference.typeArguments[typeParameter].IsCompatibleWith(typeArguments[typeParameter]))
                    return false;
            return true;
        }
    }

    public sealed partial class ReturnStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IRValue ToReturn { get; private set; }

        private CodeBlock parentCodeBlock;

#pragma warning disable CS8618
        public ReturnStatement(IRValue toReturn, AstIRProgramBuilder irBuilder, IAstStatement errorReportedStatement)
#pragma warning restore CS8618
        {
            ToReturn = ArithmeticCast.CastTo(toReturn, irBuilder.ScopedProcedures.Peek().ReturnType);
            ErrorReportedElement = errorReportedStatement;
#pragma warning disable CS8600 // null runtime errors not possible during parsing
#pragma warning disable CS8601
            this.parentCodeBlock = (CodeBlock)irBuilder.CurrentMasterScope;
#pragma warning restore CS8601 
#pragma warning restore CS8600
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class LinkedProcedureCall : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => Procedure.ReturnType;

        public ProcedureReference Procedure { get; private set; }
        public readonly List<IRValue> Arguments;

        public LinkedProcedureCall(ProcedureDeclaration procedure, List<IRValue> arguments, IType? returnType, IAstElement errorReportedElement) : this(new ProcedureReference(procedure, arguments, returnType, false, errorReportedElement), arguments, errorReportedElement)
        {

        }

        private LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments, IAstElement errorReportedElement)
        {
            Procedure = procedure;
            Arguments = arguments;
            ErrorReportedElement = errorReportedElement;
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    public sealed partial class AnonymousProcedureCall : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => ProcedureType.ReturnType;

        public IRValue ProcedureValue { get; private set; }
        public readonly List<IRValue> Arguments;
        public ProcedureType ProcedureType { get; private set; }

        public AnonymousProcedureCall(IRValue procedureValue, List<IRValue> arguments, IAstElement errorReportedElement)
        {
            ProcedureValue = procedureValue;
            ErrorReportedElement = errorReportedElement;

            if (procedureValue.Type is ProcedureType procedureType)
            {
                ProcedureType = procedureType;

                if (procedureType.ParameterTypes.Count != arguments.Count)
                    throw new UnexpectedTypeArgumentsException(procedureType.ParameterTypes.Count, arguments.Count, errorReportedElement);
                for (int i = 0; i < procedureType.ParameterTypes.Count; i++)
                    arguments[i] = ArithmeticCast.CastTo(arguments[i], procedureType.ParameterTypes[i]);
                Arguments = arguments;
            }
            else
                throw new UnexpectedTypeException(procedureValue.Type, errorReportedElement);
            ErrorReportedElement = errorReportedElement;
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new AnonymousProcedureCall(ProcedureValue.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    public sealed partial class AnonymizeProcedure : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get => new ProcedureType(Procedure.ReturnType, Procedure.ParameterTypes); }

        public ProcedureReference Procedure { get; private set; }

        private AnonymizeProcedure(ProcedureReference procedure, IAstElement errorReportedElement)
        {
            Procedure = procedure;
            ErrorReportedElement = errorReportedElement;
        }

        public AnonymizeProcedure(ProcedureDeclaration procedure, IAstElement errorReportedElement) : this(new ProcedureReference(procedure, true, errorReportedElement), errorReportedElement)
        {
            
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new AnonymizeProcedure(Procedure.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class ReturnStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            return irBuilder.ScopedProcedures.Count == 0
                ? throw new UnexpectedReturnStatement(this)
                : (IRStatement)new IntermediateRepresentation.Statements.ReturnStatement(ReturnValue.GenerateIntermediateRepresentationForValue(irBuilder, irBuilder.ScopedProcedures.Peek().ReturnType), irBuilder, this);
        }
    }

    partial class ProcedureDeclaration
    {
        private IntermediateRepresentation.Statements.ProcedureDeclaration IRProcedureDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            IRProcedureDeclaration = new(Name, typeParameters, AnnotatedReturnType == null ? Primitive.Nothing : AnnotatedReturnType.ToIRType(irBuilder, this), irBuilder.CurrentMasterScope, irBuilder.ScopedProcedures.Count == 0 && irBuilder.ScopedRecordDeclaration == null, this);

            List<Variable> parameters = Parameters.ConvertAll((ProcedureParameter parameter) => new Variable(parameter.Type.ToIRType(irBuilder, this), parameter.Identifier, IRProcedureDeclaration, false));
            IRProcedureDeclaration.DelayedLinkSetParameters(parameters);

            SymbolContainer? oldMasterScope = irBuilder.CurrentMasterScope;
            irBuilder.SymbolMarshaller.DeclareSymbol(IRProcedureDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

            foreach (Variable parameter in parameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);
            if (oldMasterScope is IntermediateRepresentation.Statements.RecordDeclaration parentRecord)
            {
                Variable selfVariable = new Variable(parentRecord.SelfType, "self", IRProcedureDeclaration, true);
                irBuilder.SymbolMarshaller.DeclareSymbol(selfVariable, this);
                IRProcedureDeclaration.CapturedVariables.Add(selfVariable);
            }

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            IAstStatement.ForwardDeclareBlock(irBuilder, Statements);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();

            irBuilder.AddProcDeclaration(IRProcedureDeclaration);
        }

#pragma warning disable CS8602 // Only called after ForwardDeclare, when parameter is initialized
        public IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty GenerateProperty() => new IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty(Name, new ProcedureType(IRProcedureDeclaration.ReturnType, IRProcedureDeclaration.Parameters.ConvertAll((param) => param.Type)), true);
#pragma warning restore CS8602

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

            IRProcedureDeclaration.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, Statements));

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();
            return IRProcedureDeclaration;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class NamedFunctionCall
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            IScopeSymbol procedureSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
            return procedureSymbol is ProcedureDeclaration procedureDeclaration
                ? (IRValue)new LinkedProcedureCall(procedureDeclaration, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)), expectedType, this)
                : throw new NotAProcedureException(procedureSymbol, this);
        }
    }

    partial class AnonymousFunctionCall
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)
            GenerateIntermediateRepresentationForValue(irBuilder, null);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType)
        {
            return new AnonymousProcedureCall(ProcedureValue.GenerateIntermediateRepresentationForValue(irBuilder, null), Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)), this);
        }
    }
}