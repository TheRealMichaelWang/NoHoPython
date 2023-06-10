using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue);
        public void NonConstructorPropertyAnalysis();
    }

    partial interface IRStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration);
        public void NonConstructorPropertyAnalysis();
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
        public void NonConstructorPropertyAnalysis() => throw new InvalidOperationException();
    }

    partial class ProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class ForeignCProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() { }
    }

    partial class CSymbolDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() { }
    }

    partial class CodeBlock
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public void CodeBlockAnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Statements.ForEach((statement) => statement.AnalyzePropertyInitialization(initializedProperties, recordDeclaration));

        public void NonConstructorPropertyAnalysis() => Statements.ForEach((statement) => statement.NonConstructorPropertyAnalysis());
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    partial class LoopStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() { }
    }

    partial class AssertStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.AnalyzePropertyInitialization(initializedProperties,recordDeclaration, true);
        public void NonConstructorPropertyAnalysis() => Condition.NonConstructorPropertyAnalysis();
    }

    partial class IfBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
        public void NonConstructorPropertyAnalysis() => IfTrueBlock.NonConstructorPropertyAnalysis();
    }

    partial class IfElseBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new();
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new();

            Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            IfTrueBlock.CodeBlockAnalyzePropertyInitialization(ifTrueInitialized, recordDeclaration);
            IfFalseBlock.CodeBlockAnalyzePropertyInitialization(ifFalseInitialized, recordDeclaration);

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
    }

    partial class WhileBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis()
        {
            Condition.NonConstructorPropertyAnalysis();
            WhileTrueBlock.NonConstructorPropertyAnalysis();
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
    }

    partial class MatchStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            List<SortedSet<RecordDeclaration.RecordProperty>> handlerInitialized = new(MatchHandlers.Count);
            foreach(MatchHandler handler in MatchHandlers)
            {
                SortedSet<RecordDeclaration.RecordProperty> handlerInitted = new();
                handler.ToExecute.CodeBlockAnalyzePropertyInitialization(handlerInitted, recordDeclaration);
                handlerInitialized.Add(handlerInitted);
            }
            if(handlerInitialized.Count > 0)
            {
                SortedSet<RecordDeclaration.RecordProperty> commonInit = handlerInitialized[0];
                foreach(RecordDeclaration.RecordProperty common in commonInit)
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

        public void NonConstructorPropertyAnalysis() => MatchHandlers.ForEach((handler) => handler.ToExecute.NonConstructorPropertyAnalysis());
    }

    partial class ReturnStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ToReturn.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => ToReturn.NonConstructorPropertyAnalysis();
    }

    partial class AbortStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AbortMessage?.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => AbortMessage?.NonConstructorPropertyAnalysis();
    }

    partial class MemoryDestroy
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Address.NonConstructorPropertyAnalysis();
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
    }

    partial class AllocMemorySpan
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => ProtoValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => ProtoValue.NonConstructorPropertyAnalysis();
    }

    partial class ArrayLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Elements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true));

        public void NonConstructorPropertyAnalysis() => Elements.ForEach((element) => element.NonConstructorPropertyAnalysis());
    }

    partial class TupleLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => TupleElements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true));

        public void NonConstructorPropertyAnalysis() => TupleElements.ForEach((element) => element.NonConstructorPropertyAnalysis());
    }

    partial class MarshalIntoLowerTuple
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true); 
        
        public void NonConstructorPropertyAnalysis() => Value.NonConstructorPropertyAnalysis();
    }

    partial class InterpolatedString
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsed) => InterpolatedValues.ForEach((value) =>
        {
            if (value is IRValue irValue)
                irValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        });

        public void NonConstructorPropertyAnalysis() => InterpolatedValues.ForEach((value) =>
        {
            if (value is IRValue irValue)
                irValue.NonConstructorPropertyAnalysis();
        });
    }

    partial class AnonymizeProcedure
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class ProcedureCall
    {
        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Arguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true));

        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis() => Arguments.ForEach((arg) => arg.NonConstructorPropertyAnalysis());
    }

    partial class AnonymousProcedureCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            if(!recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }
    }

    partial class OptimizedRecordMessageCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            base.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, isUsingValue);
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
    }

    partial class ArithmeticCast
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Input.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Input.NonConstructorPropertyAnalysis();
    }

    partial class ArrayOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => ArrayValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => ArrayValue.NonConstructorPropertyAnalysis();
    }

    partial class SizeofOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class MemorySet
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis()
        {
            Address.NonConstructorPropertyAnalysis();
            Index.NonConstructorPropertyAnalysis();
            Value.NonConstructorPropertyAnalysis();
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
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Span.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Span.NonConstructorPropertyAnalysis();
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
    }

    partial class LogicalOperator
    {
        //only examine left hand only side because of short circuiting
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Left.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
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
    }

    partial class SetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Array.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis()
        {
            Array.NonConstructorPropertyAnalysis();
            Index.NonConstructorPropertyAnalysis();
            Value.NonConstructorPropertyAnalysis();
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
    }

    partial class SetPropertyValue
    {
        public bool IsInitializingProperty { get; private set; }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

            if (!Property.HasDefaultValue && Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf)
            {
                initializedProperties.Add(Property);
                IsInitializingProperty = true;
            }
            else if (Property.IsReadOnly)
                throw new CannotMutateReadonlyPropertyException(Property, ErrorReportedElement);
        }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis()
        {
            if (Property.IsReadOnly)
                throw new CannotMutateReadonlyPropertyException(Property, ErrorReportedElement);
        }
    }

    partial class MarshalIntoEnum
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Value.NonConstructorPropertyAnalysis();
    }

    partial class UnwrapEnumValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => EnumValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => EnumValue.NonConstructorPropertyAnalysis();
    }

    partial class CheckEnumOption
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => EnumValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => EnumValue.NonConstructorPropertyAnalysis();
    }

    partial class MarshalIntoInterface
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void NonConstructorPropertyAnalysis() => Value.NonConstructorPropertyAnalysis();
    }

    partial class IfElseValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new();
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new();

            Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
            IfTrueValue.AnalyzePropertyInitialization(ifTrueInitialized, recordDeclaration, isUsingValue);
            IfTrueValue.AnalyzePropertyInitialization(ifFalseInitialized, recordDeclaration, isUsingValue);

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
    }

    partial class VariableDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => InitialValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis() => InitialValue.NonConstructorPropertyAnalysis();
    }

    partial class SetVariable
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) => SetValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration, true);
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AnalyzePropertyInitialization(initializedProperties, recordDeclaration, false);

        public void NonConstructorPropertyAnalysis() => SetValue.NonConstructorPropertyAnalysis();
    }

    partial class VariableReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) 
        {
            if (isUsingValue && Variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class CSymbolReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class CharacterLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class DecimalLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class IntegerLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class TrueLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class FalseLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class NullPointerLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class StaticCStringLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }

    partial class EmptyTypeLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration, bool isUsingValue) { }

        public void NonConstructorPropertyAnalysis() { }
    }
}