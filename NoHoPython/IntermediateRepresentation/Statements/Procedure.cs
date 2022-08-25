using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class ProcedureDeclaration : CodeBlock, IScopeSymbol, IRStatement
    {
        public bool IsGloballyNavigable => true;
        public string Name { get; private set; }

        public readonly List<TypeParameter> TypeParameters;
        public readonly List<Variable> Parameters;

        public IType ReturnType { get; private set; }

        public ProcedureDeclaration(string name, List<TypeParameter> typeParameters, List<Variable> parameters, IType returnType, List<IRStatement> statements, SymbolContainer? parentContainer) : base(statements, parentContainer, parameters)
        {
            this.Name = name;
            this.TypeParameters = typeParameters;
            this.Parameters = parameters;
            this.ReturnType = returnType;

            foreach (TypeParameter typeParameter in typeParameters)
                base.DeclareSymbol(typeParameter);
        }
    }

    public sealed class ProcedureReference
    {
        public readonly List<IType> TypeArguments;
        public readonly List<IType> ParameterTypes;
        public IType ReturnType { get; private set; }

        private ProcedureDeclaration procedureDeclaration;

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IType> typeArguments)
        {
            this.procedureDeclaration = procedureDeclaration;
            TypeArguments = typeArguments;

            TypeParameter.ValidateTypeArguments(this.procedureDeclaration.TypeParameters, typeArguments);

            Dictionary<TypeParameter, IType> typeargs = new Dictionary<TypeParameter, IType>(TypeArguments.Count);
            for (int i = 0; i < TypeArguments.Count; i++)
                typeargs.Add(procedureDeclaration.TypeParameters[i], TypeArguments[i]);
            
            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(typeargs)).ToList();
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(typeargs);
        }

        public ProcedureReference SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ProcedureReference(procedureDeclaration, TypeArguments.Select((IType argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public abstract class ProcedureCall : IRValue
    {
        public abstract IType Type { get; }

        public readonly List<IRValue> Arguments;

        public ProcedureCall(List<IRValue> arguments)
        {
            Arguments = arguments;
        }

        public abstract IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
    }

    public sealed class LinkedProcedureCall : ProcedureCall
    {
        public override IType Type => Procedure.ReturnType;
        public ProcedureReference Procedure { get; private set; }

        public LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments) : base(arguments)
        {
            Procedure = procedure;
            for (int i = 0; i < arguments.Count; i++)
                if (!procedure.ParameterTypes[i].IsCompatibleWith(arguments[i].Type))
                    throw new UnexpectedTypeException(procedure.ParameterTypes[i], arguments[i].Type);
        }

        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    public sealed class AnonymousProcedureCall : ProcedureCall
    {
        public override IType Type => procedureType.ReturnType;

        public IRValue ProcedureValue { get; private set; }
        private ProcedureType procedureType;

        public AnonymousProcedureCall(IRValue procedureValue, List<IRValue> arguments) : base(arguments)
        {
            ProcedureValue = procedureValue;
            procedureType = (ProcedureType)procedureValue.Type;

            if (arguments.Count != procedureType.ParameterTypes.Count)
                throw new UnexpectedTypeArgumentsException(arguments.Count, procedureType.ParameterTypes.Count);
            for (int i = 0; i < arguments.Count; i++)
                if (!procedureType.ParameterTypes[i].IsCompatibleWith(arguments[i].Type))
                    throw new UnexpectedTypeException(procedureType.ParameterTypes[i], arguments[i].Type);
        }

        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AnonymousProcedureCall(ProcedureValue.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    public sealed class AnonymizeProcedure : IRValue
    {
        public IType Type { get => new ProcedureType(Procedure.ReturnType, Procedure.ParameterTypes); }

        public ProcedureReference Procedure { get; private set; }

        public AnonymizeProcedure(ProcedureReference procedure)
        {
            Procedure = procedure;
        }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AnonymizeProcedure(Procedure.SubstituteWithTypearg(typeargs));
    }
}