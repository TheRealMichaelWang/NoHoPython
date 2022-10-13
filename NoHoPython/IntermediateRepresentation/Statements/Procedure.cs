using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private void LinkCapturedVariables()
        {
            Dictionary<ProcedureDeclaration, HashSet<ProcedureDeclaration>> dependentProcedures = new(ProcedureDeclarations.Count);
            HashSet<ProcedureDeclaration> unprocessedProcedures = new(ProcedureDeclarations);
            foreach (ProcedureDeclaration procedureDeclaration in ProcedureDeclarations)
                dependentProcedures.Add(procedureDeclaration, new());
            foreach (ProcedureDeclaration procedureDeclaration in ProcedureDeclarations)
                foreach (ProcedureDeclaration callSite in procedureDeclaration.CallSiteProcedures)
                    dependentProcedures[callSite].Add(procedureDeclaration);

            while (unprocessedProcedures.Count > 0)
            {
                foreach(ProcedureDeclaration procedureDeclaration in unprocessedProcedures)
                    if (dependentProcedures[procedureDeclaration].Count == 0)
                    {
                        foreach (ProcedureDeclaration callSite in procedureDeclaration.CallSiteProcedures)
                        {
                            foreach (Variable capturedVariable in procedureDeclaration.CapturedVariables)
                                if (!callSite.HasVariable(capturedVariable))
                                    callSite.CapturedVariables.Add(capturedVariable);
                            dependentProcedures[callSite].Remove(procedureDeclaration);
                        }
                        unprocessedProcedures.Remove(procedureDeclaration);
                    }
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class ProcedureDeclaration : CodeBlock, IScopeSymbol, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }

        public bool IsGloballyNavigable => true;
        public string Name { get; private set; }

        public bool IsCompileHead => TypeParameters.Count == 0 && CapturedVariables.Count == 0 && parentContainer.IsHeadContainer;

        public readonly List<Typing.TypeParameter> TypeParameters;
        public List<Variable>? Parameters { get; private set; }
        public List<Variable> CapturedVariables { get; private set; }
        public List<ProcedureDeclaration> CallSiteProcedures { get; private set; }

        public IType ReturnType { get; private set; }

        public ProcedureDeclaration(string name, List<Typing.TypeParameter> typeParameters, IType returnType, SymbolContainer parentContainer, IAstElement errorReportedElement) : base(parentContainer, false)
        {
            Name = name;
            TypeParameters = typeParameters;
            ReturnType = returnType;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
            CapturedVariables = new List<Variable>();
            CallSiteProcedures = new List<ProcedureDeclaration>();
        }

        public bool HasVariable(Variable variable)
        {
            if (variable.ParentProcedure == this)
                return true;
            foreach (Variable capturedVariable in CapturedVariables)
                if (variable.Name == capturedVariable.Name && variable.Type.IsCompatibleWith(capturedVariable.Type))
                    return true;
            return false;
        }

        public void DelayedLinkSetParameters(List<Variable> parameters)
        {
            if (Parameters != null)
                throw new InvalidOperationException();
            Parameters = parameters;
        }

        public override void DelayedLinkSetStatements(List<IRStatement> statements)
        {
            base.DelayedLinkSetStatements(statements);
            if (ReturnType is not NothingType && !CodeBlockAllCodePathsReturn())
                throw new NotAllCodePathsReturnError(ErrorReportedElement);
        }

        public Variable SanitizeVariable(Variable variable, bool willStet, IAstElement errorReportedElement)
        {
            if (variable.ParentProcedure == this)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (Parameters.Contains(variable) && willStet)
                    throw new CannotMutateVaraible(variable, errorReportedElement);
#pragma warning restore CS8602
            }
            else if (!CapturedVariables.Contains(variable))
            {
                CapturedVariables.Add(variable);
                if (willStet)
                    throw new CannotMutateVaraible(variable, errorReportedElement);
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

            if (procedureDeclaration.TypeParameters.Count > 0)
            {
                for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                    arguments[i] = procedureDeclaration.Parameters[i].Type.MatchTypeArgumentWithValue(typeArguments, arguments[i]);

                if (returnType != null)
                    procedureDeclaration.ReturnType.MatchTypeArgumentWithType(typeArguments, returnType, errorReportedElement);
            }
            else
            {
                for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                    arguments[i] = ArithmeticCast.CastTo(arguments[i], procedureDeclaration.Parameters[i].Type);
            }

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
            Dictionary<Typing.TypeParameter, IType> newTypeargs = new(typeArguments.Count + typeargs.Count);
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
            if (procedureReference.typeArguments.Count != typeArguments.Count)
                return false;

            foreach (KeyValuePair<Typing.TypeParameter, IType> typearg in typeArguments)
                if (procedureReference.typeArguments.ContainsKey(typearg.Key))
                {
                    if (!typearg.Value.IsCompatibleWith(procedureReference.typeArguments[typearg.Key]))
                        return false;
                }
                else
                    return false;

            return true;
        }
    }

    public sealed partial class ReturnStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IRValue ToReturn { get; private set; }

        private List<Variable> activeVariables;

        public ReturnStatement(IRValue toReturn, AstIRProgramBuilder irBuilder, IAstStatement errorReportedStatement)
        {
            ToReturn = ArithmeticCast.CastTo(toReturn, irBuilder.ScopedProcedures.Peek().ReturnType);
            activeVariables = irBuilder.SymbolMarshaller.CurrentCodeBlock.GetCurrentLocals();
            ErrorReportedElement = errorReportedStatement;
        }
    }

    public sealed partial class ForeignCProcedureDeclaration : IRStatement, IScopeSymbol
    {
        public SymbolContainer ParentContainer { get; private set; }
        public IAstElement ErrorReportedElement { get; private set; }
        public bool IsGloballyNavigable => true;

        public string Name { get; private set; }

        public readonly List<IType> ParameterTypes;
        public IType ReturnType { get; private set; }

        public ForeignCProcedureDeclaration(string name, List<IType> parameterTypes, IType returnType, IAstElement errorReportedElement, SymbolContainer parentContainer)
        {
            Name = name;
            ErrorReportedElement = errorReportedElement;
            ParentContainer = parentContainer;
            ParameterTypes = parameterTypes;
            ReturnType = returnType;

            foreach (IType parameterType in parameterTypes)
                if (!parameterType.IsNativeCType)
                    throw new UnexpectedTypeException(parameterType, errorReportedElement);
            if (!ReturnType.IsNativeCType)
                throw new UnexpectedTypeException(ReturnType, errorReportedElement);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public abstract partial class ProcedureCall : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public abstract IType Type { get; }

        public readonly List<IRValue> Arguments;

        public ProcedureCall(List<IType> expectedParameters, List<IRValue> arguments, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Arguments = arguments;

            if (expectedParameters.Count != arguments.Count)
                throw new UnexpectedTypeArgumentsException(expectedParameters.Count, arguments.Count, errorReportedElement);
            for(int i = 0; i < expectedParameters.Count; i++)
                Arguments[i] = ArithmeticCast.CastTo(Arguments[i], expectedParameters[i]);
        }

        public abstract IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs);
    }

    public sealed partial class LinkedProcedureCall : ProcedureCall
    {
        public override IType Type => Procedure.ReturnType;

        public ProcedureReference Procedure { get; private set; }
        private ProcedureDeclaration? parentProcedure;

        public LinkedProcedureCall(ProcedureDeclaration procedure, List<IRValue> arguments, ProcedureDeclaration? parentProcedure, IType? returnType, IAstElement errorReportedElement) : this(new ProcedureReference(procedure, arguments, returnType, false, errorReportedElement), arguments, parentProcedure, errorReportedElement)
        {

        }

        private LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments, ProcedureDeclaration? parentProcedure, IAstElement errorReportedElement) : base(procedure.ParameterTypes, arguments, errorReportedElement)
        {
            Procedure = procedure;
            this.parentProcedure = parentProcedure;

            if (parentProcedure != null)
                procedure.ProcedureDeclaration.CallSiteProcedures.Add(parentProcedure);
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), parentProcedure, ErrorReportedElement);
    }

    public sealed partial class AnonymousProcedureCall : ProcedureCall
    {
        public override IType Type => ProcedureType.ReturnType;

        public IRValue ProcedureValue { get; private set; }
        public ProcedureType ProcedureType { get; private set; }

        public AnonymousProcedureCall(IRValue procedureValue, List<IRValue> arguments, IAstElement errorReportedElement) : base((procedureValue.Type is ProcedureType procedureType ? procedureType : throw new UnexpectedTypeException(procedureValue.Type, errorReportedElement)).ParameterTypes, arguments, errorReportedElement)
        {
            ProcedureValue = procedureValue;
            ProcedureType = procedureType;
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new AnonymousProcedureCall(ProcedureValue.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    public sealed partial class AnonymizeProcedure : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get => new ProcedureType(Procedure.ReturnType, Procedure.ParameterTypes); }

        public ProcedureReference Procedure { get; private set; }
        private ProcedureDeclaration? parentProcedure;

        private AnonymizeProcedure(ProcedureReference procedure, ProcedureDeclaration? parentProcedure, IAstElement errorReportedElement)
        {
            Procedure = procedure;
            ErrorReportedElement = errorReportedElement;
            this.parentProcedure = parentProcedure;

            if(parentProcedure != null)
                procedure.ProcedureDeclaration.CallSiteProcedures.Add(parentProcedure);
        }

        public AnonymizeProcedure(ProcedureDeclaration procedure, IAstElement errorReportedElement, ProcedureDeclaration? parentDeclaration) : this(new ProcedureReference(procedure, true, errorReportedElement), parentDeclaration, errorReportedElement)
        {
            
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new AnonymizeProcedure(Procedure.SubstituteWithTypearg(typeargs), parentProcedure, ErrorReportedElement);
    }

    public sealed partial class ForeignFunctionCall : ProcedureCall
    {
        public override IType Type => ForeignCProcedure.ReturnType;

        public ForeignCProcedureDeclaration ForeignCProcedure { get; private set; }

        public ForeignFunctionCall(ForeignCProcedureDeclaration foreignCProcedure, List<IRValue> arguments, IAstElement errorReportedElement) : base(foreignCProcedure.ParameterTypes, arguments, errorReportedElement)
        {
            ForeignCProcedure = foreignCProcedure;
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new ForeignFunctionCall(ForeignCProcedure, Arguments.ConvertAll((argument) => argument.SubstituteWithTypearg(typeargs)), ErrorReportedElement);
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

            IRProcedureDeclaration = new(Name, typeParameters, AnnotatedReturnType == null ? Primitive.Nothing : AnnotatedReturnType.ToIRType(irBuilder, this), irBuilder.CurrentMasterScope, this);

            SymbolContainer? oldMasterScope = irBuilder.CurrentMasterScope;

            irBuilder.SymbolMarshaller.DeclareSymbol(IRProcedureDeclaration, this);

            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            List<Variable> parameters = Parameters.ConvertAll((ProcedureParameter parameter) => new Variable(parameter.Type.ToIRType(irBuilder, this), parameter.Identifier, IRProcedureDeclaration, false));
            IRProcedureDeclaration.DelayedLinkSetParameters(parameters);

            if (oldMasterScope is IntermediateRepresentation.Statements.RecordDeclaration parentRecord)
            {
                Variable selfVariable = new(parentRecord.SelfType, "self", IRProcedureDeclaration, true);
                irBuilder.SymbolMarshaller.DeclareSymbol(selfVariable, this);
                IRProcedureDeclaration.CapturedVariables.Add(selfVariable);
            }

            IAstStatement.ForwardDeclareBlock(irBuilder, Statements);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();

            irBuilder.AddProcDeclaration(IRProcedureDeclaration);
        }

#pragma warning disable CS8602 // Only called after ForwardDeclare, when parameter is initialized
        public IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty GenerateProperty() => new(Name, new ProcedureType(IRProcedureDeclaration.ReturnType, IRProcedureDeclaration.Parameters.ConvertAll((param) => param.Type)), true);
#pragma warning restore CS8602

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

#pragma warning disable CS8602 // Parameters set during forward declaration
            foreach (Variable parameter in IRProcedureDeclaration.Parameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);
#pragma warning restore CS8602
            IRProcedureDeclaration.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, Statements));

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();
            return IRProcedureDeclaration;
        }
    }

    partial class ForeignCProcedureDeclaration
    {
        private IntermediateRepresentation.Statements.ForeignCProcedureDeclaration IRForeignDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            IRForeignDeclaration = new IntermediateRepresentation.Statements.ForeignCProcedureDeclaration(Identifier, ParameterTypes.ConvertAll((type) => type.ToIRType(irBuilder, this)), ReturnType.ToIRType(irBuilder, this), this, irBuilder.SymbolMarshaller.CurrentScope);
            irBuilder.SymbolMarshaller.DeclareSymbol(IRForeignDeclaration, this);
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IRForeignDeclaration;
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
                ? (IRValue)new LinkedProcedureCall(procedureDeclaration, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)), irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek(), expectedType, this)
                : procedureSymbol is Variable variable
                ? new AnonymousProcedureCall(new IntermediateRepresentation.Values.VariableReference(variable, this), Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)), this)
                : procedureSymbol is ForeignCProcedureDeclaration foreignFunction
                ? new ForeignFunctionCall(foreignFunction, Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)), this)
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