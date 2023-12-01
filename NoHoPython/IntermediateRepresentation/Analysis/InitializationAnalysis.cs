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
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue);
       
        //analyzes code within message receivers (record methods) that aren't constructors
        public void NonConstructorPropertyAnalysis();

        //analyzes code outside of record methods
        public void NonMessageReceiverAnalysis();
    }

    partial interface IRStatement
    {
        //analyzes property initialization within a constructor
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration);

        //analyzes code within message receivers (record methods) that aren't constructors
        public void NonConstructorPropertyAnalysis();

        //analyzes code outside of record methods
        public void NonMessageReceiverAnalysis();
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
        public void NonMessageReceiverAnalysis() => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
        public void NonMessageReceiverAnalysis() => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
        public void NonMessageReceiverAnalysis() => throw new InvalidOperationException();
    }

    partial class ForeignCDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
        public void NonMessageReceiverAnalysis() => throw new InvalidOperationException();
    }

    partial class ForeignCProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() { }
        public void NonMessageReceiverAnalysis() { }
    }

    partial class CSymbolDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() { }
        public void NonMessageReceiverAnalysis() { }
    }

    partial class CodeBlock
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Statements.ForEach((statement) => statement.AnalyzePropertyInitialization(initializedProperties, recordDeclaration));

        public void NonConstructorPropertyAnalysis() => Statements.ForEach((statement) => statement.NonConstructorPropertyAnalysis());

        public void NonMessageReceiverAnalysis() => Statements.ForEach((statement) => statement.NonMessageReceiverAnalysis());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    partial class LoopStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() { }
        public void NonMessageReceiverAnalysis() { }
    }

    partial class AssertStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.AnalyzePropertyInitialization(initializedProperties,recordDeclaration, true);
        public void NonConstructorPropertyAnalysis() => Condition.NonConstructorPropertyAnalysis();
        public void NonMessageReceiverAnalysis() => Condition.NonMessageReceiverAnalysis();
    }

    partial class IfBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() => IfTrueBlock.NonConstructorPropertyAnalysis();
        public void NonMessageReceiverAnalysis() => IfTrueBlock.NonMessageReceiverAnalysis();
    }

    partial class IfElseBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new(initializedProperties);
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new(initializedProperties);

            Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            IfTrueBlock.AnalyzePropertyInitialization(ifTrueInitialized, recordDeclaration);
            IfFalseBlock.AnalyzePropertyInitialization(ifFalseInitialized, recordDeclaration);

            foreach(RecordDeclaration.RecordProperty property in initializedProperties)
            {
                ifTrueInitialized.Remove(property);
                ifFalseInitialized.Remove(property);
            }

            foreach (RecordDeclaration.RecordProperty initializedProperty in ifTrueInitialized)
                if (ifFalseInitialized.Contains(initializedProperty))
                    initializedProperties.Add(initializedProperty);
        }

        public void NonConstructorPropertyAnalysis()
        {
            Condition.NonConstructorPropertyAnalysis();
            IfTrueBlock.NonConstructorPropertyAnalysis();
            IfFalseBlock.NonConstructorPropertyAnalysis();
        }

        public void NonMessageReceiverAnalysis()
        {
            Condition.NonMessageReceiverAnalysis();
            IfTrueBlock.NonMessageReceiverAnalysis();
            IfFalseBlock.NonMessageReceiverAnalysis();
        }
    }

    partial class WhileBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis()
        {
            Condition.NonConstructorPropertyAnalysis();
            WhileTrueBlock.NonConstructorPropertyAnalysis();
        }

        public void NonMessageReceiverAnalysis()
        {
            Condition.NonMessageReceiverAnalysis();
            WhileTrueBlock.NonMessageReceiverAnalysis();
        }
    }

    partial class IterationForLoop
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            IteratorVariableDeclaration.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            UpperBound.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void NonConstructorPropertyAnalysis()
        {
            IteratorVariableDeclaration.NonConstructorPropertyAnalysis();
            UpperBound.NonConstructorPropertyAnalysis();
            IterationBlock.NonConstructorPropertyAnalysis();
        }

        public void NonMessageReceiverAnalysis()
        {
            IteratorVariableDeclaration.NonMessageReceiverAnalysis();
            UpperBound.NonMessageReceiverAnalysis();
            IterationBlock.NonMessageReceiverAnalysis();
        }
    }

    partial class MatchStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            if (!IsExhaustive)
                return;

            List<SortedSet<RecordDeclaration.RecordProperty>> handlerInitialized = new(MatchHandlers.Count);

            if (DefaultHandler != null)
            {
                SortedSet<RecordDeclaration.RecordProperty> defaultInitialized = new(initializedProperties);
                DefaultHandler.AnalyzePropertyInitialization(defaultInitialized, recordDeclaration);
                handlerInitialized.Add(defaultInitialized);
            }

            foreach (MatchHandler handler in MatchHandlers)
            {
                SortedSet<RecordDeclaration.RecordProperty> handlerInitted = new(initializedProperties);
                handler.ToExecute.AnalyzePropertyInitialization(handlerInitted, recordDeclaration);
                handlerInitialized.Add(handlerInitted);
            }
            if (handlerInitialized.Count > 0)
            {
                foreach(SortedSet<RecordDeclaration.RecordProperty> s in handlerInitialized)
                {
                    foreach (RecordDeclaration.RecordProperty prop in initializedProperties)
                        s.Remove(prop);
                }

                SortedSet<RecordDeclaration.RecordProperty> commonInit = handlerInitialized[0];
                foreach (RecordDeclaration.RecordProperty common in commonInit)
                {
                    bool initFlag = true;
                    for (int i = 1; i < handlerInitialized.Count; i++)
                        if (!handlerInitialized[i].Contains(common))
                        {
                            initFlag = false;
                            break;
                        }
                    if (initFlag)
                        initializedProperties.Add(common);
                }
            }
        }

        public void NonConstructorPropertyAnalysis()
        {
            DefaultHandler?.NonConstructorPropertyAnalysis();
            MatchHandlers.ForEach((handler) => handler.ToExecute.NonConstructorPropertyAnalysis());
        }

        public void NonMessageReceiverAnalysis()
        {
            DefaultHandler?.NonMessageReceiverAnalysis();
            MatchHandlers.ForEach((handler) => handler.ToExecute.NonMessageReceiverAnalysis());
        }
    }

    partial class ReturnStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ToReturn.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => ToReturn.NonConstructorPropertyAnalysis();

        public void NonMessageReceiverAnalysis() => ToReturn.NonMessageReceiverAnalysis();
    }

    partial class AbortStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AbortMessage?.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => AbortMessage?.NonConstructorPropertyAnalysis();

        public void NonMessageReceiverAnalysis() => AbortMessage?.NonMessageReceiverAnalysis();
    }

    partial class MemoryDestroy
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Address.NonConstructorPropertyAnalysis();

        public void NonMessageReceiverAnalysis() => Address.NonMessageReceiverAnalysis();
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class AllocArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Length.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            ProtoValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void NonConstructorPropertyAnalysis()
        {
            Length.NonConstructorPropertyAnalysis();
            ProtoValue.NonConstructorPropertyAnalysis();
        }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis()
        {
            Length.NonMessageReceiverAnalysis();
            ProtoValue.NonMessageReceiverAnalysis();
        }
    }

    partial class AllocMemorySpan
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => ProtoValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => ProtoValue.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => ProtoValue.NonMessageReceiverAnalysis();
    }

    partial class ArrayLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Elements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true));

        public void NonConstructorPropertyAnalysis() => Elements.ForEach((element) => element.NonConstructorPropertyAnalysis());

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Elements.ForEach((element) => element.NonConstructorPropertyAnalysis());
    }

    partial class TupleLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Elements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true));

        public void NonConstructorPropertyAnalysis() => Elements.ForEach((element) => element.NonConstructorPropertyAnalysis());

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Elements.ForEach((element) => element.NonMessageReceiverAnalysis());
    }

    partial class MarshalIntoLowerTuple
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true); 
        
        public void NonConstructorPropertyAnalysis() => Value.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Value.NonMessageReceiverAnalysis();
    }

    partial class AnonymizeProcedure
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class ProcedureCall
    {
        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Arguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true));
            AnalyzeReadonlyCall();
        }

        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public virtual void NonConstructorPropertyAnalysis()
        {
            Arguments.ForEach((arg) => arg.NonConstructorPropertyAnalysis());
            AnalyzeReadonlyCall();
        }

        protected virtual void AnalyzeReadonlyCall()
        {
            if (FunctionPurity == Purity.Pure)
                return;

            foreach (IRValue argument in Arguments)
                if (argument.IsReadOnly && IType.HasChildren(argument.Type))
                    throw new CannotMutateReadonlyValue(argument, ErrorReportedElement);
        }

        public bool IsReadOnly => false;

        public virtual void NonMessageReceiverAnalysis() 
        {
            Arguments.ForEach((arg) => arg.NonMessageReceiverAnalysis());
            AnalyzeReadonlyCall();
        } 
    }

    partial class AnonymousProcedureCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            ProcedureValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, isUsingValue);
            if(!recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }

        public override void NonConstructorPropertyAnalysis()
        {
            ProcedureValue.NonConstructorPropertyAnalysis();
            base.NonConstructorPropertyAnalysis();
        }

        public override void NonMessageReceiverAnalysis()
        {
            ProcedureValue.NonMessageReceiverAnalysis();
            base.NonMessageReceiverAnalysis();
        }
    }

    partial class OptimizedRecordMessageCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            base.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, isUsingValue);
        }

        public override void NonConstructorPropertyAnalysis()
        {
            Record.NonConstructorPropertyAnalysis();
            base.NonConstructorPropertyAnalysis();
        }

        public override void NonMessageReceiverAnalysis()
        {
            Record.NonMessageReceiverAnalysis();
            base.NonMessageReceiverAnalysis();
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
            List<Variable> parameters = ((RecordType)Record.Type).RecordPrototype.GetMessageReceiver(Property.Name).Parameters;
            for (int i = 1; i < Arguments.Count; i++)
                if (!parameters[i - 1].IsReadOnly && Arguments[i].IsReadOnly && IType.HasChildren(Arguments[i].Type))
                    throw new CannotMutateReadonlyValue(Arguments[i], ErrorReportedElement);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        }
    }

    partial class LinkedProcedureCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
            {
                if(variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                    throw new CannotUseUninitializedSelf(ErrorReportedElement);
            }
            base.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, isUsingValue);
        }

        protected override void AnalyzeReadonlyCall()
        {
            if (FunctionPurity == Purity.Pure)
                return;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            for(int i = 0; i < Arguments.Count; i++)
                if (!Procedure.ProcedureDeclaration.Parameters[i].IsReadOnly && Arguments[i].IsReadOnly && IType.HasChildren(Arguments[i].Type))
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
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Input.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Input.NonMessageReceiverAnalysis();
    }

    partial class HandleCast
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Input.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Input.NonMessageReceiverAnalysis();
    }

    partial class ArrayOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => ArrayValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => ArrayValue.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => ArrayValue.NonMessageReceiverAnalysis();
    }

    partial class SizeofOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class MemorySet
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

            if (Address.IsReadOnly)
                throw new CannotMutateReadonlyValue(Address, ErrorReportedElement);
        }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis()
        {
            Address.NonConstructorPropertyAnalysis();
            Index.NonConstructorPropertyAnalysis();
            Value.NonConstructorPropertyAnalysis();

            if (Address.IsReadOnly)
                throw new CannotMutateReadonlyValue(Address, ErrorReportedElement);
        }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis()
        {
            Address.NonMessageReceiverAnalysis();
            Index.NonMessageReceiverAnalysis();
            Value.NonMessageReceiverAnalysis();

            if (Address.IsReadOnly)
                throw new CannotMutateReadonlyValue(Address, ErrorReportedElement);
        }
    }

    partial class MarshalHandleIntoArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Length.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void NonConstructorPropertyAnalysis()
        {
            Length.NonConstructorPropertyAnalysis();
            Address.NonConstructorPropertyAnalysis();
        }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis()
        {
            Length.NonMessageReceiverAnalysis();
            Address.NonMessageReceiverAnalysis();
        }
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Span.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Span.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Span.NonMessageReceiverAnalysis();
    }

    partial class BinaryOperator
    {
        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Left.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            Right.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void NonConstructorPropertyAnalysis()
        {
            Left.NonConstructorPropertyAnalysis();
            Right.NonConstructorPropertyAnalysis();
        }

        public virtual bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis()
        {
            Left.NonMessageReceiverAnalysis();
            Right.NonMessageReceiverAnalysis();
        }
    }

    partial class LogicalOperator
    {
        //only examine left hand only side because of short circuiting
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Left.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
    }

    partial class MemoryGet
    {
        public override bool IsReadOnly => Left.IsReadOnly;
    }

    partial class GetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Array.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void NonConstructorPropertyAnalysis()
        {
            Array.NonConstructorPropertyAnalysis();
            Index.NonConstructorPropertyAnalysis();
        }

        public bool IsReadOnly => Array.IsReadOnly;

        public void NonMessageReceiverAnalysis()
        {
            Array.NonMessageReceiverAnalysis();
            Index.NonMessageReceiverAnalysis();
        }
    }

    partial class SetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Array.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

            if (Array.IsReadOnly)
                throw new CannotMutateReadonlyValue(Array, ErrorReportedElement);
        }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis()
        {
            Array.NonConstructorPropertyAnalysis();
            Index.NonConstructorPropertyAnalysis();
            Value.NonConstructorPropertyAnalysis();

            if (Array.IsReadOnly)
                throw new CannotMutateReadonlyValue(Array, ErrorReportedElement);
        }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis()
        {
            Array.NonMessageReceiverAnalysis();
            Index.NonMessageReceiverAnalysis();
            Value.NonMessageReceiverAnalysis();

            if (Array.IsReadOnly)
                throw new CannotMutateReadonlyValue(Array, ErrorReportedElement);
        }
    }

    partial class GetPropertyValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

            if(Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf)
            {
                RecordDeclaration.RecordProperty recordProperty = (RecordDeclaration.RecordProperty)Property;
                if (!recordProperty.HasDefaultValue && !initializedProperties.Contains(recordProperty))
                    throw new CannotUseUninitializedProperty(Property, ErrorReportedElement);
            }
        }

        public void NonConstructorPropertyAnalysis() => Record.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => Record.IsReadOnly || Property.IsReadOnly;

        public void NonMessageReceiverAnalysis() => Record.NonMessageReceiverAnalysis();
    }

    partial class SetPropertyValue
    {
        public bool IsInitializingProperty { get; private set; }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

            if (!Property.HasDefaultValue && Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf && !initializedProperties.Contains(Property))
            {
                initializedProperties.Add(Property);
                IsInitializingProperty = true;
            }
            else if (Property.IsReadOnly)
                throw new CannotMutateReadonlyProperty(Property, ErrorReportedElement);
            else if (Record.IsReadOnly)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);
        }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis()
        {
            Record.NonConstructorPropertyAnalysis();

            if (IsReadOnly)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);
        }

        public bool IsReadOnly => Property.IsReadOnly || Record.IsReadOnly;

        public void NonMessageReceiverAnalysis()
        {
            Record.NonMessageReceiverAnalysis();

            if (IsReadOnly)
                throw new CannotMutateReadonlyValue(Record, ErrorReportedElement);
        }
    }

    partial class MarshalIntoEnum
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Value.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Value.NonMessageReceiverAnalysis();
    }

    partial class UnwrapEnumValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => EnumValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => EnumValue.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => EnumValue.NonMessageReceiverAnalysis();
    }

    partial class CheckEnumOption
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => EnumValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => EnumValue.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => EnumValue.NonMessageReceiverAnalysis();
    }

    partial class MarshalIntoInterface
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Value.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => Value.NonMessageReceiverAnalysis();
    }

    partial class IfElseValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new(initializedProperties);
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new(initializedProperties);

            Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            IfTrueValue.AnalyzePropertyInitialization(ifTrueInitialized, recordDeclaration, isUsingValue);
            IfTrueValue.AnalyzePropertyInitialization(ifFalseInitialized, recordDeclaration, isUsingValue);

            foreach (RecordDeclaration.RecordProperty property in initializedProperties)
            {
                ifTrueInitialized.Remove(property);
                ifFalseInitialized.Remove(property);
            }

            foreach (RecordDeclaration.RecordProperty initializedProperty in ifTrueInitialized)
                if (ifFalseInitialized.Contains(initializedProperty))
                    initializedProperties.Add(initializedProperty);
        }

        public void NonConstructorPropertyAnalysis()
        {
            Condition.NonConstructorPropertyAnalysis();
            IfTrueValue.NonConstructorPropertyAnalysis();
            IfFalseValue.NonConstructorPropertyAnalysis();
        }

        public bool IsReadOnly => IfTrueValue.IsReadOnly || IfFalseValue.IsReadOnly;

        public void NonMessageReceiverAnalysis()
        {
            Condition.NonConstructorPropertyAnalysis();
            IfTrueValue.NonConstructorPropertyAnalysis();
            IfFalseValue.NonConstructorPropertyAnalysis();
        }
    }

    partial class VariableDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => InitialValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis() => InitialValue.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => InitialValue.NonMessageReceiverAnalysis();
    }

    partial class SetVariable
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => SetValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis() => SetValue.NonConstructorPropertyAnalysis();

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() => SetValue.NonMessageReceiverAnalysis();
    }

    partial class VariableReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) 
        {
            if (isUsingValue && Variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => IsConstant;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class CSymbolReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => true;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class CharacterLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class DecimalLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class IntegerLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class TrueLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class FalseLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class NullPointerLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class StaticCStringLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }

    partial class EmptyTypeLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }

        public bool IsReadOnly => false;

        public void NonMessageReceiverAnalysis() { }
    }
}