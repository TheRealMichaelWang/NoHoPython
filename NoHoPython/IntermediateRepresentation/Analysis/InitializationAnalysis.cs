﻿using NoHoPython.IntermediateRepresentation.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    partial class CodeBlock
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public void CodeBlockAnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Statements.ForEach((statement) => statement.AnalyzePropertyInitialization(initializedProperties));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
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
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class ReturnStatement
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => ToReturn.AnalyzePropertyInitialization(initializedProperties);
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

    partial class AllocRecord
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => ConstructorArguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties));
    }

    partial class ArrayLiteral
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Elements.ForEach((element) => element.AnalyzePropertyInitialization(initializedProperties));
    }

    partial class AnonymizeProcedure
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) { }
    }

    partial class AnonymousProcedureCall
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties)
        {
            ProcedureValue.AnalyzePropertyInitialization(initializedProperties);
            Arguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties));
        }
    }

    partial class ForeignFunctionCall
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Arguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties));
    }

    partial class LinkedProcedureCall
    {
        public void AnalyzePropertyInitialization(SortedSet<RecordDeclaration.RecordProperty> initializedProperties) => Arguments.ForEach((arg) => arg.AnalyzePropertyInitialization(initializedProperties));
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

            if (Record is VariableReference variableReference && variableReference.Variable.IsRecordSelf)
            {
                initializedProperties.Add(Property);
                if(Property.DefaultValue == null)
                    IsInitializingProperty = true;
            }
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