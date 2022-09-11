using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class ProcedureDeclaration : CodeBlock, IScopeSymbol, IRStatement
    {
        public bool IsGloballyNavigable => true;
        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        public List<Variable> Parameters { get; private set; }

        public IType ReturnType { get; private set; }

        public ProcedureDeclaration(string name, List<TypeParameter> typeParameters, List<Variable> parameters, IType returnType, SymbolContainer? parentContainer) : base(parentContainer, parameters)
        {
            Name = name;
            TypeParameters = typeParameters;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }

    public sealed class ProcedureReference
    {
        public readonly List<IType> ParameterTypes;
        public IType ReturnType { get; private set; }

        private Dictionary<TypeParameter, IType> typeArguments;

        public ProcedureDeclaration ProcedureDeclaration { get; private set; }

        private static Dictionary<TypeParameter, IType> MatchTypeArguments(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments, IType? returnType, Syntax.IAstElement errorReportedElement)
        {
            Dictionary<TypeParameter, IType> typeArguments = new();

            if (procedureDeclaration.Parameters == null)
                throw new InvalidOperationException();
            if (arguments.Count != procedureDeclaration.Parameters.Count)
                throw new UnexpectedArgumentsException(arguments.ConvertAll((arg) => arg.Type), procedureDeclaration.Parameters, errorReportedElement);

            for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                arguments[i] = procedureDeclaration.Parameters[i].Type.MatchTypeArgumentWithValue(typeArguments, arguments[i]);
            if (returnType != null)
                procedureDeclaration.ReturnType.MatchTypeArgumentWithType(typeArguments, returnType);

            return typeArguments;
        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments, IType? returnType, Syntax.IAstElement errorReportedElement) : this(MatchTypeArguments(procedureDeclaration, arguments, returnType, errorReportedElement), procedureDeclaration)
        {

        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration) : this(new Dictionary<TypeParameter, IType>(), procedureDeclaration)
        {

        }

        private ProcedureReference(Dictionary<TypeParameter, IType> typeArguments, ProcedureDeclaration procedureDeclaration)
        {
            ProcedureDeclaration = procedureDeclaration;
            this.typeArguments = typeArguments;
#pragma warning disable CS8604 // Panic should happen at runtime. Parameters might (but shouldn't unless bug) be null b/c of linking.
            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(typeArguments)).ToList();
#pragma warning restore CS8604 
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(typeArguments);
        }

        public ProcedureReference SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs)
        {
            Dictionary<TypeParameter, IType> newTypeargs = new(typeargs.Count);
            foreach (KeyValuePair<TypeParameter, IType> typearg in typeArguments)
                newTypeargs.Add(typearg.Key, typearg.Value.SubstituteWithTypearg(typeargs));
            return new ProcedureReference(newTypeargs, ProcedureDeclaration);
        }
    }

    public sealed class ReturnStatement : IRStatement
    {
        public IRValue ToReturn { get; private set; }

        public ReturnStatement(IRValue toReturn, ProcedureDeclaration procedureDeclaration)
        {
            ToReturn = ArithmeticCast.CastTo(toReturn, procedureDeclaration.ReturnType);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed class LinkedProcedureCall : IRValue, IRStatement
    {
        public bool IsConstant => false;
        public IType Type => Procedure.ReturnType;

        public ProcedureReference Procedure { get; private set; }
        public readonly List<IRValue> Arguments;

        public LinkedProcedureCall(ProcedureDeclaration procedure, List<IRValue> arguments, IType? returnType, Syntax.IAstElement errorReportedElement) : this(new ProcedureReference(procedure, arguments, returnType, errorReportedElement), arguments)
        {

        }

        private LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments)
        {
            Procedure = procedure;
            Arguments = arguments;
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    public sealed class AnonymousProcedureCall : IRValue, IRStatement
    {
        public bool IsConstant => false;
        public IType Type => ProcedureType.ReturnType;

        public IRValue ProcedureValue { get; private set; }
        public readonly List<IRValue> Arguments;
        public ProcedureType ProcedureType { get; private set; }

        public AnonymousProcedureCall(IRValue procedureValue, List<IRValue> arguments)
        {
            ProcedureValue = procedureValue;
            if (procedureValue.Type is ProcedureType procedureType)
            {
                ProcedureType = procedureType;
                for (int i = 0; i < procedureType.ParameterTypes.Count; i++)
                    arguments[i] = ArithmeticCast.CastTo(arguments[i], procedureType.ParameterTypes[i]);
                Arguments = arguments;
            }
            else
                throw new UnexpectedTypeException(procedureValue.Type);
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AnonymousProcedureCall(ProcedureValue.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    public sealed class AnonymizeProcedure : IRValue
    {
        public bool IsConstant => false;

        public IType Type { get => new ProcedureType(Procedure.ReturnType, Procedure.ParameterTypes); }

        public ProcedureReference Procedure { get; private set; }

        private AnonymizeProcedure(ProcedureReference procedure)
        {
            Procedure = procedure;
        }

        public AnonymizeProcedure(ProcedureDeclaration procedure) : this(new ProcedureReference(procedure))
        {

        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AnonymizeProcedure(Procedure.SubstituteWithTypearg(typeargs));
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
                : (IRStatement)new IntermediateRepresentation.Statements.ReturnStatement(ReturnValue.GenerateIntermediateRepresentationForValue(irBuilder, irBuilder.ScopedProcedures.Peek().ReturnType), irBuilder.ScopedProcedures.Peek());
        }
    }

    partial class ProcedureDeclaration
    {
        private IntermediateRepresentation.Statements.ProcedureDeclaration IRProcedureDeclaration;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder, this));

            SymbolContainer? parentContainer = null;
            if (irBuilder.ScopedProcedures.Count > 0)
                parentContainer = irBuilder.ScopedProcedures.Peek();
            else if (irBuilder.ScopedRecordDeclaration != null)
                parentContainer = irBuilder.ScopedRecordDeclaration;

            List<Variable> parameters = Parameters.ConvertAll((ProcedureParameter parameter) => new Variable(parameter.Type.ToIRType(irBuilder, this), parameter.Identifier));
            if (irBuilder.ScopedRecordDeclaration != null)
                parameters.Insert(0, new Variable(irBuilder.ScopedRecordDeclaration.SelfType, "self"));
            IRProcedureDeclaration = new(Name, typeParameters, parameters, AnnotatedReturnType == null ? Primitive.Nothing : AnnotatedReturnType.ToIRType(irBuilder, this), parentContainer);

            irBuilder.SymbolMarshaller.DeclareSymbol(IRProcedureDeclaration, this);
            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);
            irBuilder.ScopedProcedures.Push(IRProcedureDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter, this);

            IAstStatement.ForwardDeclareBlock(irBuilder, Statements);

            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.ScopedProcedures.Pop();

            irBuilder.AddProcDeclaration(IRProcedureDeclaration);
        }

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
            int i = 0;
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
            return new AnonymousProcedureCall(ProcedureValue.GenerateIntermediateRepresentationForValue(irBuilder, null), Arguments.ConvertAll((IAstValue argument) => argument.GenerateIntermediateRepresentationForValue(irBuilder, null)));
        }
    }
}