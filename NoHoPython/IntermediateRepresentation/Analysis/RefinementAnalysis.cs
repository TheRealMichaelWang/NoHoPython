using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation
{
    partial interface IRValue
    {
        //assuming the current value evaluates to true, this function will make the correct variable refinements
        public void RefineIfTrue(AstIRProgramBuilder irBuilder);

        //assuming the current value's type is the assumedType, this function will make the correct variable refinements 
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement);

        //when a variable is set to ths value, this function will make the correct variable refinements 
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet);
    } 
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class DecimalLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class CharacterLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class TrueLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class FalseLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class NullPointerLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class StaticCStringLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class EmptyTypeLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class ArrayLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class TupleLiteral
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class MarshalIntoLowerTuple
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class InterpolatedString
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class AllocArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class AllocMemorySpan
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class ProcedureCall
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class BinaryOperator
    {
        public virtual void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
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
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class SetValueAtIndex
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class GetPropertyValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class SetPropertyValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class ArithmeticCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class HandleCast
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class ArrayOperator
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class VariableReference
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, RefinementEmitter?) assumedRefinement) => irBuilder.SymbolMarshaller.CurrentCodeBlock.RefineVariable(Variable, assumedRefinement);

        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class VariableDeclaration
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class SetVariable
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class CSymbolReference
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class AnonymizeProcedure
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class IfElseValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class SizeofOperator
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class MarshalHandleIntoArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class MarshalMemorySpanIntoArray
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class MarshalIntoEnum
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }

        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) => irBuilder.SymbolMarshaller.CurrentCodeBlock.RefineVariable(toSet, (Value.Type, EnumDeclaration.GetRefinedEnumEmitter(Value.Type)));
    }

    partial class UnwrapEnumValue
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class CheckEnumOption
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) => EnumValue.RefineAssumeType(irBuilder, (Option, EnumDeclaration.GetRefinedEnumEmitter(Option)));

        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class MarshalIntoInterface
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }

    partial class MemorySet
    {
        public void RefineIfTrue(AstIRProgramBuilder irBuilder) { }
        public void RefineAssumeType(AstIRProgramBuilder irBuilder, (IType, VariableReference.RefinementEmitter?) assumedRefinement) { }
        public void RefineSetVariable(AstIRProgramBuilder irBuilder, Variable toSet) { }
    }
}