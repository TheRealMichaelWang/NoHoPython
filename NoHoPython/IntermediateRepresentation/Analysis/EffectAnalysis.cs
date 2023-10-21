using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Syntax;

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class CannotCallImpureFunctionInPureFunction : IRGenerationError
    {
        private static string GetDescription(Purity purity)
        {
            return purity switch
            {
                Purity.Pure => "a pure function that doesn't mutate any external state",
                Purity.OnlyAffectsArguments => "an impure function that only mutates the state of it's arguments(not it's captured variables)",
                Purity.OnlyAffectsArgumentsAndCaptured => "an impure function that only mutates the state of it's arguments and captured variables",
                Purity.AffectsGlobals => "an impure function that may potentially affect some sort of internal global state or depends on some global mutable",
                _ => throw new NotImplementedException()
            };
        }

        public CannotCallImpureFunctionInPureFunction(Purity minimumPurity, Purity actionPurity, IAstElement errorReportedElement) : base(errorReportedElement, $"Cannot invoke {GetDescription(actionPurity)} in {GetDescription(minimumPurity)}.")
        {

        }
    }

    public sealed class CannotReadMutableGlobalStateInPureFunction : IRGenerationError
    {
        public CannotReadMutableGlobalStateInPureFunction(IAstElement errorReportedElement) : base(errorReportedElement, "Cannot read mutable global state in a function not marked as globally impure.")
        {

        }
    }

    partial interface IRStatement
    {
        public void EnsureMinimumPurity(Purity purity);
    }

    partial interface IRValue
    {
        public bool IsPure { get; } //whether the evaluation of a value can potentially affect the evaluation of another
        public bool IsConstant { get; } //whether the evaluation of a value can be affected by the evaluation of another
        
        //gets a pure value - one that doesn't mutate state once evaluated - that can be safley evaluated following evaluation of the parent value
        public IRValue GetPostEvalPure();

        public static bool EvaluationOrderGuarenteed(params IRValue[] operands) => EvaluationOrderGuarenteed(operands as IEnumerable<IRValue>);

        public static bool EvaluationOrderGuarenteed(IEnumerable<IRValue> operands) => operands.All((operand) => operand.IsPure) || operands.All((operand) => operand.IsConstant);

        public static bool HasPostEvalPure(IRValue value)
        {
            try
            {
                value.GetPostEvalPure();
                return true;
            }
            catch (NoPostEvalPureValue)
            {
                return false;
            }
        }

        public void EnsureMinimumPurity(Purity purity);
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
        public void EnsureMinimumPurity(Purity purity) => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public void EnsureMinimumPurity(Purity purity) => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public void EnsureMinimumPurity(Purity purity) => throw new InvalidOperationException();
    }

    partial class ForeignCDeclaration
    {
        public void EnsureMinimumPurity(Purity purity) => throw new InvalidOperationException();
    }

    partial class ForeignCProcedureDeclaration
    {
        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class CSymbolDeclaration
    {
        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class MemoryDestroy
    {
        public void EnsureMinimumPurity(Purity purity) => Address.EnsureMinimumPurity(purity);
    }

    partial class IfElseBlock
    {
        public void EnsureMinimumPurity(Purity purity)
        {
            Condition.EnsureMinimumPurity(purity);
            IfTrueBlock.EnsureMinimumPurity(purity);
            IfTrueBlock.EnsureMinimumPurity(purity);
        }
    }

    partial class IfBlock
    {
        public void EnsureMinimumPurity(Purity purity)
        {
            Condition.EnsureMinimumPurity(purity);
            IfTrueBlock.EnsureMinimumPurity(purity);
        }
    }

    partial class WhileBlock
    {
        public void EnsureMinimumPurity(Purity purity)
        {
            Condition.EnsureMinimumPurity(purity);
            WhileTrueBlock.EnsureMinimumPurity(purity);
        }
    }

    partial class MatchStatement
    {
        public void EnsureMinimumPurity(Purity purity)
        {
            MatchValue.EnsureMinimumPurity(purity);
            MatchHandlers.ForEach((handler) => handler.ToExecute.EnsureMinimumPurity(purity));
            DefaultHandler?.EnsureMinimumPurity(purity);
        }
    }

    partial class IterationForLoop
    {
        public void EnsureMinimumPurity(Purity purity)
        {
            UpperBound.EnsureMinimumPurity(purity);
            IteratorVariableDeclaration.EnsureMinimumPurity(purity);
            IterationBlock.EnsureMinimumPurity(purity);
        }
    }

    partial class CodeBlock
    {
        public void EnsureMinimumPurity(Purity purity) => Statements?.ForEach((statement) => statement.EnsureMinimumPurity(purity));
    }

    partial class LoopStatement
    {
        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class ReturnStatement
    {
        public void EnsureMinimumPurity(Purity purity) => ToReturn.EnsureMinimumPurity(purity);
    }

    partial class AssertStatement
    {
        public void EnsureMinimumPurity(Purity purity) => Condition.EnsureMinimumPurity(purity);
    }

    partial class AbortStatement
    {
        public void EnsureMinimumPurity(Purity purity) { }
    } 
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new IntegerLiteral(Number, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class DecimalLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new DecimalLiteral(Number, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class CharacterLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new CharacterLiteral(Character, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class TrueLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new TrueLiteral(ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class FalseLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new FalseLiteral(ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class NullPointerLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new NullPointerLiteral(Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class StaticCStringLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new StaticCStringLiteral(String, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class EmptyTypeLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new EmptyTypeLiteral(Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class ArrayLiteral
    {
        public bool IsPure => Elements.TrueForAll((elem) => elem.IsPure);
        public bool IsConstant => Elements.TrueForAll((elem) => elem.IsConstant);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) => Elements.ForEach((element) => element.EnsureMinimumPurity(purity));
    }

    partial class TupleLiteral
    {
        public bool IsPure => Elements.TrueForAll((elem) => elem.IsPure);
        public bool IsConstant => Elements.TrueForAll((elem) => elem.IsConstant);

        public IRValue GetPostEvalPure() => new TupleLiteral(Elements.Select((element) => element.GetPostEvalPure()).ToList(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Elements.ForEach((element) => element.EnsureMinimumPurity(purity));
    }

    partial class MarshalIntoLowerTuple
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsConstant;

        public IRValue GetPostEvalPure() => new MarshalIntoLowerTuple(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Value.EnsureMinimumPurity(purity);
    }

    partial class InterpolatedString
    {
        public bool IsPure => InterpolatedValues.TrueForAll((value) => value is IRValue irValue ? irValue.IsPure : true);
        public bool IsConstant => InterpolatedValues.TrueForAll((value) => value is IRValue irValue ? irValue.IsConstant : true);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) => InterpolatedValues.ForEach((value) =>
        {
            if (value is IRValue irValue)
                irValue.EnsureMinimumPurity(purity);
        });
    }

    partial class AllocArray
    {
        public bool IsPure => Length.IsPure && ProtoValue.IsPure;
        public bool IsConstant => Length.IsConstant && ProtoValue.IsConstant;

        public IRValue GetPostEvalPure() => new AllocArray(ElementType, Length.GetPostEvalPure(), ProtoValue.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity)
        {
            Length.EnsureMinimumPurity(purity);
            ProtoValue.EnsureMinimumPurity(purity);
        }
    }

    partial class AllocMemorySpan
    {
        public bool IsPure => ProtoValue.IsPure;
        public bool IsConstant => ProtoValue.IsConstant;

        public IRValue GetPostEvalPure() => new AllocMemorySpan(ElementType, Length, ProtoValue.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => ProtoValue.EnsureMinimumPurity(purity);
    }

    partial class ProcedureCall
    {
        public virtual bool IsPure => FunctionPurity == Purity.Pure && Arguments.TrueForAll((arg) => arg.IsPure);
        public virtual bool IsConstant => FunctionPurity == Purity.Pure && Arguments.TrueForAll((arg) => arg.IsConstant);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public virtual void EnsureMinimumCallLevelPurity(Purity purity) { }

        public virtual void EnsureMinimumPurity(Purity purity)
        {
            if (purity <= Purity.AffectsGlobals)
                return;

            EnsureMinimumPurity(purity);
            Arguments.ForEach((argument) => argument.EnsureMinimumPurity(purity));
        }
    }

    partial class AllocRecord
    {
        public override bool IsConstant => Arguments.TrueForAll((arg) => arg.IsConstant);
    }

    partial class AnonymousProcedureCall
    {
        public override bool IsPure => ProcedureValue.IsPure && base.IsPure;

        public override void EnsureMinimumCallLevelPurity(Purity purity)
        {
            if(FunctionPurity <= Purity.OnlyAffectsArgumentsAndCaptured)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }
    }

    partial class OptimizedRecordMessageCall
    {
        public override bool IsPure => Record.IsPure && base.IsPure;

        public override void EnsureMinimumPurity(Purity purity)
        {
            Record.EnsureMinimumPurity(purity);
            base.EnsureMinimumPurity(purity);
        }

        public override void EnsureMinimumCallLevelPurity(Purity purity)
        {
            if (FunctionPurity <= Purity.AffectsGlobals)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }
    }

    partial class LinkedProcedureCall
    {
        public override void EnsureMinimumCallLevelPurity(Purity purity)
        {
            if (FunctionPurity <= Purity.AffectsGlobals)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }
    }

    partial class ForeignFunctionCall
    {
        public override void EnsureMinimumCallLevelPurity(Purity purity)
        {
            if (FunctionPurity <= Purity.AffectsGlobals)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }
    }

    partial class BinaryOperator
    {
        public virtual bool IsPure => Left.IsPure && Right.IsPure;
        public bool IsConstant => Left.IsConstant && Right.IsConstant;

        public abstract IRValue GetPostEvalPure();

        public void EnsureMinimumPurity(Purity purity)
        {
            Left.EnsureMinimumPurity(purity);
            Right.EnsureMinimumPurity(purity);
        }
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
        public override IRValue GetPostEvalPure() => new ArithmeticOperator(Type, Operation, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class PointerAddOperator
    {
        public override IRValue GetPostEvalPure() => new PointerAddOperator(Address.GetPostEvalPure(), Left.GetPostEvalPure(), ErrorReportedElement);
    }

    partial class GetValueAtIndex
    {
        public bool IsPure => Array.IsPure && Index.IsPure;
        public bool IsConstant => Array.IsConstant && Index.IsConstant;

        public IRValue GetPostEvalPure() => new GetValueAtIndex(Type, Array.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) 
        {
            Array.EnsureMinimumPurity(purity);
            Index.EnsureMinimumPurity(purity);
        }
    }

    partial class SetValueAtIndex
    {
        public bool IsPure => false;
        public bool IsConstant => Array.IsConstant && Index.IsConstant && Value.IsConstant;

        public IRValue GetPostEvalPure() => new GetValueAtIndex(Type, Array.GetPostEvalPure(), Index.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) 
        {
            Array.EnsureMinimumPurity(purity);
            Index.EnsureMinimumPurity(purity);
            Value.EnsureMinimumPurity(purity);
        }
    }

    partial class GetPropertyValue
    {
        public bool IsPure => Record.IsPure;
        public bool IsConstant => Record.IsConstant;

        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, Refinements, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Record.EnsureMinimumPurity(purity);
    }

    partial class SetPropertyValue
    {
        public bool IsPure => false;
        public bool IsConstant => Record.IsConstant && Value.IsConstant;

        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, null, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity)
        {
            Record.EnsureMinimumPurity(purity);
            Value.EnsureMinimumPurity(purity);
        }
    }

    partial class ArithmeticCast
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new ArithmeticCast(Operation, Input.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Input.EnsureMinimumPurity(purity);
    }

    partial class HandleCast
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new HandleCast(TargetHandleType, Input.GetPostEvalPure(), ErrorReportedElement); 
        
        public void EnsureMinimumPurity(Purity purity) => Input.EnsureMinimumPurity(purity);
    }

    partial class ArrayOperator
    {
        public bool IsPure => ArrayValue.IsPure;
        public bool IsConstant => ArrayValue.IsConstant;

        public IRValue GetPostEvalPure() => new ArrayOperator(Operation, ArrayValue.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => ArrayValue.EnsureMinimumPurity(purity);
    }

    partial class VariableReference
    {
        public bool IsPure => true;
        public bool IsConstant { get; private set; }

        public IRValue GetPostEvalPure() => new VariableReference(Variable, IsConstant, Refinements, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class VariableDeclaration
    {
        public bool IsPure => false;
        public bool IsConstant => InitialValue.IsConstant;

        public IRValue GetPostEvalPure() => new VariableReference(Variable, false, null, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => InitialValue.EnsureMinimumPurity(purity);
    }

    partial class SetVariable
    {
        public bool IsPure => false;
        public bool IsConstant => SetValue.IsConstant;

        public IRValue GetPostEvalPure() => new VariableReference(Variable, false, null, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => SetValue.EnsureMinimumPurity(purity);
    }

    partial class CSymbolReference
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new CSymbolReference(CSymbol, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity)
        {
            if (CSymbol.IsMutableGlobal && purity >= Purity.OnlyAffectsArgumentsAndCaptured)
                throw new CannotReadMutableGlobalStateInPureFunction(ErrorReportedElement);
        }
    }

    partial class AnonymizeProcedure
    {
        public bool IsPure => true;
        public bool IsConstant => true; 
        
        public IRValue GetPostEvalPure() => new AnonymizeProcedure(Procedure, GetFunctionHandle, parentProcedure, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class IfElseValue
    {
        public bool IsPure => Condition.IsPure && IfTrueValue.IsPure && IfFalseValue.IsPure;
        public bool IsConstant => Condition.IsConstant && IfTrueValue.IsConstant && IfFalseValue.IsConstant;

        public IRValue GetPostEvalPure() => new IfElseValue(Type, Condition.GetPostEvalPure(), IfTrueValue.GetPostEvalPure(), IfFalseValue.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity)
        {
            Condition.EnsureMinimumPurity(purity);
            IfTrueValue.EnsureMinimumPurity(purity);
            IfFalseValue.EnsureMinimumPurity(purity);
        }
    }

    partial class SizeofOperator
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new SizeofOperator(TypeToMeasure, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
    }

    partial class MarshalHandleIntoArray
    {
        public bool IsPure => Length.IsPure && Address.IsPure;
        public bool IsConstant => Length.IsConstant && Address.IsConstant;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) 
        {
            Length.EnsureMinimumPurity(purity);
            Address.EnsureMinimumPurity(purity);
        } 
    }

    partial class MarshalMemorySpanIntoArray
    {
        public bool IsPure => Span.IsPure;
        public bool IsConstant => Span.IsConstant;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) => Span.EnsureMinimumPurity(purity);
    }

    partial class MarshalIntoEnum
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsConstant;

        public IRValue GetPostEvalPure() => new MarshalIntoEnum(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Value.EnsureMinimumPurity(purity);
    }

    partial class UnwrapEnumValue
    {
        public bool IsPure => false;
        public bool IsConstant => EnumValue.IsConstant;

        public IRValue GetPostEvalPure() => new UnwrapEnumValue(EnumValue.GetPostEvalPure(), Type, ErrorReturnEnum, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => EnumValue.EnsureMinimumPurity(purity);
    }

    partial class CheckEnumOption
    {
        public bool IsPure => EnumValue.IsPure;
        public bool IsConstant => EnumValue.IsConstant;

        public IRValue GetPostEvalPure() => new CheckEnumOption(EnumValue.GetPostEvalPure(), Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => EnumValue.EnsureMinimumPurity(purity);
    }

    partial class MarshalIntoInterface
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsPure;

        public IRValue GetPostEvalPure() => new MarshalIntoInterface(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Value.EnsureMinimumPurity(purity);
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

        public void EnsureMinimumPurity(Purity purity)
        {
            Address.EnsureMinimumPurity(purity);
            Index.EnsureMinimumPurity(purity);
            Value.EnsureMinimumPurity(purity);
        }
    } 
}