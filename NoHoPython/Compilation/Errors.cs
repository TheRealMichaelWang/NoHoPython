using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using NoHoPython.Typing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.IntermediateRepresentation
{
    public abstract class CCodegenError : Exception
    {
        public IRElement? IRElement { get; private set; }

        public CCodegenError(IRElement? iRElement, string message) : base(message)
        {
            IRElement = iRElement;
        }

        public void Print()
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

    public sealed class CannotEmitDestructorError : CCodegenError
    {
        public IRValue Value { get; private set; }

        public CannotEmitDestructorError(IRValue value) : base(value, "Cannot emit destructor for value. Please move to a variable, or consider enabling expression-statements.")
        {
            Value = value;
        }
    }

    public sealed class CannotPerformCallStackReporting : CCodegenError
    {
        public ProcedureCall ProcedureCall { get; private set; }

        public CannotPerformCallStackReporting(ProcedureCall procedureCall) : base(procedureCall, "Cannot perform call stack reporting; please enable expression-statements via ommiting the -nogcc flag.")
        {
            ProcedureCall = procedureCall;
        }
    }

    public sealed class CannotCompileNothingError : CCodegenError
    {
        public CannotCompileNothingError(IRElement? errorReportedElement) : base(errorReportedElement, "(Internal Error)Cannot actually compile/emit a nothing literal nor scope a nothing type.")
        {

        }
    }

    public sealed class UnexpectedTypeParameterError : CCodegenError
    {
        public UnexpectedTypeParameterError(Typing.TypeParameter typeParameter, IRElement? errorReportedElement) : base(errorReportedElement, $"(Internal Error)Could not scope or compile/emit the type parameter {typeParameter.Name}.")
        {

        }
    }

    public sealed class CircularDependentTypesError : CCodegenError
    {
        public CircularDependentTypesError(List<IType> dependecyChain, IType circularDependentType) : base(null, $"Type {dependecyChain[0].TypeName} is circularly dependent; {string.Join(" -> ", dependecyChain.ConvertAll((type) => type.TypeName))}, and depends on {circularDependentType.TypeName} again. Please note that the size of {dependecyChain[0].TypeName} has to be known during compilation)")
        {

        }
    }
}
