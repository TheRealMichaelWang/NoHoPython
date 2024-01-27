using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;
using System.Diagnostics;

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

        public static bool EvaluationOrderGuarenteed(params IRValue[] operands)
        {
            if (operands.All((operand) => operand.IsPure) || operands.All((operand) => operand.IsConstant))
                return true;

            for (int i = 0; i < operands.Length; i++)
            {
                if (operands[i].IsPure)
                    continue;

                for (int j = 0; j < operands.Length; j++)
                {
                    if (i == j || operands[j].IsConstant)
                        continue;

                    if (operands[j].IsAffectedByEvaluation(operands[j]))
                        return false;

                    List<IRValue> potentiallyMutatedValues = new();
                    operands[i].GetMutatedValues(potentiallyMutatedValues);

                    if (potentiallyMutatedValues.Any(mutatedValue => operands[j].IsAffectedByMutation(mutatedValue)))
                        return false;
                }
            }
            return true;
        }

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

        //ensures the value meets a minimum purity standard when evaluated
        public void EnsureMinimumPurity(Purity purity);

        //gets a list of values that are potentially mutated when evaluated
        //returns a bool indicating whether the function mutates an "infinite" amount of values
        public void GetMutatedValues(List<IRValue> affectedValues);

        //when a value is mutated, gets values that are also mutated as a result (including itself)
        public void GetCoMutatedValues(List<IRValue> coMutatedValues);

        //returns true, if mutatedValue affects the value when it's mutated
        public bool IsAffectedByMutation(IRValue mutatedValue);

        //returns true, if evaluatedValue affects the value when it's evaluated
        public bool IsAffectedByEvaluation(IRValue evaluatedValue);
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

        public void GetAffectedValues(List<IRValue> affectedValues)
        {
            Address.GetMutatedValues(affectedValues);
        }
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
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class DecimalLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new DecimalLiteral(Number, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class CharacterLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new CharacterLiteral(Character, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class TrueLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new TrueLiteral(ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class FalseLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new FalseLiteral(ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class NullPointerLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new NullPointerLiteral(Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class StaticCStringLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new StaticCStringLiteral(String, Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class EmptyTypeLiteral
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new EmptyTypeLiteral(Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }
        public void GetMutatedValues(List<IRValue> affectedValues) { }
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }
        public bool IsAffectedByMutation(IRValue mutatedValue) => false;
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class ArrayLiteral
    {
        public bool IsPure => Elements.TrueForAll((elem) => elem.IsPure);
        public bool IsConstant => Elements.TrueForAll((elem) => elem.IsConstant);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) => Elements.ForEach((element) => element.EnsureMinimumPurity(purity));
        public void GetMutatedValues(List<IRValue> affectedValues) => Elements.ForEach((element) => element.GetMutatedValues(affectedValues));
        
        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Elements.ForEach(element => element.GetCoMutatedValues(coMutatedValues));
        }
        
        public bool IsAffectedByMutation(IRValue mutatedValue) => Elements.Any((element) => element.IsAffectedByMutation(mutatedValue));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Elements.Any((element) => element.IsAffectedByEvaluation(evaluatedValue));
    }

    partial class TupleLiteral
    {
        public bool IsPure => Elements.TrueForAll((elem) => elem.IsPure);
        public bool IsConstant => Elements.TrueForAll((elem) => elem.IsConstant);

        public IRValue GetPostEvalPure() => new TupleLiteral(Elements.Select((element) => element.GetPostEvalPure()).ToList(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Elements.ForEach((element) => element.EnsureMinimumPurity(purity));
        public void GetMutatedValues(List<IRValue> affectedValues) => Elements.ForEach((element) => element.GetMutatedValues(affectedValues));

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Elements.ForEach(element => element.GetCoMutatedValues(coMutatedValues));
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Elements.Any((element) => element.IsAffectedByMutation(mutatedValue));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Elements.Any((element) => element.IsAffectedByEvaluation(evaluatedValue));
    }

    partial class ReferenceLiteral
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new ReferenceLiteral(Input.GetPostEvalPure(), ErrorReportedElement);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Input.GetCoMutatedValues(coMutatedValues);
        }

        public void EnsureMinimumPurity(Purity purity) => Input.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Input.GetMutatedValues(affectedValues);

        public bool IsAffectedByMutation(IRValue mutatedValue) => Input.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Input.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class MarshalIntoLowerTuple
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsConstant;

        public IRValue GetPostEvalPure() => new MarshalIntoLowerTuple(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Value.EnsureMinimumPurity(purity);
        public void GetMutatedValues(List<IRValue> affectedValues) => Value.GetMutatedValues(affectedValues);
        public bool IsAffectedByMutation(IRValue mutatedValue) => Value.IsAffectedByMutation(mutatedValue);
        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Value.IsAffectedByEvaluation(evaluatedValue);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Length.GetMutatedValues(affectedValues);
            ProtoValue.GetMutatedValues(affectedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Length.IsAffectedByMutation(mutatedValue) || ProtoValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Length.IsAffectedByEvaluation(evaluatedValue) || ProtoValue.IsAffectedByEvaluation(evaluatedValue);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);
    }

    partial class AllocMemorySpan
    {
        public bool IsPure => ProtoValue.IsPure;
        public bool IsConstant => ProtoValue.IsConstant;

        public IRValue GetPostEvalPure() => new AllocMemorySpan(ElementType, Length, ProtoValue.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => ProtoValue.EnsureMinimumPurity(purity);
        public void GetMutatedValues(List<IRValue> affectedValues) => ProtoValue.GetMutatedValues(affectedValues);
        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);
        public bool IsAffectedByMutation(IRValue mutatedValue) => ProtoValue.IsAffectedByMutation(mutatedValue);
        public bool IsAffectedByEvaluation(IRValue mutatedValue) => ProtoValue.IsAffectedByEvaluation(mutatedValue);
    }

    partial class ProcedureCall
    {
        public virtual bool IsPure => FunctionPurity == Purity.Pure && Arguments.TrueForAll((arg) => arg.IsPure);
        public virtual bool IsConstant => FunctionPurity == Purity.Pure && Arguments.TrueForAll((arg) => arg.IsConstant);

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public virtual void EnsureMinimumCallLevelPurity(Purity purity) { }

        public virtual void EnsureMinimumPurity(Purity purity)
        {
            if (purity >= Purity.AffectsGlobals)
                return;

            EnsureMinimumPurity(purity);
            Arguments.ForEach((argument) => argument.EnsureMinimumPurity(purity));
        }

        public virtual void GetMutatedValues(List<IRValue> affectedValues)
        {
            if (FunctionPurity <= Purity.Pure)
                return;

            Arguments.ForEach(arg => arg.GetMutatedValues(affectedValues));
            GetCoMutatedValues(affectedValues);
            Arguments.ForEach(arg => {
                if(arg.Type.HasMutableChildren)
                    arg.GetCoMutatedValues(affectedValues);
            });
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);

            if (CanMutateArguments)
                coMutatedValues.AddRange(Arguments.Where(argument => (argument.Type.IsCompatibleWith(Type) || argument.Type.ContainsType(Type)) && !argument.IsReadOnly));
        }

        public virtual bool IsAffectedByMutation(IRValue mutatedValue) => Arguments.Any(arg => arg.IsAffectedByMutation(mutatedValue));

        public virtual bool IsAffectedByEvaluation(IRValue evaluatedValue)
        {
            if (Arguments.Any(arg => arg.IsAffectedByEvaluation(evaluatedValue)))
                return true;

            if (FunctionPurity <= Purity.Pure)
                return false;

            if (evaluatedValue is AnonymousProcedureCall anonymousProcedureCall && anonymousProcedureCall.FunctionPurity >= Purity.OnlyAffectsArgumentsAndCaptured)
                return true;
            if (FunctionPurity >= Purity.AffectsGlobals && evaluatedValue is ForeignFunctionCall foreignFunctionCall && foreignFunctionCall.FunctionPurity >= Purity.AffectsGlobals)
                return true;
            return false;
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
            if(FunctionPurity >= Purity.OnlyAffectsArgumentsAndCaptured)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }

        public override bool IsAffectedByMutation(IRValue mutatedValue) => ProcedureValue.IsAffectedByMutation(mutatedValue) || base.IsAffectedByMutation(mutatedValue) || mutatedValue.Type.IsReferenceType;

        public override bool IsAffectedByEvaluation(IRValue evaluatedValue)
        {
            if (ProcedureValue.IsAffectedByEvaluation(evaluatedValue))
                return true;

            return base.IsAffectedByEvaluation(evaluatedValue);
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
            if (FunctionPurity >= Purity.AffectsGlobals)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }

        public override void GetMutatedValues(List<IRValue> affectedValues)
        {
            Record.GetMutatedValues(affectedValues);

            if (FunctionPurity >= Purity.OnlyAffectsArgumentsAndCaptured)
                Record.GetCoMutatedValues(affectedValues);
            base.GetMutatedValues(affectedValues);
        }

        public override bool IsAffectedByMutation(IRValue mutatedValue) => Record.IsAffectedByMutation(mutatedValue) || base.IsAffectedByMutation(mutatedValue);

        public override bool IsAffectedByEvaluation(IRValue evaluatedValue)
        {
            if (Record.IsAffectedByEvaluation(evaluatedValue))
                return true;
            return base.IsAffectedByEvaluation(evaluatedValue);
        }
    }

    partial class LinkedProcedureCall
    {
        public override void EnsureMinimumCallLevelPurity(Purity purity)
        {
            if (FunctionPurity >= Purity.AffectsGlobals)
                throw new CannotCallImpureFunctionInPureFunction(purity, FunctionPurity, ErrorReportedElement);
        }

        public override void GetMutatedValues(List<IRValue> affectedValues)
        {
            base.GetMutatedValues(affectedValues);

            if(FunctionPurity >= Purity.OnlyAffectsArgumentsAndCaptured)
            {
                if (Procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
                {
                    Debug.Assert(parentProcedure != null);

                    foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
                    {
                        if (variable.IsRecordSelf && variable.ParentProcedure == Procedure.ProcedureDeclaration)
                        {
                            Debug.Assert(parentProcedure.CapturedVariables.Any((captured) => captured.IsRecordSelf && captured.ParentProcedure == parentProcedure));
                            affectedValues.Add(new VariableReference(parentProcedure.SanitizeVariable(parentProcedure.CapturedVariables.Where((captured) => captured.IsRecordSelf && captured.ParentProcedure == parentProcedure).First(), false, ErrorReportedElement), null, ErrorReportedElement));
                        }
                        else
                            affectedValues.Add(new VariableReference(parentProcedure.SanitizeVariable(variable, false, ErrorReportedElement), null, ErrorReportedElement));
                    }
                }
            }
        }

        public override bool IsAffectedByMutation(IRValue mutatedValue)
        {
            if (Procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
            {
                Debug.Assert(parentProcedure != null);

                foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
                {
                    VariableReference variableReference;
                    if (variable.IsRecordSelf && variable.ParentProcedure == Procedure.ProcedureDeclaration)
                        variableReference = new VariableReference(parentProcedure.SanitizeVariable(parentProcedure.CapturedVariables.Where((captured) => captured.IsRecordSelf && captured.ParentProcedure == parentProcedure).First(), false, ErrorReportedElement), null, ErrorReportedElement);
                    else
                        variableReference = new VariableReference(parentProcedure.SanitizeVariable(variable, false, ErrorReportedElement), null, ErrorReportedElement);
                    if (variableReference.IsAffectedByMutation(mutatedValue))
                        return true;
                }
            }
            return base.IsAffectedByMutation(mutatedValue);
        }

        public override bool IsAffectedByEvaluation(IRValue evaluatedValue)
        {
            if (Procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
            {
                Debug.Assert(parentProcedure != null);

                foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
                {
                    VariableReference variableReference;
                    if (variable.IsRecordSelf && variable.ParentProcedure == Procedure.ProcedureDeclaration)
                        variableReference = new VariableReference(parentProcedure.SanitizeVariable(parentProcedure.CapturedVariables.Where((captured) => captured.IsRecordSelf && captured.ParentProcedure == parentProcedure).First(), false, ErrorReportedElement), null, ErrorReportedElement);
                    else
                        variableReference = new VariableReference(parentProcedure.SanitizeVariable(variable, false, ErrorReportedElement), null, ErrorReportedElement);
                    if (variableReference.IsAffectedByEvaluation(evaluatedValue))
                        return true;
                }
            }
            return base.IsAffectedByEvaluation(evaluatedValue);
        }
    }

    partial class ForeignFunctionCall
    {
        public override void EnsureMinimumCallLevelPurity(Purity purity)
        {
            if (FunctionPurity >= Purity.AffectsGlobals)
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Left.GetMutatedValues(affectedValues);
            Right.GetMutatedValues(affectedValues);
        }

        public virtual void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue) => Left.IsAffectedByMutation(mutatedValue) || Right.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Left.IsAffectedByEvaluation(evaluatedValue) || Right.IsAffectedByEvaluation(evaluatedValue);
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Array.GetMutatedValues(affectedValues);
            Index.GetMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Array.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Array.IsAffectedByMutation(mutatedValue) || Index.IsAffectedByMutation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Array.IsAffectedByEvaluation(evaluatedValue) || Index.IsAffectedByEvaluation(evaluatedValue);
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Array.GetMutatedValues(affectedValues);
            Index.GetMutatedValues(affectedValues);

            if(Value.Type.IsReferenceType && Value.Type.HasMutableChildren)
                Value.GetMutatedValues(affectedValues);

            new GetValueAtIndex(Type, Array, Index, ErrorReportedElement).GetCoMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Array.GetCoMutatedValues(coMutatedValues);

            if (Value.Type.IsReferenceType && Value.Type.HasMutableChildren)
                Value.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Array.IsAffectedByMutation(mutatedValue) || Index.IsAffectedByMutation(mutatedValue) || Value.IsAffectedByMutation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Array.IsAffectedByEvaluation(evaluatedValue) || Index.IsAffectedByEvaluation(evaluatedValue) || Value.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class GetPropertyValue
    {
        public bool IsPure => Record.IsPure;
        public bool IsConstant => Record.IsConstant;

        public IRValue GetPostEvalPure() => new GetPropertyValue(Record.GetPostEvalPure(), Property.Name, Refinements, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Record.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Record.GetMutatedValues(affectedValues);
        
        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Record.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Record.IsAffectedByMutation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Record.IsAffectedByEvaluation(evaluatedValue);
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Record.GetMutatedValues(affectedValues);

            if(Value.Type.IsReferenceType && Value.Type.HasMutableChildren)
                Value.GetMutatedValues(affectedValues);

            new GetPropertyValue(Record, Property.Name, null, ErrorReportedElement).GetMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Record.GetCoMutatedValues(coMutatedValues);

            if (Value.Type.IsReferenceType && Value.Type.HasMutableChildren)
                Value.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Record.IsAffectedByMutation(mutatedValue) || Value.IsAffectedByMutation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Record.IsAffectedByEvaluation(evaluatedValue) || Value.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class ReleaseReferenceElement
    {
        public bool IsPure => false;
        public bool IsConstant => ReferenceBox.IsConstant;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) => ReferenceBox.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => affectedValues.Add(ReferenceBox);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            ReferenceBox.GetCoMutatedValues(coMutatedValues);
            new GetPropertyValue(ReferenceBox, "elem", null, ErrorReportedElement).GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => ReferenceBox.IsAffectedByEvaluation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => ReferenceBox.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class SetReferenceTypeElement
    {
        public bool IsPure => false;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity)
        {
            ReferenceBox.EnsureMinimumPurity(purity);
            NewElement.EnsureMinimumPurity(purity);
        }

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            ReferenceBox.GetMutatedValues(affectedValues);

            if (NewElement.Type.IsReferenceType && NewElement.Type.HasMutableChildren)
                NewElement.GetMutatedValues(affectedValues);

            new GetPropertyValue(ReferenceBox, "elem", null, ErrorReportedElement).GetMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            ReferenceBox.GetCoMutatedValues(coMutatedValues);

            if (NewElement.Type.IsReferenceType && NewElement.Type.HasMutableChildren)
                NewElement.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => ReferenceBox.IsAffectedByEvaluation(mutatedValue) || NewElement.IsAffectedByEvaluation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => ReferenceBox.IsAffectedByEvaluation(evaluatedValue) || NewElement.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class ArithmeticCast
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new ArithmeticCast(Operation, Input.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Input.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Input.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue) => Input.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Input.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class HandleCast
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new HandleCast(TargetHandleType, Input.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Input.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Input.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Input.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Input.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Input.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class AutoCast
    {
        public bool IsPure => Input.IsPure;
        public bool IsConstant => Input.IsConstant;

        public IRValue GetPostEvalPure() => new AutoCast(Type, Input.GetPostEvalPure());

        public void EnsureMinimumPurity(Purity purity) => Input.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Input.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Input.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Input.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Input.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class ArrayOperator
    {
        public bool IsPure => ArrayValue.IsPure;
        public bool IsConstant => ArrayValue.IsConstant;

        public IRValue GetPostEvalPure() => new ArrayOperator(Operation, ArrayValue.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => ArrayValue.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => ArrayValue.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            ArrayValue.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => ArrayValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => ArrayValue.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class VariableReference
    {
        public bool IsPure => true;
        public bool IsConstant { get; private set; }

        public IRValue GetPostEvalPure() => new VariableReference(Variable, IsConstant, Refinements, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }

        public void GetMutatedValues(List<IRValue> affectedValues) { }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue) => (mutatedValue is VariableReference variableReference && Variable == variableReference.Variable) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => (evaluatedValue is AnonymousProcedureCall anonymousProcedureCall && anonymousProcedureCall.FunctionPurity >= Purity.OnlyAffectsArgumentsAndCaptured && (Type is RecordType || Type.IsReferenceType) && Type.HasMutableChildren) || (evaluatedValue is ProcedureCall procedureCall && procedureCall.FunctionPurity >= Purity.AffectsGlobals);
    }

    partial class VariableDeclaration
    {
        public bool IsPure => false;
        public bool IsConstant => InitialValue.IsConstant;

        public IRValue GetPostEvalPure() => new VariableReference(Variable, false, null, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => InitialValue.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) { }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            new VariableReference(Variable, false, null, ErrorReportedElement).GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => InitialValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => InitialValue.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class SetVariable
    {
        public bool IsPure => false;
        public bool IsConstant => SetValue.IsConstant;

        public IRValue GetPostEvalPure() => new VariableReference(Variable, false, null, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => SetValue.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            if(SetValue.Type.IsReferenceType && SetValue.Type.HasMutableChildren)
                SetValue.GetMutatedValues(affectedValues);

            new VariableReference(Variable, false, null, ErrorReportedElement).GetCoMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            new VariableReference(Variable, false, null, ErrorReportedElement).GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => SetValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => SetValue.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class CSymbolReference
    {
        public bool IsPure => true;
        public bool IsConstant => CSymbol.IsMutableGlobal;

        public IRValue GetPostEvalPure() => new CSymbolReference(CSymbol, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity)
        {
            if (CSymbol.IsMutableGlobal && purity < Purity.AffectsGlobals)
                throw new CannotReadMutableGlobalStateInPureFunction(ErrorReportedElement);
        }

        public void GetMutatedValues(List<IRValue> affectedValues) { }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue)
        {
            if (!CSymbol.IsMutableGlobal)
                return false;

            return mutatedValue is CSymbolReference cSymbolReference && cSymbolReference.CSymbol == CSymbol;
        }

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => CSymbol.IsMutableGlobal && evaluatedValue is ProcedureCall procedureCall && procedureCall.FunctionPurity >= Purity.AffectsGlobals;
    }

    partial class AnonymizeProcedure
    {
        public bool IsPure => true;
        public bool IsConstant => true; 
        
        public IRValue GetPostEvalPure() => new AnonymizeProcedure(Procedure, GetFunctionHandle, parentProcedure, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }

        public void GetMutatedValues(List<IRValue> affectedValues) { }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }

        public bool IsAffectedByMutation(IRValue mutatedValue) => mutatedValue is VariableReference variableReference && Procedure.ProcedureDeclaration.CapturedVariables.Any(capturedVariable => capturedVariable == variableReference.Variable);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
    }

    partial class StartNewThread
    {
        public bool IsPure => false;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) { }

        public void GetMutatedValues(List<IRValue> affectedValues) { }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }

        public bool IsAffectedByMutation(IRValue mutatedValue) => mutatedValue is VariableReference variableReference && ProcedureReference.ProcedureDeclaration.CapturedVariables.Any(capturedVariable => capturedVariable == variableReference.Variable);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Condition.GetMutatedValues(affectedValues);
            IfTrueValue.GetMutatedValues(affectedValues);
            IfFalseValue.GetMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            IfTrueValue.GetCoMutatedValues(coMutatedValues);
            IfFalseValue.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Condition.IsAffectedByMutation(mutatedValue) || IfTrueValue.IsAffectedByMutation(mutatedValue) || IfFalseValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Condition.IsAffectedByEvaluation(evaluatedValue) || IfTrueValue.IsAffectedByEvaluation(evaluatedValue) || IfFalseValue.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class SizeofOperator
    {
        public bool IsPure => true;
        public bool IsConstant => true;

        public IRValue GetPostEvalPure() => new SizeofOperator(TypeToMeasure, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) { }

        public void GetMutatedValues(List<IRValue> affectedValues) { }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) { }

        public bool IsAffectedByMutation(IRValue mutatedValue) => false;

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => false;
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Length.GetMutatedValues(affectedValues);
            Address.GetMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue) => Length.IsAffectedByMutation(mutatedValue) || Address.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Length.IsAffectedByEvaluation(evaluatedValue) || Address.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class MarshalMemorySpanIntoArray
    {
        public bool IsPure => Span.IsPure;
        public bool IsConstant => Span.IsConstant;

        public IRValue GetPostEvalPure() => throw new NoPostEvalPureValue(this);

        public void EnsureMinimumPurity(Purity purity) => Span.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Span.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue) => Span.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Span.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class MarshalIntoEnum
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsConstant;

        public IRValue GetPostEvalPure() => new MarshalIntoEnum(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Value.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Value.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Value.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Value.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Value.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class UnwrapEnumValue
    {
        public bool IsPure => false;
        public bool IsConstant => EnumValue.IsConstant;

        public IRValue GetPostEvalPure() => new UnwrapEnumValue(EnumValue.GetPostEvalPure(), Type, ErrorReturnType, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => EnumValue.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => EnumValue.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            EnumValue.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => EnumValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => EnumValue.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class CheckEnumOption
    {
        public bool IsPure => EnumValue.IsPure;
        public bool IsConstant => EnumValue.IsConstant;

        public IRValue GetPostEvalPure() => new CheckEnumOption(EnumValue.GetPostEvalPure(), Type, ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => EnumValue.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => EnumValue.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues) => coMutatedValues.Add(this);

        public bool IsAffectedByMutation(IRValue mutatedValue) => EnumValue.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => EnumValue.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class MarshalIntoInterface
    {
        public bool IsPure => Value.IsPure;
        public bool IsConstant => Value.IsPure;

        public IRValue GetPostEvalPure() => new MarshalIntoInterface(TargetType, Value.GetPostEvalPure(), ErrorReportedElement);

        public void EnsureMinimumPurity(Purity purity) => Value.EnsureMinimumPurity(purity);

        public void GetMutatedValues(List<IRValue> affectedValues) => Value.GetMutatedValues(affectedValues);

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Value.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Value.IsAffectedByMutation(mutatedValue);

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Value.IsAffectedByEvaluation(evaluatedValue);
    }

    partial class MemoryGet
    {
        public override IRValue GetPostEvalPure() => new MemoryGet(Type, Left.GetPostEvalPure(), Right.GetPostEvalPure(), ErrorReportedElement);

        public override void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            base.GetCoMutatedValues(coMutatedValues);
            Left.GetCoMutatedValues(coMutatedValues);
        }
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

        public void GetMutatedValues(List<IRValue> affectedValues)
        {
            Address.GetMutatedValues(affectedValues);
            Index.GetMutatedValues(affectedValues);

            if(Value.Type.IsReferenceType && Value.Type.HasMutableChildren)
                Value.GetMutatedValues(affectedValues);
        }

        public void GetCoMutatedValues(List<IRValue> coMutatedValues)
        {
            coMutatedValues.Add(this);
            Address.GetCoMutatedValues(coMutatedValues);
        }

        public bool IsAffectedByMutation(IRValue mutatedValue) => Address.IsAffectedByMutation(mutatedValue) || Index.IsAffectedByMutation(mutatedValue) || Value.IsAffectedByMutation(mutatedValue) || (!IsReadOnly && Type.IsReferenceType && Type.HasMutableChildren && Type.IsCompatibleWith(mutatedValue.Type));

        public bool IsAffectedByEvaluation(IRValue evaluatedValue) => Address.IsAffectedByEvaluation(evaluatedValue) || Index.IsAffectedByEvaluation(evaluatedValue) || Value.IsAffectedByEvaluation(evaluatedValue);
    } 
}