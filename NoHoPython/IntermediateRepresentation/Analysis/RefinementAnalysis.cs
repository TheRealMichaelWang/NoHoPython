using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        //assuming the current value evaluates to true, this function will make the correct variable refinements
        public void RefineIfTrue(AstIRProgramBuilder irBuilder);

        //assuming the current value's type is the assumedType, this function will make the correct variable refinements 
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement);

        //when a variable is set to ths value, this function will make the correct variable refinements 
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry);

        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder);
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder);
    } 
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class DecimalLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class CharacterLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class TrueLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class FalseLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class NullPointerLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class StaticCStringLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class EmptyTypeLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ArrayLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class TupleLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalIntoLowerTuple
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class InterpolatedString
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class AllocArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class AllocMemorySpan
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ProcedureCall
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class BinaryOperator
    {
        public virtual void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class LogicalOperator
    {
        public override void RefineIfTrue(AstIRProgramBuilder irBuilder) 
        { 
            if(Operation == LogicalOperation.And)
            {
                Left.RefineIfTrue(irBuilder);
                Right.RefineIfTrue(irBuilder);
            }
        }
    }

    partial class GetValueAtIndex
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class SetValueAtIndex
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class GetPropertyValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement)
        {
            CodeBlock.RefinementEntry? entry = GetRefinementEntry(irBuilder) ?? CreateRefinementEntry(irBuilder);

            if(entry != null)
                entry.Refinement = assumedRefinement;
        }

        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => Record.GetRefinementEntry(irBuilder)?.GetSubentry(Property.Name);

        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => Record.CreateRefinementEntry(irBuilder)?.NewSubentry(Property.Name);
    }

    partial class SetPropertyValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }

        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null; //refinement entry should be cleared immediatley upon IR generation
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ArithmeticCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class HandleCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class ArrayOperator
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class VariableReference
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement)
        {
            CodeBlock.RefinementEntry? entry = irBuilder.SymbolMarshaller.CurrentCodeBlock.GetRefinementEntry(Variable, true);
            if (entry == null)
                irBuilder.SymbolMarshaller.CurrentCodeBlock.NewRefinementEntry(Variable, new(assumedRefinement, new()));
            else
                entry.Refinement = assumedRefinement;
        }

        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }

        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => irBuilder.SymbolMarshaller.CurrentCodeBlock.GetRefinementEntry(Variable, true);

        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder)
        {
            CodeBlock.RefinementEntry? existingEntry = irBuilder.SymbolMarshaller.CurrentCodeBlock.GetRefinementEntry(Variable, true);
            if (existingEntry != null)
                return existingEntry;

            CodeBlock.RefinementEntry newEntry = new CodeBlock.RefinementEntry(null, new());
            irBuilder.SymbolMarshaller.CurrentCodeBlock.NewRefinementEntry(Variable, newEntry);
            return newEntry;
        }
    }

    partial class VariableDeclaration
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class SetVariable
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class CSymbolReference
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class AnonymizeProcedure
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class IfElseValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class SizeofOperator
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalHandleIntoArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalIntoEnum
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;

        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) => destinationEntry.Refinement = (Value.Type, EnumDeclaration.GetRefinedEnumEmitter(TargetType, Value.Type));
    }

    partial class UnwrapEnumValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class CheckEnumOption
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) => EnumValue.RefineAssumeType(irBuilder, (Option, EnumDeclaration.GetRefinedEnumEmitter((EnumType)EnumValue.Type, Option)));

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MarshalIntoInterface
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }

    partial class MemorySet
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, CodeBlock.RefinementEmitter?) assumedRefinement) { }
        public void RefineSet(AstIRProgramBuilder irBuilder, CodeBlock.RefinementEntry destinationEntry) { }
        public CodeBlock.RefinementEntry? GetRefinementEntry(AstIRProgramBuilder irBuilder) => null;
        public CodeBlock.RefinementEntry? CreateRefinementEntry(AstIRProgramBuilder irBuilder) => null;
    }
}