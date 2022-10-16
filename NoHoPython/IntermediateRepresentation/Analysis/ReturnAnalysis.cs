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
#pragma warning disable CS8602 //Statements initialized during ir generation
        public bool CodeBlockAllCodePathsReturn() => Statements.TrueForAll((statement) => statement.AllCodePathsReturn());
#pragma warning restore CS8602

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
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => IfTrueBlock.CodeBlockSomeCodePathsBreak();
    }

    partial class IfElseBlock
    {
        public bool AllCodePathsReturn() => IfTrueBlock.CodeBlockAllCodePathsReturn() && IfFalseBlock.CodeBlockAllCodePathsReturn();
        public bool SomeCodePathsBreak() => IfTrueBlock.CodeBlockSomeCodePathsBreak() || IfFalseBlock.CodeBlockSomeCodePathsBreak();
    }

    partial class MatchStatement
    {
        public bool AllCodePathsReturn() => MatchHandlers.TrueForAll((handler) => handler.ToExecute.CodeBlockSomeCodePathsBreak());

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
        public bool AllCodePathsReturn() => Condition.IsTruey ? !WhileTrueBlock.CodeBlockSomeCodePathsBreak() : false;
        public bool SomeCodePathsBreak() => false;
    }

    partial class ReturnStatement
    {
        public bool AllCodePathsReturn() => true;
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
}