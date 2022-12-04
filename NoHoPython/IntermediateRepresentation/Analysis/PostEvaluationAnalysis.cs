namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class AllocArray
    {
        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class AnonymizeProcedure
    {
        public IRValue GetPostEvalPure() => new AnonymizeProcedure(Procedure, parentProcedure, ErrorReportedElement);
    }

    partial class ProcedureCall
    {
        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class ArithmeticCast
    {
        public IRValue GetPostEvalPure() => new ArithmeticCast(Operation, Input.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class ArrayLiteral
    {
        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class ArrayOperator
    {
        public IRValue GetPostEvalPure() => new ArrayOperator(Operation, ArrayValue.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class CharacterLiteral
    {
        public IRValue GetPostEvalPure() => new CharacterLiteral(Character, ErrorReportedElement);
    }

    partial class DecimalLiteral
    {
        public IRValue GetPostEvalPure() => new DecimalLiteral(Number, ErrorReportedElement);
    }

    partial class IntegerLiteral
    {
        public IRValue GetPostEvalPure() => new IntegerLiteral(Number, ErrorReportedElement);
    }

    partial class TrueLiteral
    {
        public IRValue GetPostEvalPure() => new TrueLiteral(ErrorReportedElement);
    }

    partial class FalseLiteral
    {
        public IRValue GetPostEvalPure() => new FalseLiteral(ErrorReportedElement);
    }

    partial class NothingLiteral
    {
        public IRValue GetPostEvalPure() => new NothingLiteral(ErrorReportedElement);
    }

    partial class SizeofOperator
    {
        public IRValue GetPostEvalPure() => new SizeofOperator(TypeToMeasure, ErrorReportedElement);
    }

    partial class MemoryGet
    {
        public IRValue GetPostEvalPure() => new MemoryGet(Type, Address.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class MemorySet
    {
        public IRValue GetPostEvalPure() => new MemoryGet(Type, Address.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class ComparativeOperator
    {
        public IRValue GetPostEvalPure() => new ComparativeOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class LogicalOperator
    {
        public IRValue GetPostEvalPure() => new LogicalOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class ArithmeticOperator
    {
        public IRValue GetPostEvalPure() => new ArithmeticOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class IfElseValue
    {
        public IRValue GetPostEvalPure() => new IfElseValue(Condition.GetPostEvalPure(), IfTrueValue.GetPostEvalPure(), IfFalseValue.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class VariableReference
    {
        public IRValue GetPostEvalPure() => new VariableReference(Variable, ErrorReportedElement);
    }

    partial class CSymbolReference
    {
        public IRValue GetPostEvalPure() => new CSymbolReference(CSymbol, ErrorReportedElement);
    }

    partial class SetVariable
    {
        public IRValue GetPostEvalPure() => new VariableReference(Variable, ErrorReportedElement);
    }

    partial class VariableDeclaration
    {
        public IRValue GetPostEvalPure() => new VariableReference(Variable, ErrorReportedElement);
    }

    partial class GetValueAtIndex
    {
        public IRValue GetPostEvalPure() => new GetValueAtIndex(Array.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class SetValueAtIndex
    {
        public IRValue GetPostEvalPure() => GetValueAtIndex.ComposeGetValueAtIndex(Array.GetPostEvalPure(), Index.GetPostEvalPure(), Type, ErrorReportedElement);
    }

    partial class GetPropertyValue
    {
        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, ErrorReportedElement);
    }

    partial class SetPropertyValue
    {
        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, ErrorReportedElement);
    }

    partial class MarshalIntoEnum
    {
        public IRValue GetPostEvalPure() => new MarshalIntoEnum(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class MarshalIntoInterface
    {
        public IRValue GetPostEvalPure() => new MarshalIntoInterface(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);
    }
}
