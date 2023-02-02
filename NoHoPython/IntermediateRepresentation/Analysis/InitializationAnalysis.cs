using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration);
    }

    partial interface IRStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration);
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => throw new InvalidOperationException();
    }

    partial class ProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class ForeignCProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class CSymbolDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class CodeBlock
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public void CodeBlockAnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Statements.ForEach((statement) => statement.AnalyzePropertyInitialization(initializedProperties, recordDeclaration));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    partial class LoopStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class AssertStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.AnalyzePropertyInitialization(initializedProperties,recordDeclaration);
    }

    partial class IfBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class IfElseBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new();
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new();

            Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            IfTrueBlock.CodeBlockAnalyzePropertyInitialization(ifTrueInitialized, recordDeclaration);
            IfFalseBlock.CodeBlockAnalyzePropertyInitialization(ifFalseInitialized, recordDeclaration);

            foreach (RecordDeclaration.RecordProperty initializedProperty in ifTrueInitialized)
                if (ifFalseInitialized.Contains(initializedProperty))
                    initializedProperties.Add(initializedProperty);
        }
    }

    partial class WhileBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class IterationForLoop
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            IteratorVariableDeclaration.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            UpperBound.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
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
    }

    partial class ReturnStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ToReturn.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class AbortStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => AbortMessage?.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class MemoryDestroy
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            if(Index != null)
                Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class AllocArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Length.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            ProtoValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class ArrayLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Elements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties, recordDeclaration));
    }

    partial class TupleLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => TupleElements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties, recordDeclaration));
    }

    partial class InterpolatedString
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => InterpolatedValues.ForEach((value) =>
        {
            if (value is IRValue irValue)
                irValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        });
    }

    partial class AnonymizeProcedure
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class ProcedureCall
    {
        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Arguments.ForEach((arg) =>
        {
            if(arg is VariableReference variableReference && variableReference.Variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
                
            arg.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        });
    }

    partial class AnonymousProcedureCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            if(!recordDeclaration.AllPropertiesInitialized(initializedProperties))
                throw new CannotUseUninitializedSelf(ErrorReportedElement);
        }
    }

    partial class LinkedProcedureCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            foreach (Variable variable in Procedure.ProcedureDeclaration.CapturedVariables)
            {
                if(variable.IsRecordSelf && !recordDeclaration.AllPropertiesInitialized(initializedProperties))
                    throw new CannotUseUninitializedSelf(ErrorReportedElement);
            }
            base.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class ArithmeticCast
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Input.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class ArrayOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => ArrayValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class SizeofOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class MemorySet
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            if (ResponsibleDestroyer != null)
                ResponsibleDestroyer.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class MarshalIntoArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Length.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Address.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class BinaryOperator
    {
        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Left.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Right.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class LogicalOperator
    {
        //only examine left hand only side because of short circuiting
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Left.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class GetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Array.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class SetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Array.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Index.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
        }
    }

    partial class GetPropertyValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);

            if(Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf)
            {
                RecordDeclaration.RecordProperty recordProperty = (RecordDeclaration.RecordProperty)Property;
                if (recordProperty.DefaultValue == null && !initializedProperties.Contains(recordProperty))
                    throw new CannotUseUninitializedProperty(Property, ErrorReportedElement);
            }
        }
    }

    partial class SetPropertyValue
    {
        public bool IsInitializingProperty { get; private set; }

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            Record.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);

            if (Property.DefaultValue == null && Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf)
            {
                initializedProperties.Add(Property);
                IsInitializingProperty = true;
            }
            else if (Property.IsReadOnly)
                throw new CannotMutateReadonlyPropertyException(Property, ErrorReportedElement);
        }
    }

    partial class MarshalIntoEnum
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class UnwrapEnumValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => EnumValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class CheckEnumOption
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => EnumValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class MarshalIntoInterface
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => Value.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class IfElseValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new();
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new();

            Condition.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
            IfTrueValue.AnalyzePropertyInitialization(ifTrueInitialized, recordDeclaration);
            IfTrueValue.AnalyzePropertyInitialization(ifFalseInitialized, recordDeclaration);

            foreach (RecordDeclaration.RecordProperty initializedProperty in ifTrueInitialized)
                if (ifFalseInitialized.Contains(initializedProperty))
                    initializedProperties.Add(initializedProperty);
        }
    }

    partial class VariableDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => InitialValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class SetVariable
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) => SetValue.AnalyzePropertyInitialization(initializedProperties, recordDeclaration);
    }

    partial class VariableReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class CSymbolReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class CharacterLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class DecimalLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class IntegerLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class TrueLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class FalseLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }

    partial class EmptyTypeLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties, RecordDeclaration recordDeclaration) { }
    }
}