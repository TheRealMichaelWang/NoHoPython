using NoHoPython.IntermediateRepresentation.Statements;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRStatement
    {
        public bool AllCodePathsReturn();
        public bool SomeCodePathsBreak();
        public AbortStatement? AllCodePathsAbort();
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class CodeBlock
    {
#pragma warning disable CS8604 // Possible null reference argument.
        public bool CodeBlockAllCodePathsReturn() => Statements.Any(statement => statement.AllCodePathsReturn());
#pragma warning restore CS8604 // Possible null reference argument.

#pragma warning disable CS8604 // Possible null reference argument.
        public bool CodeBlockSomeCodePathsBreak() => Statements.Any(statement => statement.SomeCodePathsBreak());
#pragma warning restore CS8604 // Possible null reference argument.

        public AbortStatement? CodeBlockAllCodePathsAbort()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            foreach (IRStatement statement in Statements)
            {
                AbortStatement? abortStatement = statement.AllCodePathsAbort();
                if (abortStatement != null)
                    return abortStatement;
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            return null;
        }
    }

    partial class EnumDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
        public AbortStatement? AllCodePathsAbort() => throw new InvalidOperationException();
    }

    partial class InterfaceDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
        public AbortStatement? AllCodePathsAbort() => throw new InvalidOperationException();
    }

    partial class RecordDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
        public AbortStatement? AllCodePathsAbort() => throw new InvalidOperationException();
    }

    partial class ForeignCDeclaration
    {
        public bool AllCodePathsReturn() => throw new InvalidOperationException();
        public bool SomeCodePathsBreak() => throw new InvalidOperationException();
        public AbortStatement? AllCodePathsAbort() => throw new InvalidOperationException();
    }

    partial class ForeignCProcedureDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class ProcedureDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class CSymbolDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class LoopStatement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => Action.Type == Syntax.Parsing.TokenType.Break;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class AssertStatement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class IfBlock
    {
        public bool AllCodePathsReturn() => Condition.IsTruey && IfTrueBlock.CodeBlockAllCodePathsReturn();
        public bool SomeCodePathsBreak() => IfTrueBlock.CodeBlockSomeCodePathsBreak();
        public AbortStatement? AllCodePathsAbort() => Condition.IsTruey ? IfTrueBlock.CodeBlockAllCodePathsAbort() : null;
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

        public AbortStatement? AllCodePathsAbort()
        {
            if (Condition.IsTruey)
                return IfTrueBlock.CodeBlockAllCodePathsAbort();
            else if (Condition.IsFalsey)
                return IfFalseBlock.CodeBlockAllCodePathsAbort();
            AbortStatement? ifTrueRes = IfTrueBlock.CodeBlockAllCodePathsAbort();
            AbortStatement? ifFalseRes = IfFalseBlock.CodeBlockAllCodePathsAbort();
            if (ifTrueRes != null && ifFalseRes != null)
                return ifTrueRes;
            return null;
        }
    }

    partial class MatchStatement
    {
        public bool AllCodePathsReturn() //=> IsExhaustive && MatchHandlers.TrueForAll((handler) => handler.ToExecute.CodeBlockAllCodePathsReturn());
        {
            if (!IsExhaustive)
                return false;

            if (!MatchHandlers.All(handler => handler.ToExecute.CodeBlockAllCodePathsReturn()))
                return false;

            if (DefaultHandler != null && !DefaultHandler.CodeBlockAllCodePathsReturn())
                return false;

            return true;
        }

        public bool SomeCodePathsBreak()
        {
            foreach (MatchHandler handler in MatchHandlers)
                if (handler.ToExecute.CodeBlockSomeCodePathsBreak())
                    return true;
            if (DefaultHandler != null)
                return DefaultHandler.CodeBlockSomeCodePathsBreak();
            return false;
        }

        public AbortStatement? AllCodePathsAbort()
        {
            if (!IsExhaustive)
                return null;

            if (!MatchHandlers.All(handler => handler.ToExecute.CodeBlockAllCodePathsReturn()))
                return null;

            if (DefaultHandler != null && !DefaultHandler.CodeBlockAllCodePathsReturn())
                return null;

            if(MatchHandlers.Count > 0)
                return MatchHandlers[0].ToExecute.CodeBlockAllCodePathsAbort();
            return DefaultHandler?.CodeBlockAllCodePathsAbort();
        }
    }

    partial class WhileBlock
    {
        public bool AllCodePathsReturn() => Condition.IsTruey && !WhileTrueBlock.CodeBlockSomeCodePathsBreak();
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => Condition.IsTruey ? WhileTrueBlock.CodeBlockAllCodePathsAbort() : null;
    }

    partial class IterationForLoop
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class ReturnStatement
    {
        public bool AllCodePathsReturn() => true;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class AbortStatement
    {
        public bool AllCodePathsReturn() => true;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => this;
    }

    partial class MemoryDestroy
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class ProcedureCall
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class MemorySet
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class SetValueAtIndex
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class SetPropertyValue
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class ReleaseReferenceElement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class SetReferenceTypeElement
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class VariableDeclaration
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class SetVariable
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }

    partial class UnwrapEnumValue
    {
        public bool AllCodePathsReturn() => false;
        public bool SomeCodePathsBreak() => false;
        public AbortStatement? AllCodePathsAbort() => null;
    }
}