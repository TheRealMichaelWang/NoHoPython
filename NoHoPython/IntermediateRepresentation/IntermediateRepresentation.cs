using NoHoPython.Scoping;
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
}