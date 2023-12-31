using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        //assuming the current value evaluates to true, this function will make the correct variable refinements
        public void RefineIfTrue(AstIRProgramBuilder irBuilder);

        //assuming the current value evaluates to false, this function will make the correct variable refinements
        public void RefineIfFalse(AstIRProgramBuilder irBuilder);

        //assuming the current value's type is the assumedType, this function will make the correct variable refinements 
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement);

        //when a variable is set to ths value, this function will make the correct variable refinements 
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry);

        //gets the refinment entry for a certain value
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder);

        //creates a refinment entry for a certain value
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder);
    } 
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class DecimalLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class CharacterLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class TrueLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class FalseLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class NullPointerLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class StaticCStringLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class EmptyTypeLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ArrayLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class TupleLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalIntoLowerTuple
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class AllocArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class AllocMemorySpan
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ProcedureCall
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class BinaryOperator
    {
        public virtual void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public virtual void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class LogicalOperator
    {
        public override void RefineIfFalse(AstIRProgramBuilder irBuilder)
        {
            if(Operation == LogicalOperation.Or)
            {
                Left.RefineIfFalse(irBuilder);
                Right.RefineIfFalse(irBuilder);
            }
        }

        public override void RefineIfTrue(AstIRProgramBuilder irBuilder) 
        { 
            if(Operation == LogicalOperation.And)
            {
                Left.RefineIfTrue(irBuilder);
                Right.RefineIfTrue(irBuilder);
            }
        }
    }

    partial class ComparativeOperator
    {
        public override void RefineIfFalse(AstIRProgramBuilder irBuilder)
        {
            if (Operation == CompareOperation.Equals)
            {
                if (Left.IsTruey)
                    Right.RefineIfFalse(irBuilder);
                else if (Right.IsTruey)
                    Left.RefineIfFalse(irBuilder);
                else if (Left.IsFalsey)
                    Right.RefineIfTrue(irBuilder);
                else if (Right.IsFalsey)
                    Left.RefineIfTrue(irBuilder);
            }
            else if (Operation == CompareOperation.NotEquals)
            {
                if (Left.IsTruey)
                    Right.RefineIfTrue(irBuilder);
                else if (Right.IsTruey)
                    Left.RefineIfTrue(irBuilder);
                else if (Left.IsFalsey)
                    Right.RefineIfFalse(irBuilder);
                else if (Right.IsFalsey)
                    Left.RefineIfFalse(irBuilder);
            }
        }

        public override void RefineIfTrue(AstIRProgramBuilder irBuilder)
        {
            if(Operation == CompareOperation.Equals)
            {
                if (Left.IsTruey)
                    Right.RefineIfTrue(irBuilder);
                else if (Right.IsTruey)
                    Left.RefineIfTrue(irBuilder);
                else if (Left.IsFalsey)
                    Right.RefineIfFalse(irBuilder);
                else if (Right.IsFalsey)
                    Left.RefineIfFalse(irBuilder);
            }
            else if(Operation == CompareOperation.NotEquals)
            {
                if (Left.IsTruey)
                    Right.RefineIfFalse(irBuilder);
                else if (Right.IsTruey)
                    Left.RefineIfFalse(irBuilder);
                else if (Left.IsFalsey)
                    Right.RefineIfTrue(irBuilder);
                else if (Right.IsFalsey)
                    Left.RefineIfTrue(irBuilder);
            }
        }
    }

    partial class GetValueAtIndex
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class SetValueAtIndex
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class GetPropertyValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement)
        {
            RefinementContext.RefinementEntry? entry = GetRefinementEntry(irBuilder) ?? CreateRefinementEntry(irBuilder);

            if(entry != null)
                entry.SetRefinement(assumedRefinement);
        }

        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => Record.GetRefinementEntry(irBuilder)?.GetSubentry(Property.Name);

        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => Record.CreateRefinementEntry(irBuilder)?.NewSubentry(Property.Name);
    }

    partial class SetPropertyValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }

        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null; //refinement entry should be cleared immediatley upon IR generation
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ArithmeticCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) => Input.RefineIfTrue(irBuilder);
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) => Input.RefineIfFalse(irBuilder);
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => Input.GetRefinementEntry(irBuilder);
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => Input.CreateRefinementEntry(irBuilder);
    }

    partial class HandleCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) => Input.RefineIfTrue(irBuilder);
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) => Input.RefineIfFalse(irBuilder);
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => Input.GetRefinementEntry(irBuilder);
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => Input.CreateRefinementEntry(irBuilder);
    }

    partial class AutoCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) => Input.RefineIfTrue(irBuilder);
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) => Input.RefineIfFalse(irBuilder);
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => Input.GetRefinementEntry(irBuilder);
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => Input.CreateRefinementEntry(irBuilder);
    }

    partial class ArrayOperator
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class VariableReference
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement)
        {
            RefinementContext.RefinementEntry? entry = irBuilder.Refinements.Peek().GetRefinementEntry(Variable);
            if (entry == null)
                irBuilder.Refinements.Peek().NewRefinementEntry(Variable, new(assumedRefinement, new(), null));
            else
                entry.SetRefinement(assumedRefinement);
        }

        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }

        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => irBuilder.Refinements.Peek().GetRefinementEntry(Variable);

        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder)
        {
            RefinementContext.RefinementEntry? existingEntry = irBuilder.Refinements.Peek().GetRefinementEntry(Variable);
            if (existingEntry != null)
                return existingEntry;

            RefinementContext.RefinementEntry newEntry = new RefinementContext.RefinementEntry(null, new(), null);
            irBuilder.Refinements.Peek().NewRefinementEntry(Variable, newEntry);
            return newEntry;
        }
    }

    partial class VariableDeclaration
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class SetVariable
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class CSymbolReference
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class AnonymizeProcedure
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class IfElseValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class SizeofOperator
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalHandleIntoArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalIntoEnum
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;

        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) => destinationEntry.SetRefinement((Value.Type, EnumDeclaration.GetRefinedEnumEmitter(TargetType, Value.Type)));
    }

    partial class UnwrapEnumValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class CheckEnumOption
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) => EnumValue.RefineAssumeType(irBuilder, (Option, EnumDeclaration.GetRefinedEnumEmitter((EnumType)EnumValue.Type, Option)));
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalIntoInterface
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MemorySet
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineIfFalse(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementContext.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, RefinementContext.RefinementEntry destinationEntry) { }
        public RefinementContext.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public RefinementContext.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }
}