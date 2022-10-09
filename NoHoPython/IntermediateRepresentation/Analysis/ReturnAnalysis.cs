namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRStatement
    {
        public bool AllCodePathsReturn();
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
        public bool CodeBlockAllCodePathsReturn()
        {
#pragma warning disable CS8602 // Called after dallayedlinksetstatements
            foreach (IRStatement statement in Statements)
                if (statement.AllCodePathsReturn())
                    return true;
#pragma warning restore CS8602
            return false;
        }
    }

    partial class EnumDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
    }

    partial class ForeignCProcedureDeclaration
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class ProcedureDeclaration
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class LoopStatement
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class AssertStatement
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class IfBlock
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class IfElseBlock
    {
        public bool AllCodePathsReturn() => IfTrueBlock.CodeBlockAllCodePathsReturn() && IfFalseBlock.CodeBlockAllCodePathsReturn();
    }

    partial class MatchStatement
    {
        public bool AllCodePathsReturn()
        {
            foreach (MatchHandler handler in MatchHandlers)
                if (!handler.ToExecute.CodeBlockAllCodePathsReturn())
                    return false;
            return true;
        }
    }

    partial class WhileBlock
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class ReturnStatement
    {
        public bool AllCodePathsReturn() => true;
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ProcedureCall
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class SetValueAtIndex
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class SetPropertyValue
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class VariableDeclaration
    {
        public bool AllCodePathsReturn() => false;
    }

    partial class SetVariable
    {
        public bool AllCodePathsReturn() => false;
    }
}