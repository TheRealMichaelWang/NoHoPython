using NoHoPython.Syntax;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder);
    }

    partial class Primitive
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) { }
    }

    partial class ArrayType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) => ElementType.ScopeForUsedTypeParameters(irBuilder);
    }

    partial class ProcedureType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder)
        {
            ParameterTypes.ForEach((parameter) => parameter.ScopeForUsedTypeParameters(irBuilder));
            ReturnType.ScopeForUsedTypeParameters(irBuilder);
        }
    }

    partial class RecordType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) => TypeArguments.ForEach((typearg) => typearg.ScopeForUsedTypeParameters(irBuilder));
    }

    partial class InterfaceType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) => TypeArguments.ForEach((typearg) => typearg.ScopeForUsedTypeParameters(irBuilder));
    }

    partial class EnumType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) => TypeArguments.ForEach((typearg) => typearg.ScopeForUsedTypeParameters(irBuilder));
    }

    partial class TupleType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder)
        {
            foreach (IType type in ValueTypes.Keys)
                type.ScopeForUsedTypes(irBuilder);
        }
    }

    partial class TypeParameterReference
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) => irBuilder.ScopedProcedures.Peek().SanitizeTypeParameter(TypeParameter);
    }

    partial class EmptyEnumOption
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) { }
    }

    partial class NothingType
    {
        public void ScopeForUsedTypeParameters(AstIRProgramBuilder irBuilder) { }
    }
}