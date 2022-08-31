using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    public interface IRValue
    {
        public IType Type { get; }

        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
    }

    public interface IRStatement
    {

    }

    public sealed class IRProgram
    {
        public readonly List<EnumDeclaration> EnumDeclarations;
        public readonly List<InterfaceDeclaration> InterfaceDeclarations;
        public readonly List<RecordDeclaration> RecordDeclarations;

        public readonly List<ProcedureDeclaration> ProcedureDeclarations;

        public IRProgram(List<EnumDeclaration> enumDeclarations, List<InterfaceDeclaration> interfaceDeclarations, List<RecordDeclaration> recordDeclarations, List<ProcedureDeclaration> procedureDeclarations)
        {
            EnumDeclarations = enumDeclarations;
            InterfaceDeclarations = interfaceDeclarations;
            RecordDeclarations = recordDeclarations;
            ProcedureDeclarations = procedureDeclarations;
        }
    }
}