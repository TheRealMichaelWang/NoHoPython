using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed class IfElseBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueBlock { get; private set; }
        public CodeBlock IfFalseBlock { get; private set; }

        public IfElseBlock(IRValue condition, CodeBlock ifTrueBlock, CodeBlock ifFalseBlock)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueBlock = ifTrueBlock;
            IfFalseBlock = ifFalseBlock;
        }
    }

    public sealed class IfBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueblock { get; private set; }

        public IfBlock(IRValue condition, CodeBlock ifTrueblock)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueblock = ifTrueblock;
        }
    }

    public sealed class WhileBlock : IRStatement
    {
        public IRValue Condition { get; private set; }
        public CodeBlock whileTrueBlock { get; private set; }

        public WhileBlock(IRValue condition, CodeBlock whileTrueBlock)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            this.whileTrueBlock = whileTrueBlock;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IfElseValue : IRValue
    {
        public bool IsConstant => false;

        public IType Type { get; private set; }

        public IRValue Condition { get; private set; }
        public IRValue IfTrueValue { get; private set; }
        public IRValue IfFalseValue { get; private set; }

        public IfElseValue(IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            try
            {
                IfFalseValue = ArithmeticCast.CastTo(ifFalseValue, ifTrueValue.Type);
                IfTrueValue = ifTrueValue;
                Type = IfTrueValue.Type;
            }
            catch (UnexpectedTypeException)
            {
                IfTrueValue = ArithmeticCast.CastTo(ifTrueValue, ifFalseValue.Type);
                IfFalseValue = ifFalseValue;
                Type = IfFalseValue.Type;
            }
        }

        private IfElseValue(IType type, IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue)
        {
            Type = type;
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueValue = ArithmeticCast.CastTo(ifTrueValue, Type);
            IfFalseValue = ArithmeticCast.CastTo(ifFalseValue, Type);
        }
    }
}

namespace NoHoPython.Syntax.Statements
{
    partial class IfBlock
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            IAstStatement.ForwardDeclareBlock(irBuilder, IfTrueBlock);
            if (NextIf != null)
                NextIf.ForwardDeclare(irBuilder);
            else if (NextElse != null)
                NextElse.ForwardDeclare(irBuilder);
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean);

            CodeBlock codeBlock = irBuilder.SymbolMarshaller.NewCodeBlock();
            codeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, IfTrueBlock));
            irBuilder.SymbolMarshaller.GoBack();

            if (NextIf != null)
            {
                CodeBlock nextIf = irBuilder.SymbolMarshaller.NewCodeBlock();
                nextIf.DelayedLinkSetStatements(new List<IRStatement>() { NextIf.GenerateIntermediateRepresentationForStatement(irBuilder) });
                irBuilder.SymbolMarshaller.GoBack();
                return new IfElseBlock(condition, codeBlock, nextIf);
            }
            else return NextElse != null
                ? new IfElseBlock(condition, codeBlock, NextElse.GenerateIRCodeBlock(irBuilder))
                : new IntermediateRepresentation.Statements.IfBlock(condition, codeBlock);
        }
    }

    partial class ElseBlock
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) => IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();

        public CodeBlock GenerateIRCodeBlock(AstIRProgramBuilder irBuilder)
        {
            CodeBlock codeBlock = irBuilder.SymbolMarshaller.NewCodeBlock();
            codeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute));
            irBuilder.SymbolMarshaller.GoBack();
            return codeBlock;
        }
    }

    partial class WhileBlock
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) => IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean);

            CodeBlock codeBlock = irBuilder.SymbolMarshaller.NewCodeBlock();
            codeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute));
            irBuilder.SymbolMarshaller.GoBack();

            return new IntermediateRepresentation.Statements.WhileBlock(condition, codeBlock);
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class IfElseValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => new IntermediateRepresentation.Values.IfElseValue(Condition.GenerateIntermediateRepresentationForValue(irProgramBuilder, Primitive.Boolean), IfTrueValue.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), IfFalseValue.GenerateIntermediateRepresentationForValue(irProgramBuilder, null));
    }
}