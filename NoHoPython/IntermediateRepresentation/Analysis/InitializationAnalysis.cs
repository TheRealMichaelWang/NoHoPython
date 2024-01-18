using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public bool IsReadOnly { get; }

        //analyzes code, and property initialization, within a constructor
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue);
       
        //analyzes code within message receivers (record methods) that aren't constructors
        public void MessageReceiverMutabilityAnalysis();

        //analyzes code outside of record methods
        public void FunctionMutabilityAnalysis();
    }

    partial interface IRStatement
    {
        //analyzes property initialization within a constructor
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration);

        //analyzes code within message receivers (record methods) that aren't constructors
        public void MessageReceiverMutabilityAnalysis();

        //analyzes code outside of record methods
        public void FunctionMutabilityAnalysis();
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class AllCodePathsMustInitializeProperty : IRGenerationError
    {
        public AllCodePathsMustInitializeProperty(Syntax.IAstElement errorReportedElement, RecordDeclaration.RecordProperty property) : base(errorReportedElement, $"Property {property.Type.TypeName} {property.Name} must be initialized at every code path") 
        {

        }
    }

    partial class EnumDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void MessageReceiverMutabilityAnalysis() => throw new InvalidOperationException();
        public void FunctionMutabilityAnalysis() => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void MessageReceiverMutabilityAnalysis() => throw new InvalidOperationException();
        public void FunctionMutabilityAnalysis() => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void MessageReceiverMutabilityAnalysis() => throw new InvalidOperationException();
        public void FunctionMutabilityAnalysis() => throw new InvalidOperationException();
    }

    partial class ForeignCDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void MessageReceiverMutabilityAnalysis() => throw new InvalidOperationException();
        public void FunctionMutabilityAnalysis() => throw new InvalidOperationException();
    }

    partial class ForeignCProcedureDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void MessageReceiverMutabilityAnalysis() { }
        public void FunctionMutabilityAnalysis() { }
    }

    partial class CSymbolDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void MessageReceiverMutabilityAnalysis() { }
        public void FunctionMutabilityAnalysis() { }
    }

    partial class CodeBlock
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Statements.ForEach((statement) => statement.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration));

        public void MessageReceiverMutabilityAnalysis() => Statements.ForEach((statement) => statement.MessageReceiverMutabilityAnalysis());

        public void FunctionMutabilityAnalysis() => Statements.ForEach((statement) => statement.FunctionMutabilityAnalysis());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    partial class LoopStatement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void MessageReceiverMutabilityAnalysis() { }
        public void FunctionMutabilityAnalysis() { }
    }

    partial class AssertStatement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.ConstructorMutabilityAnalysis(initializedProperties,recordDeclaration, true);
        public void MessageReceiverMutabilityAnalysis() => Condition.MessageReceiverMutabilityAnalysis();
        public void FunctionMutabilityAnalysis() => Condition.FunctionMutabilityAnalysis();
    }

    partial class IfBlock
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void MessageReceiverMutabilityAnalysis() => IfTrueBlock.MessageReceiverMutabilityAnalysis();
        public void FunctionMutabilityAnalysis() => IfTrueBlock.FunctionMutabilityAnalysis();
    }

    partial class IfElseBlock
    {
        public static void AnalyzeBranchedInitializations(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, Syntax.IAstElement errorReportedElement, params SortedSet<RecordDeclaration.RecordProperty>[] branchInitializedProperties)
        {
            SortedSet<RecordDeclaration.RecordProperty> common = branchInitializedProperties.First();
            for(int i = 1; i < branchInitializedProperties.Length; i++)
            {
                foreach (RecordDeclaration.RecordProperty property in common) {
                    if (!branchInitializedProperties[i].Contains(property))
                        throw new AllCodePathsMustInitializeProperty(errorReportedElement, property);
                }
                foreach (RecordDeclaration.RecordProperty property in branchInitializedProperties[i])
                {
                    if (!common.Contains(property))
                        throw new AllCodePathsMustInitializeProperty(errorReportedElement, property);
                }
            }
            foreach (RecordDeclaration.RecordProperty property in common)
                initializedProperties.Add(property);
        }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new(initializedProperties);
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new(initializedProperties);
            Condition.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            IfTrueBlock.ConstructorMutabilityAnalysis(ifTrueInitialized, recordDeclaration);
            IfFalseBlock.ConstructorMutabilityAnalysis(ifFalseInitialized, recordDeclaration);
            AnalyzeBranchedInitializations(initializedProperties, ErrorReportedElement, ifTrueInitialized, ifFalseInitialized);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            Condition.MessageReceiverMutabilityAnalysis();
            IfTrueBlock.MessageReceiverMutabilityAnalysis();
            IfFalseBlock.MessageReceiverMutabilityAnalysis();
        }

        public void FunctionMutabilityAnalysis()
        {
            Condition.FunctionMutabilityAnalysis();
            IfTrueBlock.FunctionMutabilityAnalysis();
            IfFalseBlock.FunctionMutabilityAnalysis();
        }
    }

    partial class WhileBlock
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis()
        {
            Condition.MessageReceiverMutabilityAnalysis();
            WhileTrueBlock.MessageReceiverMutabilityAnalysis();
        }

        public void FunctionMutabilityAnalysis()
        {
            Condition.FunctionMutabilityAnalysis();
            WhileTrueBlock.FunctionMutabilityAnalysis();
        }
    }

    partial class IterationForLoop
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            IteratorVariableDeclaration.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            UpperBound.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            IteratorVariableDeclaration.MessageReceiverMutabilityAnalysis();
            UpperBound.MessageReceiverMutabilityAnalysis();
            IterationBlock.MessageReceiverMutabilityAnalysis();
        }

        public void FunctionMutabilityAnalysis()
        {
            IteratorVariableDeclaration.FunctionMutabilityAnalysis();
            UpperBound.FunctionMutabilityAnalysis();
            IterationBlock.FunctionMutabilityAnalysis();
        }
    }

    partial class MatchStatement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            if (!IsExhaustive)
                return;

            MatchValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

            List<SortedSet<RecordDeclaration.RecordProperty>> handlerInitialized = new(MatchHandlers.Count);
            if (DefaultHandler != null)
            {
                SortedSet<RecordDeclaration.RecordProperty> defaultInitialized = new(initializedProperties);
                DefaultHandler.ConstructorMutabilityAnalysis(defaultInitialized, recordDeclaration);
                handlerInitialized.Add(defaultInitialized);
            }
            foreach (MatchHandler handler in MatchHandlers)
            {
                SortedSet<RecordDeclaration.RecordProperty> handlerInitted = new(initializedProperties);
                handler.ToExecute.ConstructorMutabilityAnalysis(handlerInitted, recordDeclaration);
                handlerInitialized.Add(handlerInitted);
            }

            IfElseBlock.AnalyzeBranchedInitializations(initializedProperties, ErrorReportedElement, handlerInitialized.ToArray());
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            DefaultHandler?.MessageReceiverMutabilityAnalysis();
            MatchHandlers.ForEach((handler) => handler.ToExecute.MessageReceiverMutabilityAnalysis());
        }

        public void FunctionMutabilityAnalysis()
        {
            DefaultHandler?.FunctionMutabilityAnalysis();
            MatchHandlers.ForEach((handler) => handler.ToExecute.FunctionMutabilityAnalysis());
        }
    }

    partial class ReturnStatement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ToReturn.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => ToReturn.MessageReceiverMutabilityAnalysis();

        public void FunctionMutabilityAnalysis() => ToReturn.FunctionMutabilityAnalysis();
    }

    partial class AbortStatement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AbortMessage?.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => AbortMessage?.MessageReceiverMutabilityAnalysis();

        public void FunctionMutabilityAnalysis() => AbortMessage?.FunctionMutabilityAnalysis();
    }

    partial class MemoryDestroy
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Address.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Address.MessageReceiverMutabilityAnalysis();

        public void FunctionMutabilityAnalysis() => Address.FunctionMutabilityAnalysis();
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class AllocArray
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Length.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            ProtoValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            Length.MessageReceiverMutabilityAnalysis();
            ProtoValue.MessageReceiverMutabilityAnalysis();
        }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            Length.FunctionMutabilityAnalysis();
            ProtoValue.FunctionMutabilityAnalysis();
        }
    }

    partial class AllocMemorySpan
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => ProtoValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => ProtoValue.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => ProtoValue.FunctionMutabilityAnalysis();
    }

    partial class ArrayLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Elements.ForEach((element) => element.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true));

        public void MessageReceiverMutabilityAnalysis() => Elements.ForEach((element) => element.MessageReceiverMutabilityAnalysis());

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Elements.ForEach((element) => element.MessageReceiverMutabilityAnalysis());
    }

    partial class TupleLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Elements.ForEach((element) => element.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true));

        public void MessageReceiverMutabilityAnalysis() => Elements.ForEach((element) => element.MessageReceiverMutabilityAnalysis());

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Elements.ForEach((element) => element.FunctionMutabilityAnalysis());
    }

    partial class ReferenceLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, isUsingValue);

        public void MessageReceiverMutabilityAnalysis() => Input.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Input.FunctionMutabilityAnalysis();
    }

    partial class MarshalIntoLowerTuple
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true); 
        
        public void MessageReceiverMutabilityAnalysis() => Value.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Value.FunctionMutabilityAnalysis();
    }

    partial class AnonymizeProcedure
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class ProcedureCall
    {
        public virtual void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Arguments.ForEach((arg) => arg.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true));
            AnalyzeReadonlyCall();
        }

        public virtual void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public virtual void MessageReceiverMutabilityAnalysis()
        {
            Arguments.ForEach((arg) => arg.MessageReceiverMutabilityAnalysis());
            AnalyzeReadonlyCall();
        }

        protected virtual void AnalyzeReadonlyCall()
        {
            if (FunctionPurity == Purity.Pure)
                return;

            foreach (IRValue argument in Arguments)
                if (argument.IsReadOnly && argument.Type.HasMutableChildren)
                    throw new CannotMutateReadonlyValue(argument, ErrorReportedElement);
        }

        public virtual bool IsReadOnly => Type.IsReferenceType && Type.HasMutableChildren && !(FunctionPurity <= Purity.OnlyAffectsArguments && !Arguments.Any(argument => (argument.Type.IsCompatibleWith(Type) || argument.Type.ContainsType(Type)) && argument.IsReadOnly));
        public virtual bool CanMutateArguments => Type.IsReferenceType && Type.HasMutableChildren && !(FunctionPurity <= Purity.OnlyAffectsArguments && Arguments.Any(argument => (argument.Type.IsCompatibleWith(Type) || argument.Type.ContainsType(Type)) && !argument.IsReadOnly));

        public virtual void FunctionMutabilityAnalysis() 
        {
            Arguments.ForEach((arg) => arg.FunctionMutabilityAnalysis());
            AnalyzeReadonlyCall();
        } 
    }

    partial class AnonymousProcedureCall
    {
        public override void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            ProcedureValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, isUsingValue);
            if(!recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }

        public override void MessageReceiverMutabilityAnalysis()
        {
            ProcedureValue.MessageReceiverMutabilityAnalysis();
            base.MessageReceiverMutabilityAnalysis();
        }

        public override void FunctionMutabilityAnalysis()
        {
            ProcedureValue.FunctionMutabilityAnalysis();
            base.FunctionMutabilityAnalysis();
        }
    }

    partial class OptimizedRecordMessageCall
    {
        public override void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            base.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, isUsingValue);
        }

        public override void MessageReceiverMutabilityAnalysis()
        {
            Record.MessageReceiverMutabilityAnalysis();
            base.MessageReceiverMutabilityAnalysis();
        }

        public override void FunctionMutabilityAnalysis()
        {
            Record.FunctionMutabilityAnalysis();
            base.FunctionMutabilityAnalysis();
        }

        protected override void AnalyzeReadonlyCall()
        {
            if (FunctionPurity == Purity.Pure)
                return;

            if (Record.IsReadOnly && FunctionPurity >= Purity.OnlyAffectsArgumentsAndCaptured)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            Debug.Assert(Arguments.Count >= 1);
            List<Variable> parameters = ((RecordType)Record.Type).RecordPrototype.GetMessageReceiver(Property.Name).Procedure.ProcedureDeclaration.Parameters;
            for (int i = 1; i < Arguments.Count; i++)
                if (!parameters[i - 1].IsReadOnly && Arguments[i].IsReadOnly && Arguments[i].Type.HasMutableChildren)
                    throw new CannotMutateReadonlyValue(Arguments[i], ErrorReportedElement);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }
    }

    partial class LinkedProcedureCall
    {
        public override void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
            {
                if(variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                    throw new CannotUseUninitializedSelf(ErrorReportedElement);
            }
            base.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, isUsingValue);
        }

        protected override void AnalyzeReadonlyCall()
        {
            if (FunctionPurity == Purity.Pure)
                return;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            for(int i = 0; i < Arguments.Count; i++)
                if (!Procedure.ProcedureDeclaration.Parameters[i].IsReadOnly && Arguments[i].IsReadOnly && Arguments[i].Type.HasMutableChildren)
                    throw new CannotMutateReadonlyValue(Arguments[i], ErrorReportedElement);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (FunctionPurity <= Purity.OnlyAffectsArgumentsAndCaptured)
            {
                if (Procedure.ProcedureDeclaration.CapturedVariables.Count > 0)
                {
                    Debug.Assert(parentProcedure != null);

                    foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
                    {
                        if (variable.IsRecordSelf && variable.ParentProcedure == Procedure.ProcedureDeclaration)
                        {
                            if (parentProcedure.Purity <= Purity.OnlyAffectsArguments)
                                throw new CannotMutateVaraible(variable, true, ErrorReportedElement);
                        }
                        else if (parentProcedure.SanitizeVariable(variable, false, ErrorReportedElement).Item2)
                            throw new CannotMutateVaraible(variable, true, ErrorReportedElement);
                    }
                }
            }
        }
    }

    partial class ArithmeticCast
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Input.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Input.FunctionMutabilityAnalysis();
    }

    partial class HandleCast
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Input.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => Input.IsReadOnly;

        public void FunctionMutabilityAnalysis() => Input.FunctionMutabilityAnalysis();
    }

    partial class AutoCast
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Input.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => Input.IsReadOnly;

        public void FunctionMutabilityAnalysis() => Input.FunctionMutabilityAnalysis();
    }

    partial class ArrayOperator
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => ArrayValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => ArrayValue.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => ArrayValue.FunctionMutabilityAnalysis();
    }

    partial class SizeofOperator
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class MemorySet
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Address.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
            Index.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            Value.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

            if (Address.IsReadOnly)
                throw new CannotMutateReadonlyValue(Address, ErrorReportedElement);
        }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public void MessageReceiverMutabilityAnalysis()
        {
            Address.MessageReceiverMutabilityAnalysis();
            Index.MessageReceiverMutabilityAnalysis();
            Value.MessageReceiverMutabilityAnalysis();

            if (Address.IsReadOnly)
                throw new CannotMutateReadonlyValue(Address, ErrorReportedElement);
        }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            Address.FunctionMutabilityAnalysis();
            Index.FunctionMutabilityAnalysis();
            Value.FunctionMutabilityAnalysis();

            if (Address.IsReadOnly)
                throw new CannotMutateReadonlyValue(Address, ErrorReportedElement);
        }
    }

    partial class MarshalHandleIntoArray
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Length.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            Address.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            Length.MessageReceiverMutabilityAnalysis();
            Address.MessageReceiverMutabilityAnalysis();
        }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            Length.FunctionMutabilityAnalysis();
            Address.FunctionMutabilityAnalysis();
        }
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Span.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Span.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Span.FunctionMutabilityAnalysis();
    }

    partial class BinaryOperator
    {
        public virtual void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Left.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            Right.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            Left.MessageReceiverMutabilityAnalysis();
            Right.MessageReceiverMutabilityAnalysis();
        }

        public virtual bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            Left.FunctionMutabilityAnalysis();
            Right.FunctionMutabilityAnalysis();
        }
    }

    partial class LogicalOperator
    {
        //only examine left hand only side because of short circuiting
        public override void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Left.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
    }

    partial class MemoryGet
    {
        public override bool IsReadOnly => Left.IsReadOnly;
    }

    partial class GetValueAtIndex
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Array.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
            Index.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            Array.MessageReceiverMutabilityAnalysis();
            Index.MessageReceiverMutabilityAnalysis();
        }

        public bool IsReadOnly => Array.IsReadOnly;

        public void FunctionMutabilityAnalysis()
        {
            Array.FunctionMutabilityAnalysis();
            Index.FunctionMutabilityAnalysis();
        }
    }

    partial class SetValueAtIndex
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Array.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
            Index.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            Value.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

            if (Array.IsReadOnly)
                throw new CannotMutateReadonlyValue(Array, ErrorReportedElement);
        }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public void MessageReceiverMutabilityAnalysis()
        {
            Array.MessageReceiverMutabilityAnalysis();
            Index.MessageReceiverMutabilityAnalysis();
            Value.MessageReceiverMutabilityAnalysis();

            if (Array.IsReadOnly)
                throw new CannotMutateReadonlyValue(Array, ErrorReportedElement);
            if (Value.Type.IsReferenceType && Value.IsReadOnly)
                throw new CannotMutateReadonlyValue(Value, ErrorReportedElement);
        }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            Array.FunctionMutabilityAnalysis();
            Index.FunctionMutabilityAnalysis();
            Value.FunctionMutabilityAnalysis();

            if (Array.IsReadOnly)
                throw new CannotMutateReadonlyValue(Array, ErrorReportedElement);
            if (Value.Type.IsReferenceType && Value.IsReadOnly && Value.Type.HasMutableChildren)
                throw new CannotMutateReadonlyValue(Value, ErrorReportedElement);
        }
    }

    partial class GetPropertyValue
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

            if(Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf)
            {
                RecordDeclaration.RecordProperty recordProperty = (RecordDeclaration.RecordProperty)Property;
                if (!recordProperty.HasDefaultValue && !initializedProperties.Contains(recordProperty))
                    throw new CannotUseUninitializedProperty(Property, ErrorReportedElement);
            }
        }

        public void MessageReceiverMutabilityAnalysis() => Record.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => Record.IsReadOnly || Property.IsReadOnly;

        public void FunctionMutabilityAnalysis() => Record.FunctionMutabilityAnalysis();
    }

    partial class SetPropertyValue
    {
        public bool IsInitializingProperty { get; private set; }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
            Value.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

            if (!Property.HasDefaultValue && Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf && !initializedProperties.Contains(Property))
            {
                initializedProperties.Add(Property);
                IsInitializingProperty = true;

                if (Value.Type.IsReferenceType && Value.Type.HasMutableChildren && Value.IsReadOnly != IsReadOnly)
                    throw new CannotMutateReadonlyValue(this, ErrorReportedElement);

                if (Property.IsReadOnly)
                    return;
            }
            else if (Property.IsReadOnly)
                throw new CannotMutateReadonlyProperty(Property, ErrorReportedElement);
            else if (Record.IsReadOnly)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);

            if (Value.Type.IsReferenceType && Value.IsReadOnly && Value.Type.HasMutableChildren)
                throw new CannotMutateReadonlyValue(Value, ErrorReportedElement);
        }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public void MessageReceiverMutabilityAnalysis()
        {
            Record.MessageReceiverMutabilityAnalysis();

            if (IsReadOnly)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);

            if (Value.Type.IsReferenceType && Value.IsReadOnly && Value.Type.HasMutableChildren)
                throw new CannotMutateReadonlyValue(Value, ErrorReportedElement);
        }

        public bool IsReadOnly => Property.IsReadOnly || Record.IsReadOnly;

        public void FunctionMutabilityAnalysis()
        {
            Record.FunctionMutabilityAnalysis();

            if (IsReadOnly)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);

            if (Value.Type.IsReferenceType && Value.IsReadOnly && Value.Type.HasMutableChildren)
                throw new CannotMutateReadonlyValue(Value, ErrorReportedElement);
        }
    }

    partial class ReleaseReferenceElement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
        
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            ReferenceBox.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
            if (ReferenceBox.IsReadOnly)
                throw new CannotReleaseReadonlyReferenceType(ErrorReportedElement);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            ReferenceBox.MessageReceiverMutabilityAnalysis();
            if (ReferenceBox.IsReadOnly)
                throw new CannotReleaseReadonlyReferenceType(ErrorReportedElement);
        }

        public bool IsReadOnly => ReferenceBox.IsReadOnly;

        public void FunctionMutabilityAnalysis()
        {
            ReferenceBox.FunctionMutabilityAnalysis();
            if (ReferenceBox.IsReadOnly)
                throw new CannotReleaseReadonlyReferenceType(ErrorReportedElement);
        }
    }

    partial class SetReferenceTypeElement
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public bool IsReadOnly => false;

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            ReferenceBox.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);
            NewElement.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

            if (ReferenceBox.IsReadOnly)
                throw new CannotMutateReadonlyValue(ReferenceBox, ErrorReportedElement);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            ReferenceBox.MessageReceiverMutabilityAnalysis();
            NewElement.MessageReceiverMutabilityAnalysis();

            if (ReferenceBox.IsReadOnly)
                throw new CannotMutateReadonlyValue(ReferenceBox, ErrorReportedElement);
        }

        public void FunctionMutabilityAnalysis()
        {
            ReferenceBox.FunctionMutabilityAnalysis();
            NewElement.FunctionMutabilityAnalysis();

            if (ReferenceBox.IsReadOnly)
                throw new CannotMutateReadonlyValue(ReferenceBox, ErrorReportedElement);
        }
    }

    partial class MarshalIntoEnum
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Value.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Value.FunctionMutabilityAnalysis();
    }

    partial class UnwrapEnumValue
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => EnumValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => EnumValue.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => EnumValue.FunctionMutabilityAnalysis();
    }

    partial class CheckEnumOption
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => EnumValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => EnumValue.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => EnumValue.FunctionMutabilityAnalysis();
    }

    partial class MarshalIntoInterface
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);

        public void MessageReceiverMutabilityAnalysis() => Value.MessageReceiverMutabilityAnalysis();

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() => Value.FunctionMutabilityAnalysis();
    }

    partial class IfElseValue
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new(initializedProperties);
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new(initializedProperties);

            Condition.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            IfTrueValue.ConstructorMutabilityAnalysis(ifTrueInitialized, recordDeclaration, isUsingValue);
            IfTrueValue.ConstructorMutabilityAnalysis(ifFalseInitialized, recordDeclaration, isUsingValue);

            IfElseBlock.AnalyzeBranchedInitializations(initializedProperties, ErrorReportedElement, ifTrueInitialized, ifFalseInitialized);
        }

        public void MessageReceiverMutabilityAnalysis()
        {
            Condition.MessageReceiverMutabilityAnalysis();
            IfTrueValue.MessageReceiverMutabilityAnalysis();
            IfFalseValue.MessageReceiverMutabilityAnalysis();
        }

        public bool IsReadOnly => IfTrueValue.IsReadOnly || IfFalseValue.IsReadOnly;

        public void FunctionMutabilityAnalysis()
        {
            Condition.MessageReceiverMutabilityAnalysis();
            IfTrueValue.MessageReceiverMutabilityAnalysis();
            IfFalseValue.MessageReceiverMutabilityAnalysis();
        }
    }

    partial class VariableDeclaration
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            InitialValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            if (InitialValue.Type.IsReferenceType && InitialValue.Type.HasMutableChildren && InitialValue.IsReadOnly != Variable.IsReadOnly)
                throw new CannotMutateReadonlyValue(InitialValue, ErrorReportedElement);
        }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public void MessageReceiverMutabilityAnalysis()
        {
            InitialValue.MessageReceiverMutabilityAnalysis();
            if (InitialValue.Type.IsReferenceType && InitialValue.Type.HasMutableChildren && InitialValue.IsReadOnly != Variable.IsReadOnly)
                throw new CannotMutateReadonlyValue(InitialValue, ErrorReportedElement);
        }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            InitialValue.FunctionMutabilityAnalysis();
            if (InitialValue.Type.IsReferenceType && InitialValue.Type.HasMutableChildren && InitialValue.IsReadOnly != Variable.IsReadOnly)
                throw new CannotMutateReadonlyValue(InitialValue, ErrorReportedElement);
        }
    }

    partial class SetVariable
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            SetValue.ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, true);
            if (SetValue.Type.IsReferenceType && SetValue.Type.HasMutableChildren && SetValue.IsReadOnly)
                throw new CannotMutateReadonlyValue(SetValue, ErrorReportedElement);
        }

        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ConstructorMutabilityAnalysis(initializedProperties, recordDeclaration, false);

        public void MessageReceiverMutabilityAnalysis()
        {
            SetValue.MessageReceiverMutabilityAnalysis();
            if (SetValue.Type.IsReferenceType && SetValue.Type.HasMutableChildren && SetValue.IsReadOnly)
                throw new CannotMutateReadonlyValue(SetValue, ErrorReportedElement);
        }
        
        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis()
        {
            SetValue.FunctionMutabilityAnalysis();
            if (SetValue.Type.IsReferenceType && SetValue.Type.HasMutableChildren && SetValue.IsReadOnly)
                throw new CannotMutateReadonlyValue(SetValue, ErrorReportedElement);
        }
    }

    partial class VariableReference
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) 
        {
            if (isUsingValue && Variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => IsConstant;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class CSymbolReference
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => true;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class CharacterLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class DecimalLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class IntegerLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class TrueLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class FalseLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class NullPointerLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class StaticCStringLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }

    partial class EmptyTypeLiteral
    {
        public void ConstructorMutabilityAnalysis(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void MessageReceiverMutabilityAnalysis() { }

        public bool IsReadOnly => false;

        public void FunctionMutabilityAnalysis() { }
    }
}