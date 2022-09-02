using NoHoPython.IntermediateRepresentation.Statements;

namespace NoHoPython.Typing
{
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class TypeParameterReference : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => TypeParameter.Name; }

        public TypeParameter TypeParameter { get; private set; }

        public TypeParameterReference(TypeParameter typeParameter)
        {
            TypeParameter = typeParameter;
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is TypeParameterReference typeParameterReference)
                return TypeParameter == typeParameterReference.TypeParameter;
            return false;
        }
    }
}
