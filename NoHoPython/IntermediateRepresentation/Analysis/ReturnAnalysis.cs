﻿namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRStatement
    {
        public bool AllCodePathsReturn();
        public bool SomeCodePathsBreak();
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
        public bool CodeBlockAllCodePathsReturn()
        {
#pragma warning disable CS8602 //Statements initialized during ir generation
            foreach (IRStatement statement in Statements)
                if (statement.AllCodePathsReturn())
                    return true;
#pragma warning restore CS8602
            return false;
        }

        public bool CodeBlockSomeCodePathsBreak()
        {
#pragma warning disable CS8602 //Statements initialized during ir generation
            foreach (IRStatement statement in Statements)
                if (statement.SomeCodePathsBreak())
                    return true;
#pragma warning restore CS8602
            return false;
        }
    }

    partial class EnumDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
    }

    partial class ForeignCDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
    }

    partial class ForeignCProcedureDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class ProcedureDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class CSymbolDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class LoopStatement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => Action.Type == Syntax.Parsing.TokenType.Break;
    }

    partial class AssertStatement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class IfBlock
    {
        public bool AllCodePathsReturn() => Condition.IsTruey && IfTrueBlock.CodeBlockAllCodePathsReturn();
        public bool SomeCodePathsBreak() => IfTrueBlock.CodeBlockSomeCodePathsBreak();
    }

    partial class IfElseBlock
    {
        public bool AllCodePathsReturn()
        {
            if (Condition.IsTruey)
                return IfTrueBlock.CodeBlockAllCodePathsReturn();
            else if (Condition.IsFalsey)
                return IfFalseBlock.CodeBlockAllCodePathsReturn();
            return IfTrueBlock.CodeBlockAllCodePathsReturn() && IfFalseBlock.CodeBlockAllCodePathsReturn();
        }
        public bool SomeCodePathsBreak() => IfTrueBlock.CodeBlockSomeCodePathsBreak() || IfFalseBlock.CodeBlockSomeCodePathsBreak();
    }

    partial class MatchStatement
    {
        public bool AllCodePathsReturn() => IsExhaustive && MatchHandlers.TrueForAll((handler) => handler.ToExecute.CodeBlockAllCodePathsReturn());

        public bool SomeCodePathsBreak()
        {
            foreach (MatchHandler handler in MatchHandlers)
                if (handler.ToExecute.CodeBlockSomeCodePathsBreak())
                    return true;
            return false;
        }
    }

    partial class WhileBlock
    {
        public bool AllCodePathsReturn() => Condition.IsTruey && !WhileTrueBlock.CodeBlockSomeCodePathsBreak();
        public bool SomeCodePathsBreak() => false;
    }

    partial class IterationForLoop
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class ReturnStatement
    {
        public bool AllCodePathsReturn() => true;
        public bool SomeCodePathsBreak() => false;
    }

    partial class AbortStatement
    {
        public bool AllCodePathsReturn() => true;
        public bool SomeCodePathsBreak() => false;
    }

    partial class MemoryDestroy
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ProcedureCall
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class MemorySet
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class SetValueAtIndex
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class SetPropertyValue
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class ReleaseReferenceElement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class SetReferenceTypeElement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class VariableDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class SetVariable
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class UnwrapEnumValue
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
    }
}