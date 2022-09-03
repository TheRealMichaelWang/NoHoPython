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
        public readonly List<IType> ParameterTypes;
        public IType ReturnType { get; private set; }

        private Dictionary<TypeParameter, IType> typeArguments;

        private ProcedureDeclaration procedureDeclaration;

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IType> typeArguments)
        {
            this.procedureDeclaration = procedureDeclaration;
            this.typeArguments = new Dictionary<TypeParameter, IType>();

            TypeParameter.ValidateTypeArguments(this.procedureDeclaration.TypeParameters, typeArguments);
            for (int i = 0; i < typeArguments.Count; i++)
                this.typeArguments.Add(procedureDeclaration.TypeParameters[i], typeArguments[i]);
            
            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(this.typeArguments)).ToList();
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(this.typeArguments);
        }

        public ProcedureReference(ProcedureDeclaration procedureDeclaration, List<IRValue> arguments)
        {
            this.procedureDeclaration = procedureDeclaration;
            typeArguments = new Dictionary<TypeParameter, IType>();

            Debug.Assert(procedureDeclaration.Parameters.Count == arguments.Count);
            for (int i = 0; i < procedureDeclaration.Parameters.Count; i++)
                procedureDeclaration.Parameters[i].Type.MatchTypeArgument(typeArguments, arguments[i].Type);

            ParameterTypes = procedureDeclaration.Parameters.Select((Variable param) => param.Type.SubstituteWithTypearg(typeArguments)).ToList();
            ReturnType = procedureDeclaration.ReturnType.SubstituteWithTypearg(typeArguments);
        }

        public ProcedureReference SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ProcedureReference(procedureDeclaration, this.typeArguments.Select((KeyValuePair<TypeParameter, IType> argument) => argument.Value.SubstituteWithTypearg(typeargs)).ToList());
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
    public abstract class ProcedureCall : IRValue
    {
        public IType Type => ReturnType;

        public readonly List<IRValue> Arguments;
        public IType ReturnType { get; private set; }

        public ProcedureCall(List<IRValue> arguments, List<IType> expectedParameterTypes, IType returnType)
        {
            ReturnType = returnType;

            if (arguments.Count != expectedParameterTypes.Count)
                throw new UnexpectedArgumentsException(arguments.Select((IRValue argument) => argument.Type).ToList(), expectedParameterTypes);

            for (int i = 0; i < expectedParameterTypes.Count; i++)
                arguments[i] = ArithmeticCast.CastTo(arguments[i], expectedParameterTypes[i]);
                
            Arguments = arguments;
        }

        public abstract IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
    }

    public sealed class LinkedProcedureCall : ProcedureCall
    {
        public ProcedureReference Procedure { get; private set; }

        public LinkedProcedureCall(ProcedureReference procedure, List<IRValue> arguments) : base(arguments, procedure.ParameterTypes, procedure.ReturnType)
        {
            Procedure = procedure;
        }

        public LinkedProcedureCall(ProcedureDeclaration procedure, List<IRValue> arguments) : this(new ProcedureReference(procedure, arguments), arguments)
        {

        }

        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LinkedProcedureCall(Procedure.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    public sealed class AnonymousProcedureCall : ProcedureCall
    {
        public IRValue ProcedureValue { get; private set; }

        public AnonymousProcedureCall(IRValue procedureValue, List<IRValue> arguments) : base(arguments, procedureValue.Type is ProcedureType procedureType ? 
                                                                                         procedureType.ParameterTypes :
                                                                                         throw new UnexpectedTypeException(procedureValue.Type), procedureType.ReturnType)
        {
            ProcedureValue = procedureValue;
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