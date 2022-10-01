using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Typing;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    public sealed partial class IfElseBlock : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueBlock { get; private set; }
        public CodeBlock IfFalseBlock { get; private set; }

        public IfElseBlock(IRValue condition, CodeBlock ifTrueBlock, CodeBlock ifFalseBlock, IAstElement errorReportedElement)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueBlock = ifTrueBlock;
            IfFalseBlock = ifFalseBlock;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class IfBlock : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Condition { get; private set; }
        public CodeBlock IfTrueBlock { get; private set; }

        public IfBlock(IRValue condition, CodeBlock ifTrueblock, IAstElement errorReportedElement)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            IfTrueBlock = ifTrueblock;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class WhileBlock : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Condition { get; private set; }
        public CodeBlock WhileTrueBlock { get; private set; }

        public WhileBlock(IRValue condition, CodeBlock whileTrueBlock, IAstElement errorReportedElement)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
            WhileTrueBlock = whileTrueBlock;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class MatchStatement : IRStatement
    {
        public sealed partial class MatchHandler
        {
            public IType MatchedType { get; private set; }

            public Variable? MatchedVariable { get; private set; }
            public CodeBlock ToExecute { get; private set; }

            public MatchHandler(IType matchedType, string? matchIdentifier, List<IAstStatement> toExecute, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
            {
                MatchedType = matchedType;
                ToExecute = irBuilder.SymbolMarshaller.NewCodeBlock();
                if (matchIdentifier != null)
                {
                    MatchedVariable = new(matchedType, matchIdentifier, irBuilder.ScopedProcedures.Peek(), false);
                    irBuilder.SymbolMarshaller.DeclareSymbol(MatchedVariable, errorReportedElement);
                }
                else
                    MatchedVariable = null;
                ToExecute.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, toExecute));
                irBuilder.SymbolMarshaller.GoBack();
            }
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue MatchValue { get; private set; }
        public readonly List<MatchHandler> MatchHandlers;

        public MatchStatement(IRValue matchValue, List<MatchHandler> matchHandlers, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            MatchValue = matchValue;
            MatchHandlers = matchHandlers;
        }
    }

    public sealed partial class AssertStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Condition { get; private set; }

        public AssertStatement(IRValue condition, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class IfElseValue : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public IType Type { get; private set; }

        public IRValue Condition { get; private set; }
        public IRValue IfTrueValue { get; private set; }
        public IRValue IfFalseValue { get; private set; }

        public IfElseValue(IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
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

        private IfElseValue(IType type, IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue, IAstElement errorReportedElement)
        {
            Type = type;
            ErrorReportedElement = errorReportedElement;
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
                return new IfElseBlock(condition, codeBlock, nextIf, this);
            }
            else return NextElse != null
                ? new IfElseBlock(condition, codeBlock, NextElse.GenerateIRCodeBlock(irBuilder), this)
                : new IntermediateRepresentation.Statements.IfBlock(condition, codeBlock, this);
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

            return new IntermediateRepresentation.Statements.WhileBlock(condition, codeBlock, this);
        }
    }

    partial class MatchStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) => MatchHandlers.ForEach((handler) => IAstStatement.ForwardDeclareBlock(irBuilder, handler.Statements));

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue matchValue = MatchedValue.GenerateIntermediateRepresentationForValue(irBuilder, null);
            if(matchValue.Type is EnumType enumType)
            {
                HashSet<IType> handledTypes = new(enumType.GetOptions(), new ITypeComparer());

                List<IntermediateRepresentation.Statements.MatchStatement.MatchHandler> matchHandlers = new(MatchHandlers.Count);
                foreach(MatchHandler handler in MatchHandlers)
                {
                    IType handledType = handler.MatchType.ToIRType(irBuilder, this);
                    if (!handledTypes.Contains(handledType))
                        throw new UnexpectedTypeException(handledType, this);
                    handledTypes.Remove(handledType);
                    matchHandlers.Add(new(handledType, handler.MatchIdentifier, handler.Statements, irBuilder, this));
                }
                foreach (IType unhandledOption in handledTypes)
                    throw new UnhandledMatchOption(enumType, unhandledOption, this);
                return new IntermediateRepresentation.Statements.MatchStatement(matchValue, matchHandlers, this);
            }
            throw new UnexpectedTypeException(matchValue.Type, this);
        }
    }

    partial class AssertStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => new IntermediateRepresentation.Statements.AssertStatement(Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean), this);
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class IfElseValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irProgramBuilder, IType? expectedType) => new IntermediateRepresentation.Values.IfElseValue(Condition.GenerateIntermediateRepresentationForValue(irProgramBuilder, Primitive.Boolean), IfTrueValue.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), IfFalseValue.GenerateIntermediateRepresentationForValue(irProgramBuilder, null), this);
    }
}