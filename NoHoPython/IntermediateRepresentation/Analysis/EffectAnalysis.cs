namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public bool IsPure { get; } //whether the evaluation of a value can potentially affect the evaluation of another
        public bool IsConstant { get; } //whether the evaluation of a value can be affected by the evaluation of another
        
        //gets a pure value - one that doesn't mutate state once evaluated - that can be safley evaluated following evaluation of the parent value
        public IRValue GetPostEvalPure();
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new IntegerLiteral(Number, ErrorReportedElement);
    }

    partial class DecimalLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new DecimalLiteral(Number, ErrorReportedElement);
    }

    partial class CharacterLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new CharacterLiteral(Character, ErrorReportedElement);
    }

    partial class TrueLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new TrueLiteral(ErrorReportedElement);
    }

    partial class FalseLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new FalseLiteral(ErrorReportedElement);
    }

    partial class EmptyTypeLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new EmptyTypeLiteral(Type, ErrorReportedElement);
    }

    partial class ArrayLiteral
    {
        public bool IsPure => Elements.TrueForAll((elem) => elem.IsPure);
        public bool IsConstant => Elements.TrueForAll((elem) => elem.IsConstant);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class InterpolatedString
    {
        public bool IsPure => InterpolatedValues.TrueForAll((value) => value is IRValue irValue ? irValue.IsPure : true);
        public bool IsConstant => InterpolatedValues.TrueForAll((value) => value is IRValue irValue ? irValue.IsConstant : true);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class AllocArray
    {
        public bool IsPure => Length.IsPure && ProtoValue.IsPure;
        public bool IsConstant => Length.IsConstant && ProtoValue.IsConstant;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class ProcedureCall
    {
        public virtual bool IsPure => false;
        public virtual bool IsConstant => false;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class AllocRecord
    {
        public override bool IsPure => Arguments.TrueForAll((arg) => arg.IsPure);
        public override bool IsConstant => Arguments.TrueForAll((arg) => arg.IsConstant);
    }

    partial class BinaryOperator
    {
        public virtual bool IsPure => Left.IsPure && Right.IsPure;
        public bool IsConstant => Left.IsConstant && Right.IsConstant;

        public abstract IRValue GetPostEvalPure();
    }

    partial class ComparativeOperator
    {
        public override IRValue GetPostEvalPure() => new ComparativeOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class LogicalOperator
    {
        public override IRValue GetPostEvalPure() => new LogicalOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class BitwiseOperator
    {
        public override IRValue GetPostEvalPure() => new BitwiseOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class ArithmeticOperator
    {
        public override IRValue GetPostEvalPure() => new ArithmeticOperator(Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class GetValueAtIndex
    {
        public bool IsPure => Array.IsPure && Index.IsPure;
        public bool IsConstant => Array.IsConstant && Index.IsConstant;

        public IRValue GetPostEvalPure() => new GetValueAtIndex(Array.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class SetValueAtIndex
    {
        public bool IsPure => false;
        public bool IsConstant => Array.IsConstant && Index.IsConstant && Value.IsConstant;

        public IRValue GetPostEvalPure() => GetValueAtIndex.ComposeGetValueAtIndex(Array.GetPostEvalPure(), Index.GetPostEvalPure(), Type, ErrorReportedElement);
    }

    partial class GetPropertyValue
    {
        public bool IsPure => Record.IsPure;
        public bool IsConstant => Record.IsConstant;

        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, ErrorReportedElement);
    }

    partial class SetPropertyValue
    {
        public bool IsPure => false;
        public bool IsConstant => Record.IsConstant && Value.IsConstant;

        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, ErrorReportedElement);
    }

    partial class ArithmeticCast
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new ArithmeticCast(Operation, Input.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class ArrayOperator
    {
        public bool IsPure => ArrayValue.IsPure;
        public bool IsConstant => ArrayValue.IsConstant;

        public IRValue GetPostEvalPure() => new ArrayOperator(Operation, ArrayValue.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class VariableReference
    {
        public bool IsPure => true;
        public bool IsConstant { get; private set; }

        public IRValue GetPostEvalPure() => new VariableReference(Variable, IsConstant, ErrorReportedElement);
    }

    partial class VariableDeclaration
    {
        public bool IsPure => false;
        public bool IsConstant => InitialValue.IsConstant;

        public IRValue GetPostEvalPure() => new VariableReference(Variable, false, ErrorReportedElement);
    }

    partial class SetVariable
    {
        public bool IsPure => false;
        public bool IsConstant => SetValue.IsConstant;

        public IRValue GetPostEvalPure() => new VariableReference(Variable, false, ErrorReportedElement);
    }

    partial class CSymbolReference
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new CSymbolReference(CSymbol, ErrorReportedElement);
    }

    partial class AnonymizeProcedure
    {
        public bool IsPure => true;
        public bool IsConstant => true; 
        
        public IRValue GetPostEvalPure() => new AnonymizeProcedure(Procedure, parentProcedure, ErrorReportedElement);
    }

    partial class IfElseValue
    {
        public bool IsPure => Condition.IsPure && IfTrueValue.IsPure && IfFalseValue.IsPure;
        public bool IsConstant => Condition.IsConstant && IfTrueValue.IsConstant && IfFalseValue.IsConstant;

        public IRValue GetPostEvalPure() => new IfElseValue(Condition.GetPostEvalPure(), IfTrueValue.GetPostEvalPure(), IfFalseValue.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class SizeofOperator
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new SizeofOperator(TypeToMeasure, ErrorReportedElement);
    }

    partial class MarshalIntoArray
    {
        public bool IsPure => Length.IsPure && Address.IsPure;
        public bool IsConstant => Length.IsConstant && Address.IsConstant;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);
    }

    partial class MarshalIntoEnum
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsConstant;

        public IRValue GetPostEvalPure() => new MarshalIntoEnum(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class UnwrapEnumValue
    {
        public bool IsPure => EnumValue.IsPure;
        public bool IsConstant => EnumValue.IsConstant;

        public IRValue GetPostEvalPure() => new UnwrapEnumValue(EnumValue.GetPostEvalPure(), Type, ErrorReportedElement);
    }

    partial class CheckEnumOption
    {
        public bool IsPure => EnumValue.IsPure;
        public bool IsConstant => EnumValue.IsConstant;

        public IRValue GetPostEvalPure() => new CheckEnumOption(EnumValue.GetPostEvalPure(), Type, ErrorReportedElement);
    }

    partial class MarshalIntoInterface
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsPure;

        public IRValue GetPostEvalPure() => new MarshalIntoInterface(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class MemoryGet
    {
        public override IRValue GetPostEvalPure() => new MemoryGet(Type, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class MemorySet
    {
        public bool IsPure => false;
        public bool IsConstant => Address.IsConstant && Index.IsConstant && Value.IsConstant;

        public IRValue GetPostEvalPure() => new MemoryGet(Type, Address.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);
    } 
}