using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public IRValue? GetResponsibleDestroyer();
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class AllocArray
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class AllocMemorySpan
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class ArrayLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class TupleLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class ReferenceLiteral
    {
        public IRValue? GetResponsibleDestroyer() => GetPostEvalPure();
    }

    partial class MarshalIntoLowerTuple
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class AnonymizeProcedure
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class ProcedureCall
    {
        public IRValue? GetResponsibleDestroyer() => Type.IsReferenceType ? GetPostEvalPure() : null;
    }

    partial class ArithmeticCast
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class HandleCast
    {
        public IRValue? GetResponsibleDestroyer() => Input.GetResponsibleDestroyer();
    }

    partial class AutoCast
    {
        public IRValue? GetResponsibleDestroyer() => Input.GetResponsibleDestroyer();
    }

    partial class ArrayOperator
    {
        public IRValue? GetResponsibleDestroyer() => Operation == ArrayOperation.GetArrayHandle ? ArrayValue.GetResponsibleDestroyer() : null;
    }

    partial class SizeofOperator
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class BinaryOperator
    {
        public virtual IRValue? GetResponsibleDestroyer() => null;
    }

    partial class MemoryGet
    {
        public override IRValue? GetResponsibleDestroyer() => Left.GetResponsibleDestroyer();
    }

    partial class MemorySet
    {
        public IRValue? GetResponsibleDestroyer() => Address.GetResponsibleDestroyer();
    }

    partial class MarshalHandleIntoArray
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class MarshalMemorySpanIntoArray
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class GetValueAtIndex
    {
        public IRValue? GetResponsibleDestroyer() => Array.GetResponsibleDestroyer();
    }

    partial class SetValueAtIndex
    {
        public IRValue? GetResponsibleDestroyer() => Array.GetResponsibleDestroyer();
    }

    partial class GetPropertyValue
    {
        public IRValue? GetResponsibleDestroyer() => Record.Type.IsReferenceType ? Record.GetPostEvalPure() : Record.GetResponsibleDestroyer();
    }

    partial class SetPropertyValue
    {
        public IRValue? GetResponsibleDestroyer() => Record.GetPostEvalPure();
    }

    partial class ReleaseReferenceElement
    {
        public IRValue? GetResponsibleDestroyer() => ReferenceBox.GetPostEvalPure();
    }

    partial class SetReferenceTypeElement
    {
        public IRValue? GetResponsibleDestroyer() => ReferenceBox.GetPostEvalPure();
    }

    partial class MarshalIntoEnum
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class UnwrapEnumValue
    {
        public IRValue? GetResponsibleDestroyer() => EnumValue.GetResponsibleDestroyer();
    }

    partial class CheckEnumOption
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class MarshalIntoInterface
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class IfElseValue
    {
        public IRValue? GetResponsibleDestroyer()
        {
            if (Condition.IsTruey)
                return IfTrueValue.GetResponsibleDestroyer();
            else if (Condition.IsFalsey)
                return IfFalseValue.GetResponsibleDestroyer();
            else
            {
                IRValue? ifTrueResponsibleDestroyer = IfTrueValue.GetResponsibleDestroyer();
                IRValue? ifFalseResponsibleDestroyer = IfFalseValue.GetResponsibleDestroyer();

                if (ifTrueResponsibleDestroyer == null && ifFalseResponsibleDestroyer == null)
                    return null;

                return new IfElseValue(Type, Condition.GetPostEvalPure(), ifTrueResponsibleDestroyer ?? new NullPointerLiteral(Primitive.Handle, ErrorReportedElement), ifFalseResponsibleDestroyer ?? new NullPointerLiteral(Primitive.Handle, ErrorReportedElement), ErrorReportedElement);
            }
        }
    }

    partial class VariableDeclaration
    {
        public IRValue? GetResponsibleDestroyer() => Variable.Type.IsReferenceType ? GetPostEvalPure() : null;
    }

    partial class SetVariable
    {
        public IRValue? GetResponsibleDestroyer() => Variable.Type.IsReferenceType ? GetPostEvalPure() : null;
    }

    partial class VariableReference
    {
        public IRValue? GetResponsibleDestroyer() => Variable.Type.IsReferenceType ? GetPostEvalPure() : null;
    }

    partial class CSymbolReference
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class DecimalLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class IntegerLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class CharacterLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class TrueLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class FalseLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class NullPointerLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class StaticCStringLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }

    partial class EmptyTypeLiteral
    {
        public IRValue? GetResponsibleDestroyer() => null;
    }
}
