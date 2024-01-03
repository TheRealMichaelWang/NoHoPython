using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Values;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private Dictionary<SymbolContainer, int> lambdaCounts = new();

        private void LinkCapturedVariables()
        {
            Dictionary<ProcedureDeclaration, HashSet<ProcedureDeclaration>> dependentProcedures = new(ProcedureDeclarations.Count);
            HashSet<ProcedureDeclaration> unprocessedProcedures = new(ProcedureDeclarations.ConvertAll(e => e.Item1));
            foreach (ProcedureDeclaration procedureDeclaration in unprocessedProcedures)
                dependentProcedures.Add(procedureDeclaration, new());
            foreach (ProcedureDeclaration procedureDeclaration in unprocessedProcedures)
                if(procedureDeclaration.CapturedVariables.Count > 0)
                    foreach (ProcedureDeclaration callSite in procedureDeclaration.CallSiteProcedures)
                        if(!procedureDeclaration.IsChildProcedure(callSite))
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

        public int GetLambdaId()
        {
            if (lambdaCounts.ContainsKey(CurrentMasterScope))
                return ++lambdaCounts[CurrentMasterScope];
            lambdaCounts.Add(CurrentMasterScope, 0);
            return 0;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public enum Purity
    {
        Pure = 0,
        OnlyAffectsArguments = 1,
        OnlyAffectsArgumentsAndCaptured = 2,
        AffectsGlobals = 3
    }

    public sealed partial class ProcedureDeclaration : CodeBlock, IScopeSymbol, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public SymbolContainer ParentContainer { get; private set; }
        public IScopeSymbol? LastMasterScope { get; private set; }

        public string Name { get; private set; }

        public bool IsCompileHead => TypeParameters.Count == 0 && CapturedVariables.Count == 0 && parentContainer.IsHeadContainer;

        public readonly List<Typing.TypeParameter> TypeParameters;
        public List<Variable>? Parameters { get; private set; }
        public List<Variable> CapturedVariables { get; private set; }
        public List<ProcedureDeclaration> CallSiteProcedures { get; private set; }
        public List<Typing.TypeParameter> UsedTypeParameters { get; private set; }

        public IType? ReturnType { get; private set; }
        public Purity Purity { get; private set; }

        public ProcedureDeclaration(string name, List<Typing.TypeParameter> typeParameters, Purity purity, IType? returnType, SymbolContainer parentContainer, IScopeSymbol? lastMasterScope, IAstElement errorReportedElement) : base(parentContainer, false, errorReportedElement.SourceLocation)
        {
            Name = name;
            TypeParameters = typeParameters;
            Purity = purity;
            ReturnType = returnType;
            ParentContainer = parentContainer;
            LastMasterScope = lastMasterScope;
            ErrorReportedElement = errorReportedElement;
            CapturedVariables = new List<Variable>();
            CallSiteProcedures = new List<ProcedureDeclaration>();
            UsedTypeParameters = new List<Typing.TypeParameter>();
        }

        public override string ToString() => Name;

        public bool HasVariable(Variable variable)
        {
            if (variable.ParentProcedure == this)
                return true;
            foreach (Variable capturedVariable in CapturedVariables)
                if (variable.Name == capturedVariable.Name && variable.Type.IsCompatibleWith(capturedVariable.Type))
                    return true;
            return false;
        }

        public bool IsLocalVariable(Variable variable)
        {
            if (variable.ParentProcedure == this)
#pragma warning disable CS8602 // Parameters linked after initialization
                return !Parameters.Contains(variable);
#pragma warning restore CS8602
            return false;
        }

        public bool IsChildProcedure(ProcedureDeclaration potentialChild)
        {
            IScopeSymbol? current = potentialChild;
            while(current is ProcedureDeclaration procedureDeclaration)
            {
                if (procedureDeclaration == this)
                    return true;
                current = procedureDeclaration.LastMasterScope;
            }
            return false;
        }

        public void DelayedLinkSetParameters(List<Variable> parameters)
        {
            if (Parameters != null)
                throw new InvalidOperationException();
            Parameters = parameters;
        }

        public void DelayedLinkSetReturnType(IType returnType)
        {
            if (ReturnType != null)
                throw new InvalidOperationException();
            ReturnType = returnType;
        }

        public override void DelayedLinkSetStatements(List<IRStatement> statements, AstIRProgramBuilder irBuilder)
        {
            if (ReturnType == null || Parameters == null)
                throw new InvalidOperationException();
            base.DelayedLinkSetStatements(statements, irBuilder);
            if (ReturnType is not NothingType && !CodeBlockAllCodePathsReturn())
                throw new NotAllCodePathsReturnError(ErrorReportedElement);

            irBuilder.ScopedProcedures.Push(this);
            foreach (Variable parameter in Parameters)
                parameter.Type.ScopeForUsedTypeParameters(irBuilder);
            foreach (Variable capturedVariable in CapturedVariables)
                capturedVariable.Type.ScopeForUsedTypeParameters(irBuilder);
            ReturnType.ScopeForUsedTypeParameters(irBuilder);
            irBuilder.ScopedProcedures.Pop();
        }

        public (Variable, bool) SanitizeVariable(Variable variable, bool willSet, IAstElement errorReportedElement)
        {
            if (variable.ParentProcedure == this)
            {
#pragma warning disable CS8602 // Parameters linked after initialization
                if (Parameters.Contains(variable) || variable.IsReadOnly)
                {
                    if (willSet)
                        throw new CannotMutateVaraible(variable, false, errorReportedElement);
                    else
                        return (variable, (Purity <= Purity.Pure && variable.Type.HasMutableChildren) || variable.IsReadOnly); //records can still be captured and have their properties mutated
                }
#pragma warning restore CS8602
            }
            else
            {
                if (!CapturedVariables.Contains(variable))
                    CapturedVariables.Add(variable);

                if (willSet)
                    throw new CannotMutateVaraible(variable, true, errorReportedElement);
                else
                {
                    (Variable, bool) capturedVar = variable.ParentProcedure.SanitizeVariable(variable, willSet, errorReportedElement);
                    return (variable, (Purity <= Purity.OnlyAffectsArguments && capturedVar.Item1.Type.HasMutableChildren) || capturedVar.Item2);//records can still be captured and have their properties mutated
                }
            }
            return (variable, false);
        }

        public Typing.TypeParameter SanitizeTypeParameter(Typing.TypeParameter typeParameter)
        {
            if (!UsedTypeParameters.Contains(typeParameter))
                UsedTypeParameters.Add(typeParameter);
            return typeParameter;
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

        private static Dictionary<Typing.TypeParameter, IType> MatchTypeArguments(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments, IType? returnType, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Dictionary<Typing.TypeParameter, IType> typeArguments = new();

            Debug.Assert(procedureDeclaration.Parameters != null);
            if (arguments.Count != procedureDeclaration.Parameters.Count)
                throw new UnexpectedArgumentsException(arguments.ConvertAll((arg) => arg.Type), procedureDeclaration.Parameters, errorReportedElement);

            if (procedureDeclaration.TypeParameters.Count > 0)
            {
                for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                    arguments[i] = procedureDeclaration.Parameters[i].Type.MatchTypeArgumentWithValue(typeArguments, arguments[i], irBuilder);

                if (returnType != null)
#pragma warning disable CS8602 // Return types may be linked in after initialization
                    procedureDeclaration.ReturnType.MatchTypeArgumentWithType(typeArguments, returnType, errorReportedElement);
#pragma warning restore CS8602
            }
            else
            {
                for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                    arguments[i] = ArithmeticCast.CastTo(arguments[i], procedureDeclaration.Parameters[i].Type, irBuilder);
            }

            return typeArguments;
        }

        public static Dictionary<Typing.TypeParameter, IType> SubstituteTypeargsWithTypeargs(Dictionary<Typing.TypeParameter, IType> toSubstitute, Dictionary<Typing.TypeParameter, IType> typeargs)
        {
            Dictionary<Typing.TypeParameter, IType> newTypeargs = new(toSubstitute.Count + typeargs.Count);
            foreach (KeyValuePair<Typing.TypeParameter, IType> typearg in toSubstitute)
                newTypeargs.Add(typearg.Key, typearg.Value.SubstituteWithTypearg(typeargs));
            foreach (KeyValuePair<Typing.TypeParameter, IType> typearg in typeargs)
                if (!toSubstitute.ContainsKey(typearg.Key))
                    newTypeargs.Add(typearg.Key, typearg.Value);
            return newTypeargs;
        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments, IType? returnType, bool isAnonymous, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : this(MatchTypeArguments(procedureDeclaration, arguments, returnType, irBuilder, errorReportedElement), procedureDeclaration, isAnonymous, errorReportedElement)
        {

        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, bool isAnonymous, IAstElement errorReportedElement) : this(new Dictionary<Typing.TypeParameter, IType>(), procedureDeclaration, isAnonymous, errorReportedElement)
        {
            this.errorReportedElement = errorReportedElement;
        }

        public ProcedureReference(Dictionary<Typing.TypeParameter, IType> typeArguments, ProcedureDeclaration procedureDeclaration, bool isAnonymous, IAstElement errorReportedElement)
        {
            ProcedureDeclaration = procedureDeclaration;
            IsAnonymous = isAnonymous;
            this.typeArguments = typeArguments;
            this.errorReportedElement = errorReportedElement;

            if (this.typeArguments.Count < ProcedureDeclaration.TypeParameters.Count)
                throw new UnexpectedTypeArgumentsException(ProcedureDeclaration.TypeParameters.Count, this.typeArguments.Count, errorReportedElement);

#pragma warning disable CS8604 // Panic should happen at runtime. Parameters might (but shouldn't unless bug) be null b/c of linking.
            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(typeArguments)).ToList();
#pragma warning restore CS8604

#pragma warning disable CS8602 // Return types may be linked in after initialization
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(typeArguments);
#pragma warning restore CS8602 
            anonProcedureType = new ProcedureType(ReturnType, ParameterTypes, Purity.Pure);
        }

        public ProcedureReference SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new ProcedureReference(SubstituteTypeargsWithTypeargs(typeArguments, typeargs), ProcedureDeclaration, IsAnonymous, errorReportedElement);

        public ProcedureReference CreateRegularReference() => new ProcedureReference(typeArguments, ProcedureDeclaration, false, errorReportedElement);

        public bool IsCompatibleWith(ProcedureReference procedureReference)
        {
            if (ProcedureDeclaration != procedureReference.ProcedureDeclaration)
                return false;
            if (IsAnonymous != procedureReference.IsAnonymous)
                return false;

            return ProcedureDeclaration.UsedTypeParameters.TrueForAll((typeParameter) => typeArguments[typeParameter].IsCompatibleWith(procedureReference.typeArguments[typeParameter]));
        }
    }

    public sealed partial class ReturnStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IRValue ToReturn { get; private set; }

        private ProcedureDeclaration parentProcedure;

        public ReturnStatement(IRValue toReturn, AstIRProgramBuilder irBuilder, IAstElement errorReportedStatement)
        {
#pragma warning disable CS8604 // Return types may be linked in after initialization
            ToReturn = ArithmeticCast.CastTo(toReturn, irBuilder.ScopedProcedures.Peek().ReturnType, irBuilder);
#pragma warning restore CS8604 
            parentProcedure = irBuilder.ScopedProcedures.Peek();
            ErrorReportedElement = errorReportedStatement;
        }
    }

    public sealed partial class AbortStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IRValue? AbortMessage { get; private set; }

        public AbortStatement(IRValue? abortMessage, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            AbortMessage = abortMessage;
        }
    }

    public sealed partial class ForeignCProcedureDeclaration : SymbolContainer, IRStatement, IScopeSymbol
    {
        public override bool IsGloballyNavigable => false;

        public SymbolContainer ParentContainer { get; private set; }
        public IAstElement ErrorReportedElement { get; private set; }

        public string Name { get; private set; }
        public string? CFunctionName { get; private set; }

        public List<Typing.TypeParameter>? TypeParameters { get; private set; }
        public List<IType>? ParameterTypes { get; private set; }
        public IType? ReturnType { get; private set; }

        public Purity Purity { get; private set; }

        public ForeignCProcedureDeclaration(string name, string? cFunctionName, Purity purity, IAstElement errorReportedElement, SymbolContainer parentContainer)
        {
            Name = name;
            CFunctionName = cFunctionName;
            Purity = purity;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }

        public void DelayedLinkSetTypeParameters(List<Typing.TypeParameter> typeParameters)
        {
            Debug.Assert(TypeParameters == null);
            TypeParameters = typeParameters;
        }

        public void DelayedLinkSetParameters(List<IType> parameterTypes, IType returnType)
        {
            void CheckType(IType type)
            {
                if (!type.IsNativeCType)
                    throw new UnexpectedTypeException(type, ErrorReportedElement);
            }

            Debug.Assert(ParameterTypes == null);
            Debug.Assert(ReturnType == null);
            ParameterTypes = parameterTypes;
            ReturnType = returnType;

            ParameterTypes.ForEach((parameter) => CheckType(parameter));
            CheckType(ReturnType);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public abstract partial class ProcedureCall : IRValue, IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public abstract IType Type { get; }
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public Purity FunctionPurity { get; private set; }
        public readonly List<IRValue> Arguments;

        public ProcedureCall(List<IType> expectedParameters, List<IRValue> arguments, AstIRProgramBuilder irBuilder, Purity functionPurity, IAstElement errorReportedElement) : this(arguments, functionPurity, errorReportedElement)
        {
            if (expectedParameters.Count != arguments.Count)
                throw new UnexpectedArgumentsException(arguments.Select((arg) => arg.Type).ToList(), expectedParameters, errorReportedElement);
            for (int i = 0; i < expectedParameters.Count; i++)
            {
                if (functionPurity <= Purity.Pure && !Arguments[i].IsReadOnly)
                {
                    if (Arguments[i].Type is ReferenceType referenceType && referenceType.Mode == ReferenceType.ReferenceMode.UnreleasedCanRelease)
                        Arguments[i].RefineAssumeType(irBuilder, (new ReferenceType(referenceType.ElementType, ReferenceType.ReferenceMode.Released), null));
                    else
                        Arguments[i].GetRefinementEntry(irBuilder)?.ClearSubRefinements();
                }

                Arguments[i] = ArithmeticCast.CastTo(Arguments[i], expectedParameters[i], irBuilder);
            }
        }

        protected ProcedureCall(List<IRValue> arguments, Purity functionPurity, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Arguments = arguments;
            FunctionPurity = functionPurity;
        }

        public abstract IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs);
    }

    public sealed partial class LinkedProcedureCall : ProcedureCall
    {
        public override IType Type => Procedure.ReturnType;

        public ProcedureReference Procedure { get; private set; }
        private ProcedureDeclaration? parentProcedure;

        public LinkedProcedureCall(ProcedureDeclaration procedure, List<IRValue> arguments, ProcedureDeclaration? parentProcedure, IType? returnType, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : this(new ProcedureReference(procedure, arguments, returnType, false, irBuilder, errorReportedElement), arguments, parentProcedure, irBuilder, errorReportedElement)
        {

        }

        public LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments, ProcedureDeclaration? parentProcedure, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(procedure.ParameterTypes, arguments, irBuilder, procedure.ProcedureDeclaration.Purity, errorReportedElement)
        {
            Procedure = procedure;
            this.parentProcedure = parentProcedure;

            if (parentProcedure != null)
                procedure.ProcedureDeclaration.CallSiteProcedures.Add(parentProcedure);
        }

        private LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments, ProcedureDeclaration? parentProcedure, IAstElement errorReportedElement) : base(arguments, procedure.ProcedureDeclaration.Purity, errorReportedElement)
        {
            Procedure = procedure;
            this.parentProcedure = parentProcedure;
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), parentProcedure, ErrorReportedElement);
    }

    public sealed partial class AnonymousProcedureCall : ProcedureCall
    {
        public static IRValue ComposeCall(IRValue procedureValue, List<IRValue> arguments, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            if (procedureValue is GetPropertyValue getProperty && getProperty.Property is RecordDeclaration.RecordProperty property && property.OptimizeMessageReciever)
                return new OptimizedRecordMessageCall(property, getProperty.Record, arguments, irBuilder, errorReportedElement);
            else if (procedureValue is AnonymizeProcedure anonymizeProcedure)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (anonymizeProcedure.Procedure.ProcedureDeclaration.Parameters.Count == 1 && anonymizeProcedure.Procedure.ProcedureDeclaration.Statements != null && anonymizeProcedure.Procedure.ProcedureDeclaration.Statements.Count == 1)
                {
                    IRStatement onlyStatement = anonymizeProcedure.Procedure.ProcedureDeclaration.Statements[0];
                    bool doubleCheck = false;
                check_if_lamda:
                    if (onlyStatement is LinkedProcedureCall procedureCall && procedureCall.Arguments.Count == 1 && procedureCall.Arguments[0] is VariableReference variableReference && variableReference.Variable == anonymizeProcedure.Procedure.ProcedureDeclaration.Parameters[0])
                    {
                        return new AnonymizeProcedure(procedureCall.Procedure.ProcedureDeclaration, false, errorReportedElement, irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek());
                    }
                    if(!doubleCheck && onlyStatement is ReturnStatement returnStatement && returnStatement.ToReturn is LinkedProcedureCall procedureCall1)
                    {
                        onlyStatement = procedureCall1;
                        doubleCheck = true;
                        goto check_if_lamda;
                    }
                }
                return new LinkedProcedureCall(anonymizeProcedure.Procedure, arguments, irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek(), irBuilder, errorReportedElement);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            return new AnonymousProcedureCall(procedureValue, arguments, irBuilder, errorReportedElement);
        }

        public static IRValue SendMessage(IRValue value, string receiverName, IType? expectedReturnType, List<IRValue> arguments, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement, bool finalCast = true){
            if(value.Type is IPropertyContainer propertyContainer && propertyContainer.HasProperty(receiverName))
                return ComposeCall(GetPropertyValue.ComposeGetProperty(value, receiverName, irBuilder, errorReportedElement), arguments, irBuilder, errorReportedElement);
            
            IScopeSymbol? procedure = irBuilder.SymbolMarshaller.FindSymbol($"typeExt:{value.Type.Identifier}_{receiverName}");
            if(procedure == null)
                procedure = irBuilder.SymbolMarshaller.FindSymbol($"typeExt:{value.Type.PrototypeIdentifier}_{receiverName}");
            if(procedure == null && value.Type is EnumType enumType)
            {
                foreach(IType option in enumType.GetOptions())
                    if(GetPropertyValue.HasMessageReceiver(option, receiverName, value.ErrorReportedElement, irBuilder))
                        return SendMessage(new UnwrapEnumValue(value, option, irBuilder, value.ErrorReportedElement), receiverName, expectedReturnType, arguments, irBuilder, errorReportedElement);
            }
            if (procedure == null)
            {
                if (value.Type.IsCompatibleWith(Primitive.GetStringType(irBuilder, value.ErrorReportedElement)))
                    procedure = irBuilder.SymbolMarshaller.FindSymbol($"typeExt:string_{receiverName}", errorReportedElement);
                else
                    throw new SymbolNotFoundException($"typeExt:{value.Type.Identifier}_{receiverName}", irBuilder.SymbolMarshaller.CurrentScope, value.ErrorReportedElement);
            }

            List<IRValue> newArguments = new List<IRValue>(arguments);
            newArguments.Insert(0, value);
            IRValue? toreturn = null;
            if (procedure is ProcedureDeclaration procedureDeclaration)
                toreturn = new LinkedProcedureCall(procedureDeclaration, newArguments, irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek(), expectedReturnType, irBuilder, errorReportedElement);
            else if (procedure is ForeignCProcedureDeclaration foreignCProcedureDeclaration)
                toreturn = new ForeignFunctionCall(foreignCProcedureDeclaration, newArguments, expectedReturnType, irBuilder, errorReportedElement);

            if(toreturn != null)
            {
                if (expectedReturnType == null || !finalCast)
                    return toreturn;
                else
                    return ArithmeticCast.CastTo(toreturn, expectedReturnType, irBuilder);
            }
            throw new NotAProcedureException(procedure, errorReportedElement);
        }

        public override IType Type => ProcedureType.ReturnType;

        public IRValue ProcedureValue { get; private set; }
        public ProcedureType ProcedureType { get; private set; }

        private AnonymousProcedureCall(IRValue procedureValue, List<IRValue> arguments, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base((procedureValue.Type is ProcedureType procedureType ? procedureType : throw new UnexpectedTypeException(procedureValue.Type, errorReportedElement)).ParameterTypes, arguments, irBuilder, procedureType.Purity, errorReportedElement)
        {
            ProcedureValue = procedureValue;
            ProcedureType = procedureType;
            arguments.Insert(0, ProcedureValue);
        }

        private AnonymousProcedureCall(IRValue procedureValue, ProcedureType procedureType , List<IRValue> arguments, IAstElement errorReportedElement) : base(arguments, procedureType.Purity, errorReportedElement)
        {
            ProcedureValue = procedureValue;
            ProcedureType = procedureType;
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new AnonymousProcedureCall(ProcedureValue.SubstituteWithTypearg(typeargs), (ProcedureType)ProcedureType.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    public sealed partial class OptimizedRecordMessageCall : ProcedureCall
    {
        public override IType Type => ((ProcedureType)Property.Type).ReturnType;

        public RecordDeclaration.RecordProperty Property { get; private set; }
        public IRValue Record { get; private set; }

        public OptimizedRecordMessageCall(RecordDeclaration.RecordProperty property, IRValue record, List<IRValue> arguments, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(property.Type is ProcedureType procedureType ? procedureType.ParameterTypes : throw new UnexpectedTypeException(property.Type, errorReportedElement), arguments, irBuilder, ((ProcedureType)property.Type).Purity, errorReportedElement)
        {
            Debug.Assert(property.OptimizeMessageReciever);

            Property = property;
            Record = record;
        }

        private OptimizedRecordMessageCall(RecordDeclaration.RecordProperty property, IRValue record, List<IRValue> arguments, IAstElement errorReportedElement) : base(arguments, ((ProcedureType)property.Type).Purity, errorReportedElement)
        {
            Property = property;
            Record = record;
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new OptimizedRecordMessageCall(Property.SubstituteWithTypeargs(typeargs), Record.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    public sealed partial class ForeignFunctionCall : ProcedureCall
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public override IType Type => ForeignCProcedure.ReturnType.SubstituteWithTypearg(typeArguments);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public ForeignCProcedureDeclaration ForeignCProcedure { get; private set; }
        private Dictionary<Typing.TypeParameter, IType> typeArguments;

        private static Dictionary<Typing.TypeParameter, IType> MatchTypeArguments(ForeignCProcedureDeclaration foreignCProcedure, List<IRValue> arguments, IType? returnType, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Dictionary<Typing.TypeParameter, IType> typeArguments = new();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (foreignCProcedure.TypeParameters.Count > 0)
            {
                for (int i = 0; i < foreignCProcedure.ParameterTypes.Count; i++)
                    arguments[i] = foreignCProcedure.ParameterTypes[i].MatchTypeArgumentWithValue(typeArguments, arguments[i], irBuilder);

                if (returnType != null)
                    foreignCProcedure.ReturnType.MatchTypeArgumentWithType(typeArguments, returnType, errorReportedElement);
            }
            else
            {
                for (int i = 0; i < foreignCProcedure.ParameterTypes.Count; i++)
                    arguments[i] = ArithmeticCast.CastTo(arguments[i], foreignCProcedure.ParameterTypes[i], irBuilder);
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return typeArguments;
        }

#pragma warning disable CS8604 // Possible null reference argument.
        public ForeignFunctionCall(ForeignCProcedureDeclaration foreignCProcedure, List<IRValue> arguments, IType? expectedReturnType, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement) : base(foreignCProcedure.ParameterTypes.Select((parameter) => parameter.SubstituteWithTypearg(MatchTypeArguments(foreignCProcedure, arguments, expectedReturnType, irBuilder, errorReportedElement))).ToList(), arguments, irBuilder, foreignCProcedure.Purity, errorReportedElement)
#pragma warning restore CS8604 // Possible null reference argument.
        {
            ForeignCProcedure = foreignCProcedure;
            typeArguments = MatchTypeArguments(foreignCProcedure, arguments, expectedReturnType, irBuilder, errorReportedElement);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (typeArguments.Count != ForeignCProcedure.TypeParameters.Count)
                throw new UnexpectedTypeArgumentsException(ForeignCProcedure.TypeParameters.Count, typeArguments.Count, errorReportedElement);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }

        private ForeignFunctionCall(ForeignCProcedureDeclaration foreignCProcedure, List<IRValue> argments, Dictionary<Typing.TypeParameter, IType> typeArguments, IAstElement errorReportedElement) : base(argments, foreignCProcedure.Purity, errorReportedElement)
        {
            ForeignCProcedure = foreignCProcedure;
            this.typeArguments = typeArguments;
        }

        public override IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new ForeignFunctionCall(ForeignCProcedure, Arguments.ConvertAll((argument) => argument.SubstituteWithTypearg(typeargs)), ProcedureReference.SubstituteTypeargsWithTypeargs(typeArguments, typeargs), ErrorReportedElement);
    }

    public sealed partial class AnonymizeProcedure : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type => GetFunctionHandle ? Primitive.Handle : new ProcedureType(Procedure.ReturnType, Procedure.ParameterTypes, Procedure.ProcedureDeclaration.Purity == Purity.OnlyAffectsArgumentsAndCaptured && !Procedure.ProcedureDeclaration.CapturedVariables.Any((variable) => variable.Type.IsReferenceType && variable.Type.HasMutableChildren) ? Purity.OnlyAffectsArguments : Procedure.ProcedureDeclaration.Purity);
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public ProcedureReference Procedure { get; private set; }
        public bool GetFunctionHandle { get; private set; }
        private ProcedureDeclaration? parentProcedure;

        private AnonymizeProcedure(ProcedureReference procedure, bool getFunctionHandle, ProcedureDeclaration? parentProcedure, IAstElement errorReportedElement)
        {
            Procedure = procedure;
            ErrorReportedElement = errorReportedElement;
            GetFunctionHandle = getFunctionHandle;
            this.parentProcedure = parentProcedure;

            if(parentProcedure != null)
                procedure.ProcedureDeclaration.CallSiteProcedures.Add(parentProcedure);

            if (getFunctionHandle && procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
                throw new CannotGetFunctionPointer(procedure.ProcedureDeclaration, errorReportedElement);
        }

        public AnonymizeProcedure(ProcedureDeclaration procedure, bool getFunctionHandle, IAstElement errorReportedElement, ProcedureDeclaration? parentDeclaration) : this(new ProcedureReference(procedure, !getFunctionHandle, errorReportedElement), getFunctionHandle, parentDeclaration, errorReportedElement)
        {
            
        }

        public IRValue SubstituteWithTypearg(Dictionary<Typing.TypeParameter, IType> typeargs) => new AnonymizeProcedure(Procedure.SubstituteWithTypearg(typeargs), GetFunctionHandle, parentProcedure, ErrorReportedElement);
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
                : (IRStatement)new IntermediateRepresentation.Statements.ReturnStatement(ReturnValue.GenerateIntermediateRepresentationForValue(irBuilder, irBuilder.ScopedProcedures.Peek().ReturnType, false), irBuilder, this);
        }
    }

    partial class AbortStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IType stringType = Primitive.GetStringType(irBuilder, this);

            IRValue? abortMessage = null;
            if(AbortMessage != null)
            {
                if (AbortMessage is StringLiteral)
                    return new IntermediateRepresentation.Statements.AbortStatement(AbortMessage.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.CString, false), this);

                abortMessage = ArithmeticCast.CastTo(AbortMessage.GenerateIntermediateRepresentationForValue(irBuilder, stringType, false), stringType, irBuilder);
                try
                {
                    abortMessage = ArithmeticCast.CastTo(abortMessage, new ArrayType(Primitive.Character), irBuilder);
                }
                catch
                {
                    abortMessage = ArithmeticCast.CastTo(abortMessage, Primitive.CString, irBuilder);
                }
            }
            
            return new IntermediateRepresentation.Statements.AbortStatement(abortMessage, this);
        }
    }

    partial class ProcedureDeclaration
    {
        partial class ProcedureParameter
        {
            public Variable ToIRVariable(AstIRProgramBuilder irBuilder, IntermediateRepresentation.Statements.ProcedureDeclaration IRProcedureDeclaration, IAstElement errorReportedElement) => new Variable(Type.ToIRType(irBuilder, errorReportedElement), Identifier, IsReadOnly, IRProcedureDeclaration, false, errorReportedElement);
        }

        private IntermediateRepresentation.Statements.ProcedureDeclaration IRProcedureDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            SymbolContainer? oldScope = irBuilder.SymbolMarshaller.CurrentScope;
            IRProcedureDeclaration = new(Name, typeParameters, Purity, null, oldScope, (IScopeSymbol)irBuilder.CurrentMasterScope, this);

            irBuilder.SymbolMarshaller.DeclareSymbol(IRProcedureDeclaration, this);

            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);
            IRProcedureDeclaration.DelayedLinkSetReturnType(AnnotatedReturnType == null ? Primitive.Nothing : AnnotatedReturnType.ToIRType(irBuilder, this));

            List<Variable> parameters = Parameters.ConvertAll((ProcedureParameter parameter) => parameter.ToIRVariable(irBuilder, IRProcedureDeclaration, this));
            IRProcedureDeclaration.DelayedLinkSetParameters(parameters);

            if (oldScope is IntermediateRepresentation.Statements.RecordDeclaration parentRecord)
            {
                Variable selfVariable = new(parentRecord.GetSelfType(irBuilder), "self", false, IRProcedureDeclaration, true, this);
                irBuilder.SymbolMarshaller.DeclareSymbol(selfVariable, this);
                IRProcedureDeclaration.CapturedVariables.Add(selfVariable);
            }

            IAstStatement.ForwardDeclareBlock(irBuilder, Statements);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();

            irBuilder.AddProcDeclaration(IRProcedureDeclaration, DeclarationType);
        }

#pragma warning disable CS8602 // Only called after ForwardDeclare, when parameter is initialized
#pragma warning disable CS8604 // Return type linked after initialization
        public IntermediateRepresentation.Statements.RecordDeclaration.RecordProperty GenerateProperty(AstIRProgramBuilder irBuilder, RecordType selfType) => new(Name, new ProcedureType(IRProcedureDeclaration.ReturnType, IRProcedureDeclaration.Parameters.ConvertAll((param) => param.Type), Purity), true, selfType, irBuilder.ScopedRecordDeclaration);
#pragma warning restore CS8604
#pragma warning restore CS8602

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            irBuilder.PrintVerbose($"Generating IR for procedure {IScopeSymbol.GetAbsolouteName(IRProcedureDeclaration)}...");

            irBuilder.NewRefinmentContext(true);
            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

#pragma warning disable CS8602 // Parameters set during forward declaration
            foreach (Variable parameter in IRProcedureDeclaration.Parameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);
#pragma warning restore CS8602
            IRProcedureDeclaration.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, Statements), irBuilder);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();
            irBuilder.Refinements.Pop();

            return IRProcedureDeclaration;
        }
    }

    partial class ForeignCProcedureDeclaration
    {
        private IntermediateRepresentation.Statements.ForeignCProcedureDeclaration IRForeignDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            IRForeignDeclaration = new IntermediateRepresentation.Statements.ForeignCProcedureDeclaration(Identifier, CFunctionName, Purity, this, irBuilder.SymbolMarshaller.CurrentScope);

            irBuilder.SymbolMarshaller.NavigateToScope(IRForeignDeclaration);

            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));
            foreach (Typing.TypeParameter typeParameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(typeParameter, this);

            IRForeignDeclaration.DelayedLinkSetTypeParameters(typeParameters);
            IRForeignDeclaration.DelayedLinkSetParameters(ParameterTypes.ConvertAll((type) => type.ToIRType(irBuilder, this)), ReturnType.ToIRType(irBuilder, this));

            irBuilder.SymbolMarshaller.GoBack();

            irBuilder.SymbolMarshaller.DeclareSymbol(IRForeignDeclaration, this);
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => IRForeignDeclaration;
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class NamedFunctionCall
    {
        public static List<IRValue> GenerateArguments(AstIRProgramBuilder irBuilder, List<IAstValue> arguments, List<IType> parameterTypes, bool willRevaluate)
        {
            if (arguments.Count != parameterTypes.Count)
                return arguments.ConvertAll((arg) => arg.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate));
            List<IRValue> newArguments = new(arguments.Count);
            for (int i = 0; i < arguments.Count; i++)
                newArguments.Add(arguments[i].GenerateIntermediateRepresentationForValue(irBuilder, parameterTypes[i], willRevaluate));
            return newArguments;
        }

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IScopeSymbol procedureSymbol = irBuilder.SymbolMarshaller.FindSymbol(Name, this);
#pragma warning disable CS8602 // Many items aren't linked immediatley
#pragma warning disable CS8604 // Possible null reference argument.
            return procedureSymbol is ProcedureDeclaration procedureDeclaration
                ? (IRValue)new LinkedProcedureCall(procedureDeclaration, GenerateArguments(irBuilder, Arguments, procedureDeclaration.Parameters.ConvertAll((parameter) => parameter.Type), false), irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek(), expectedType, irBuilder, this)
                : procedureSymbol is Variable variable
                ? AnonymousProcedureCall.ComposeCall(new IntermediateRepresentation.Values.VariableReference(irBuilder.ScopedProcedures.Peek().SanitizeVariable(variable, false, this), irBuilder.Refinements.Peek().GetRefinementEntry(variable)?.Refinement, this), Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate)), irBuilder, this)
                : procedureSymbol is ForeignCProcedureDeclaration foreignFunction
                ? new ForeignFunctionCall(foreignFunction, GenerateArguments(irBuilder, Arguments, foreignFunction.ParameterTypes, willRevaluate), expectedType, irBuilder, this)
                : throw new NotAProcedureException(procedureSymbol, this);
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602
        }
    }

    partial class AnonymousFunctionCall
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }
        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => (IRStatement)
            GenerateIntermediateRepresentationForValue(irBuilder, null, false);

        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            List<IRValue> arguments = Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate));
            if (ProcedureValue is GetPropertyValue propertyValue)
                return AnonymousProcedureCall.SendMessage(propertyValue.Record.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), propertyValue.Property, expectedType, arguments, irBuilder, this);
            return AnonymousProcedureCall.ComposeCall(ProcedureValue.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), arguments, irBuilder, this);
        }
    }

    partial class LambdaDeclaration
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            ProcedureDeclaration lamdaDeclaration = new($"lambdaNo{irBuilder.GetLambdaId()}", new(), Purity, null, irBuilder.SymbolMarshaller.CurrentScope, (IScopeSymbol)irBuilder.CurrentMasterScope, this);
            List<Variable> parameters = Parameters.ConvertAll((parameter) => parameter.ToIRVariable(irBuilder, lamdaDeclaration, this));
            lamdaDeclaration.DelayedLinkSetParameters(parameters);
            
            irBuilder.SymbolMarshaller.NavigateToScope(lamdaDeclaration);
            irBuilder.ScopedProcedures.Push(lamdaDeclaration);
            foreach (Variable parameter in parameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            IType? expectedReturnType = (expectedType is ProcedureType expectedProcedureType) ? expectedProcedureType.ReturnType : null;
            IRValue returnExpression = ReturnExpression.GenerateIntermediateRepresentationForValue(irBuilder, null, false);

            List<IRStatement> statements = new(1);
            if (ReturnExpression is IAstStatement statement && (returnExpression.Type is NothingType || expectedReturnType is NothingType))
            {
                lamdaDeclaration.DelayedLinkSetReturnType(Primitive.Nothing);
                statements.Add(statement.GenerateIntermediateRepresentationForStatement(irBuilder));
            }
            else
            {
                lamdaDeclaration.DelayedLinkSetReturnType(returnExpression.Type);
                statements.Add(new ReturnStatement(returnExpression, irBuilder, this));
            }
            lamdaDeclaration.DelayedLinkSetStatements(statements, irBuilder);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();
            irBuilder.AddProcDeclaration(lamdaDeclaration, Statements.ProcedureDeclaration.Type.Normal);
            return new AnonymizeProcedure(lamdaDeclaration, expectedType == null ? false : expectedType is HandleType, this, irBuilder.ScopedProcedures.Count == 0 ? null : irBuilder.ScopedProcedures.Peek());
        }
    }
}