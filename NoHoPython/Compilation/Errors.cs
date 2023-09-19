using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    public abstract class CodegenError : Exception
    {
        public IRElement? IRElement { get; private set; }

        public CodegenError(IRElement? iRElement, string message) : base(message)
        {
            IRElement = iRElement;
        }

        public virtual void Print()
        {
            Console.WriteLine($"Codegen(to C) Error: {Message}");

            if (IRElement == null)
                return;

            IAstElement AstElement = IRElement.ErrorReportedElement;
            Console.WriteLine($"\nIn {AstElement.SourceLocation}:\n");

            if (AstElement is IAstValue astValue)
            {
                Console.WriteLine($"\t{astValue}");
            }
            else if (AstElement is IAstStatement astStatement)
                Console.WriteLine(astStatement.ToString(0));
        }
    }

    public sealed class CannotEmitDestructorError : CodegenError
    {
        public CannotEmitDestructorError(IRElement? errorReportedElement) : base(errorReportedElement, "Cannot emit destructor for value. Please move to a variable, or consider enabling expression-statements.")
        {

        }
    }

    public sealed class CannotCompileEmptyTypeError : CodegenError
    {
        public CannotCompileEmptyTypeError(IRElement? errorReportedElement) : base(errorReportedElement, "(Internal Error)Cannot actually compile/emit a nothing literal nor scope a nothing type nor any other type with no associated data.")
        {

        }
    }

    public sealed class CannotConfigureResponsibleDestroyerError : CodegenError
    {
        public CannotConfigureResponsibleDestroyerError(ProcedureCall procedureCall, IType rawReturnType) : base(procedureCall, $"Cannot configure responsible destroyer for call {procedureCall} and raw-return-type {rawReturnType}.")
        {

        }
    }

    public sealed class UnexpectedTypeParameterError : CodegenError
    {
        public UnexpectedTypeParameterError(Typing.TypeParameter typeParameter, IRElement? errorReportedElement) : base(errorReportedElement, $"(Internal Error)Could not scope or compile/emit the type parameter {typeParameter.Name}.")
        {

        }
    }

    public sealed class CircularDependentTypesError : CodegenError
    {
        public CircularDependentTypesError(List<IType> dependecyChain, IType circularDependentType) : base(null, $"Type {dependecyChain[0].TypeName} is circularly dependent; {string.Join(" -> ", dependecyChain.ConvertAll((type) => type.TypeName))}, and depends on {circularDependentType.TypeName} again. Please note that the size of {dependecyChain[0].TypeName} has to be known during compilation)")
        {

        }
    }

    public sealed class NoFormatSpecifierForType : CodegenError
    {
        public NoFormatSpecifierForType(IType type) : base(null, $"Cannot interpolate value of type {type.TypeName}; No such valid C format specifier for sprintf.")
        {

        }
    }

    public sealed class CannotEnsureOrderOfEvaluation : CodegenError
    {
        public CannotEnsureOrderOfEvaluation(IRElement? irElement) : base(irElement, "Cannot ensure order of evaluation; try enabling expression statements via omitting the -nogcc flag.")
        {

        }
    }
}
