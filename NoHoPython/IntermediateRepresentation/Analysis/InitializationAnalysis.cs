using NoHoPython.IntermediateRepresentation.Statements;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties);
    }

    partial interface IRStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties);
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordProperty> initializedProperties) => throw new InvalidOperationException();
    }

    partial class ProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class ForeignCProcedureDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class CSymbolDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class CodeBlock
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public void CodeBlockAnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Statements.ForEach((statement) => statement.AnalyzePropertyInitialization(initializedProperties));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    partial class LoopStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class AssertStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Condition.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class IfBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class IfElseBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new();
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new();

            Condition.AnalyzePropertyInitialization(initializedProperties);
            IfTrueBlock.CodeBlockAnalyzePropertyInitialization(ifTrueInitialized);
            IfFalseBlock.CodeBlockAnalyzePropertyInitialization(ifFalseInitialized);

            foreach (RecordDeclaration.RecordProperty initializedProperty in ifTrueInitialized)
                if (ifFalseInitialized.Contains(initializedProperty))
                    initializedProperties.Add(initializedProperty);
        }
    }

    partial class WhileBlock
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Condition.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class IterationForLoop
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            IteratorVariableDeclaration.AnalyzePropertyInitialization(initializedProperties);
            UpperBound.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class MatchStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            List<SortedSet<RecordDeclaration.RecordProperty>> handlerInitialized = new(MatchHandlers.Count);
            foreach(MatchHandler handler in MatchHandlers)
            {
                SortedSet<RecordDeclaration.RecordProperty> handlerInitted = new();
                handler.ToExecute.CodeBlockAnalyzePropertyInitialization(handlerInitted);
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
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => ToReturn.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class AbortStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => AbortMessage?.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class MemoryDestroy
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Address.AnalyzePropertyInitialization(initializedProperties);
            if(Index != null)
                Index.AnalyzePropertyInitialization(initializedProperties);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class AllocArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Length.AnalyzePropertyInitialization(initializedProperties);
            ProtoValue.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class ArrayLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Elements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties));
    }

    partial class AnonymizeProcedure
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class ProcedureCall
    {
        public virtual void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Arguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties));
    }

    partial class AnonymousProcedureCall
    {
        public override void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            ProcedureValue.AnalyzePropertyInitialization(initializedProperties);
            base.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class ArithmeticCast
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Input.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class ArithmeticOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Left.AnalyzePropertyInitialization(initializedProperties);
            Right.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class ArrayOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => ArrayValue.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class SizeofOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class MemoryGet
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Address.AnalyzePropertyInitialization(initializedProperties);
            Index.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class MemorySet
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Address.AnalyzePropertyInitialization(initializedProperties);
            Index.AnalyzePropertyInitialization(initializedProperties);
            Value.AnalyzePropertyInitialization(initializedProperties);
            if (ResponsibleDestroyer != null)
                ResponsibleDestroyer.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class MarshalIntoArray
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Length.AnalyzePropertyInitialization(initializedProperties);
            Address.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class ComparativeOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Left.AnalyzePropertyInitialization(initializedProperties);
            Right.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class LogicalOperator
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Left.AnalyzePropertyInitialization(initializedProperties);
            Right.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class GetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Array.AnalyzePropertyInitialization(initializedProperties);
            Index.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class SetValueAtIndex
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Array.AnalyzePropertyInitialization(initializedProperties);
            Index.AnalyzePropertyInitialization(initializedProperties);
            Value.AnalyzePropertyInitialization(initializedProperties);
        }
    }

    partial class GetPropertyValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Record.AnalyzePropertyInitialization(initializedProperties);

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

        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            Record.AnalyzePropertyInitialization(initializedProperties);
            Value.AnalyzePropertyInitialization(initializedProperties);

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
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Value.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class MarshalIntoInterface
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Value.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class IfElseValue
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            SortedSet<RecordDeclaration.RecordProperty> ifTrueInitialized = new();
            SortedSet<RecordDeclaration.RecordProperty> ifFalseInitialized = new();

            Condition.AnalyzePropertyInitialization(initializedProperties);
            IfTrueValue.AnalyzePropertyInitialization(ifTrueInitialized);
            IfTrueValue.AnalyzePropertyInitialization(ifFalseInitialized);

            foreach (RecordDeclaration.RecordProperty initializedProperty in ifTrueInitialized)
                if (ifFalseInitialized.Contains(initializedProperty))
                    initializedProperties.Add(initializedProperty);
        }
    }

    partial class VariableDeclaration
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => InitialValue.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class SetVariable
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => SetValue.AnalyzePropertyInitialization(initializedProperties);
    }

    partial class VariableReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class CSymbolReference
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class CharacterLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class DecimalLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class IntegerLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class TrueLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class FalseLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class NothingLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }
}