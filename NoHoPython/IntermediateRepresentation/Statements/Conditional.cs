using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Syntax;
using NoHoPython.Syntax.Parsing;
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

    public sealed partial class IterationForLoop : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public VariableDeclaration IteratorVariableDeclaration { get; private set; }
        public IRValue UpperBound { get; private set; }

        public CodeBlock IterationBlock { get; private set; }

        public IterationForLoop(VariableDeclaration iteratorVariableDeclaration, IRValue upperBound, CodeBlock iterationBlock, IAstElement errorReportedElement)
        {
            IteratorVariableDeclaration = iteratorVariableDeclaration;
            UpperBound = upperBound;
            IterationBlock = iterationBlock;
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

            public MatchHandler(IType matchedType, string? matchIdentifier, CodeBlock toExecute, List<IAstStatement> toExecuteStatements, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
            {
                MatchedType = matchedType;
                ToExecute = toExecute;
                irBuilder.SymbolMarshaller.NavigateToScope(toExecute);

                if (matchIdentifier != null)
                {
                    if (matchedType.IsEmpty)
                        throw new UnexpectedTypeException(matchedType, errorReportedElement);
                    MatchedVariable = new(matchedType, matchIdentifier, irBuilder.ScopedProcedures.Peek(), false);
                    irBuilder.SymbolMarshaller.DeclareSymbol(MatchedVariable, errorReportedElement);
                }
                else
                    MatchedVariable = null;
                
                ToExecute.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, toExecuteStatements), irBuilder);
                irBuilder.SymbolMarshaller.GoBack();
            }
        }

        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue MatchValue { get; private set; }
        public readonly List<MatchHandler> MatchHandlers;
        public CodeBlock? DefaultHandler { get; private set; }

        public MatchStatement(IRValue matchValue, List<MatchHandler> matchHandlers, CodeBlock? defaultHandler, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            MatchValue = matchValue;
            MatchHandlers = matchHandlers;
            DefaultHandler = defaultHandler;
        }
    }

    public sealed partial class LoopStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public Token Action { get; private set; }

        private List<Variable> activeLoopVariables;
        private int? breakLabelId;

        public LoopStatement(Token action, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Action = action;
            activeLoopVariables = irBuilder.SymbolMarshaller.CurrentCodeBlock.GetLoopLocals(errorReportedElement);

            breakLabelId = action.Type == TokenType.Break ? irBuilder.SymbolMarshaller.CurrentCodeBlock.GetLoopBreakLabelId(errorReportedElement, irBuilder) : null;
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

        public bool IsTruey => false;
        public bool IsFalsey => false;

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
        private CodeBlock scopedCodeBlock;
        private CodeBlock? scopedNextIf = null;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(false);
            IAstStatement.ForwardDeclareBlock(irBuilder, IfTrueBlock);
            irBuilder.SymbolMarshaller.GoBack();

            if (NextIf != null)
            {
                scopedNextIf = irBuilder.SymbolMarshaller.NewCodeBlock(false);
                NextIf.ForwardDeclare(irBuilder);
                irBuilder.SymbolMarshaller.GoBack();
            }
            else if (NextElse != null)
                NextElse.ForwardDeclare(irBuilder);
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, false);

            irBuilder.SymbolMarshaller.NavigateToScope(scopedCodeBlock);
            scopedCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, IfTrueBlock), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();

            if (scopedNextIf != null)
            {
                irBuilder.SymbolMarshaller.NavigateToScope(scopedNextIf);
#pragma warning disable CS8602 // NextIf is never null when scopedNextIf isn't
                scopedNextIf.DelayedLinkSetStatements(new List<IRStatement>() { NextIf.GenerateIntermediateRepresentationForStatement(irBuilder) }, irBuilder);
#pragma warning restore CS8602
                irBuilder.SymbolMarshaller.GoBack();
                return new IfElseBlock(condition, scopedCodeBlock, scopedNextIf, this);
            }
            else return NextElse != null
                ? new IfElseBlock(condition, scopedCodeBlock, NextElse.GenerateIRCodeBlock(irBuilder), this)
                : new IntermediateRepresentation.Statements.IfBlock(condition, scopedCodeBlock, this);
        }
    }

    partial class ElseBlock
    {
        private CodeBlock scopedToExecute;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedToExecute = irBuilder.SymbolMarshaller.NewCodeBlock(false);
            IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();

        public CodeBlock GenerateIRCodeBlock(AstIRProgramBuilder irBuilder)
        {
            irBuilder.SymbolMarshaller.NavigateToScope(scopedToExecute);
            scopedToExecute.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();
            return scopedToExecute;
        }
    }

    partial class WhileBlock
    {
        private CodeBlock scopedCodeBlock;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(true);
            IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, true);

            irBuilder.SymbolMarshaller.NavigateToScope(scopedCodeBlock);
            scopedCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();

            return new IntermediateRepresentation.Statements.WhileBlock(condition, scopedCodeBlock, this);
        }
    }

    partial class IterationForLoop
    {
        private CodeBlock scopedCodeBlock;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(true);
            IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue lowerBound = ArithmeticOperator.ComposeArithmeticOperation(ArithmeticOperator.ArithmeticOperation.Subtract, LowerBound.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, false), new IntegerLiteral(1, this), this);
            IRValue upperBound = UpperBound.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, false);

            irBuilder.SymbolMarshaller.NavigateToScope(scopedCodeBlock);
            VariableDeclaration iteratorDeclaration = new(IteratorIdentifier, lowerBound, false, irBuilder, this);
            scopedCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();

            return new IntermediateRepresentation.Statements.IterationForLoop(iteratorDeclaration, upperBound, scopedCodeBlock, this);
        }
    }

    partial class MatchStatement
    {
        private Dictionary<MatchHandler, CodeBlock> handlerCodeBlocks;
        private CodeBlock? defaultHandlerCodeBlock = null;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            handlerCodeBlocks = new Dictionary<MatchHandler, CodeBlock>();
            MatchHandlers.ForEach((handler) => {
                handlerCodeBlocks.Add(handler, irBuilder.SymbolMarshaller.NewCodeBlock(false));
                IAstStatement.ForwardDeclareBlock(irBuilder, handler.Statements);
                irBuilder.SymbolMarshaller.GoBack();
            });
            if(DefaultHandler != null)
            {
                defaultHandlerCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(false);
                IAstStatement.ForwardDeclareBlock(irBuilder, DefaultHandler);
                irBuilder.SymbolMarshaller.GoBack();
            }
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue matchValue = MatchedValue.GenerateIntermediateRepresentationForValue(irBuilder, null, false);
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
                    matchHandlers.Add(new(handledType, handler.MatchIdentifier, handlerCodeBlocks[handler], handler.Statements, irBuilder, this));
                }
                if (defaultHandlerCodeBlock == null)
                    foreach (IType unhandledOption in handledTypes)
                        throw new UnhandledMatchOption(enumType, unhandledOption, this);
                else
                {
                    irBuilder.SymbolMarshaller.NavigateToScope(defaultHandlerCodeBlock);
#pragma warning disable CS8604 // Default handler is not null when defaultHandlerCodeBlock isn't null
                    defaultHandlerCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, DefaultHandler), irBuilder);
#pragma warning restore CS8604
                    irBuilder.SymbolMarshaller.GoBack();
                }
                return new IntermediateRepresentation.Statements.MatchStatement(matchValue, matchHandlers, defaultHandlerCodeBlock, this);
            }
            throw new UnexpectedTypeException(matchValue.Type, this);
        }
    }

    partial class LoopStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => new IntermediateRepresentation.Statements.LoopStatement(Action, irBuilder, this);
    }

    partial class AssertStatement
    {
        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder) { }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => new IntermediateRepresentation.Statements.AssertStatement(Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, false), this);
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class IfElseValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.IfElseValue(Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, willRevaluate), IfTrueValue.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), IfFalseValue.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate), this);
    }
}