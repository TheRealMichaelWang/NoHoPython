using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class UnexpectedArgumentsException : Exception
    {
        public readonly List<IType> ArgumentTypes;
        public readonly List<IType> ParameterTypes;

        public UnexpectedArgumentsException(List<IType> argumentTypes, List<IType> parameterTypes) : base($"Procedure expected ({string.Join(", ", parameterTypes.Select((IType param) => param.TypeName))}), but got ({string.Join(", ", argumentTypes.Select((IType argument) => argument.TypeName))}) instead.")
        {
            ArgumentTypes = argumentTypes;
            ParameterTypes = parameterTypes;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class ProcedureDeclaration : CodeBlock, IScopeSymbol, IRStatement
    {
        public bool IsGloballyNavigable => true;
        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        public List<Variable>? Parameters { get; private set; }

        public IType ReturnType { get; private set; }

        public ProcedureDeclaration(string name, List<TypeParameter> typeParameters, IType returnType, SymbolContainer? parentContainer) : base(parentContainer, new List<Variable>())
        {
            this.Name = name;
            this.TypeParameters = typeParameters;
            this.Parameters = null;
            this.ReturnType = returnType;
        }

        public void DeclareParameters(List<Variable> parameters)
        {
            Parameters = parameters;
            foreach (Variable parameter in parameters)
                DeclareSymbol(parameter);
        }
    }

    public sealed class ProcedureReference
    {
        public readonly List<IType> ParameterTypes;
        public IType ReturnType { get; private set; }

        private Dictionary<TypeParameter, IType> typeArguments;

        public ProcedureDeclaration ProcedureDeclaration { get; private set; }

        private static Dictionary<TypeParameter, IType> MatchTypeArguments(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments)
        {
            Dictionary<TypeParameter, IType> typeArguments = new();

            Debug.Assert(procedureDeclaration.Parameters.Count == arguments.Count);
            for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                arguments[i] = procedureDeclaration.Parameters[i].Type.MatchTypeArgumentWithValue(typeArguments, arguments[i]);

            return typeArguments;
        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments) : this(MatchTypeArguments(procedureDeclaration, arguments), procedureDeclaration)
        {

        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration) : this(new Dictionary<TypeParameter, IType>(), procedureDeclaration)
        {

        }

        private ProcedureReference(Dictionary<TypeParameter, IType> typeArguments, ProcedureDeclaration procedureDeclaration)
        {
            ProcedureDeclaration = procedureDeclaration;
            this.typeArguments = typeArguments;
            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(typeArguments)).ToList();
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(typeArguments);
        }

        public ProcedureReference SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs)
        {
            Dictionary<TypeParameter, IType> newTypeargs = new Dictionary<TypeParameter, IType>(typeargs.Count);
            foreach (KeyValuePair<TypeParameter, IType> typearg in this.typeArguments)
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
    public sealed class LinkedProcedureCall : IRValue
    {
        public IType Type => Procedure.ReturnType;

        public ProcedureReference Procedure { get; private set; }
        public readonly List<IRValue> Arguments;

        public LinkedProcedureCall(ProcedureDeclaration procedure, List<IRValue> arguments) : this(new ProcedureReference(procedure, arguments), arguments)
        {

        }

        private LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments)
        {
            Procedure = procedure;
            Arguments = arguments;
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    public sealed class AnonymousProcedureCall : IRValue
    {
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
    partial class ProcedureDeclaration
    {
        private IntermediateRepresentation.Statements.ProcedureDeclaration IRProcedureDeclaration;

        public void ForwardTypeDeclare(IRProgramBuilder irBuilder) { }

        public void ForwardDeclare(IRProgramBuilder irBuilder)
        {
            List<Typing.TypeParameter> typeParameters = TypeParameters.ConvertAll((TypeParameter parameter) => parameter.ToIRTypeParameter(irBuilder));

            SymbolContainer? parentContainer = irBuilder.SymbolMarshaller.CurrentContainer;
            if (parentContainer != null && !(parentContainer is IntermediateRepresentation.Statements.ProcedureDeclaration || parentContainer is IntermediateRepresentation.Statements.RecordDeclaration))
                parentContainer = null;

            IRProcedureDeclaration = new(Name, typeParameters, ReturnType.ToIRType(irBuilder), parentContainer);
            List<Variable> parameters = Parameters.ConvertAll((ProcedureParameter parameter) => new Variable(parameter.Type.ToIRType(irBuilder), parameter.Identifier, IRProcedureDeclaration));
            IRProcedureDeclaration.DeclareParameters(parameters);

            irBuilder.SymbolMarshaller.DeclareSymbol(IRProcedureDeclaration);
            irBuilder.SymbolMarshaller.NavigateToScope(IRProcedureDeclaration);

            foreach (Typing.TypeParameter parameter in typeParameters)
                irBuilder.SymbolMarshaller.DeclareSymbol(parameter);

            IAstStatement.ForwardDeclareBlock(irBuilder, Statements);

            irBuilder.SymbolMarshaller.GoBack();
        }
    }
}