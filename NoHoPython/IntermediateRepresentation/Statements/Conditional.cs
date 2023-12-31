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

        public IfElseBlock(IRValue condition, CodeBlock ifTrueBlock, CodeBlock ifFalseBlock, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean, irBuilder);
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

        public IfBlock(IRValue condition, CodeBlock ifTrueblock, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean, irBuilder);
            IfTrueBlock = ifTrueblock;
            ErrorReportedElement = errorReportedElement;
        }
    }

    public sealed partial class WhileBlock : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Condition { get; private set; }
        public CodeBlock WhileTrueBlock { get; private set; }

        public WhileBlock(IRValue condition, CodeBlock whileTrueBlock, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean, irBuilder);
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
            public List<IType> MatchTypes { get; private set; }

            public Variable? MatchedVariable { get; private set; }
            public CodeBlock ToExecute { get; private set; }

            public MatchHandler(IRValue matchValue, List<IType> matchedTypes, string? matchIdentifier, CodeBlock toExecute, List<IAstStatement> toExecuteStatements, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
            {
                MatchTypes = matchedTypes;
                ToExecute = toExecute;
                irBuilder.SymbolMarshaller.NavigateToScope(toExecute);

                if (matchedTypes.Count == 1)
                    matchValue.RefineAssumeType(irBuilder, (matchedTypes[0], EnumDeclaration.GetRefinedEnumEmitter((EnumType)matchValue.Type, matchedTypes[0])));

                if (matchIdentifier != null)
                {
                    if (matchedTypes[0].IsEmpty)
                        throw new UnexpectedTypeException(matchedTypes[0], errorReportedElement);
                    MatchedVariable = new(matchedTypes[0], matchIdentifier, matchValue.IsReadOnly, irBuilder.ScopedProcedures.Peek(), false, errorReportedElement);
                    irBuilder.SymbolMarshaller.DeclareSymbol(MatchedVariable, errorReportedElement);
                    ToExecute.LocalVariables.Add(MatchedVariable);
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
        public bool IsExhaustive { get; private set; }

        public MatchStatement(IRValue matchValue, List<MatchHandler> matchHandlers, CodeBlock? defaultHandler, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            MatchValue = matchValue;
            MatchHandlers = matchHandlers;
            DefaultHandler = defaultHandler;

            IsExhaustive = !((EnumType)MatchValue.Type).GetOptions().Any((option) => !matchHandlers.Any((handler) => handler.MatchTypes.Any((type) => type.IsCompatibleWith(option))));

            if(defaultHandler != null)
            {
                if (IsExhaustive)
                    throw new DefaultHandlerUnreachable(errorReportedElement);
                else
                    IsExhaustive = true;
            }
        }
    }

    public sealed partial class LoopStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }
        public Token Action { get; private set; }

        private int? breakLabelId;

        public LoopStatement(Token action, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Action = action;

            breakLabelId = action.Type == TokenType.Break ? irBuilder.SymbolMarshaller.CurrentCodeBlock.GetLoopBreakLabelId(errorReportedElement, irBuilder) : null;
        }
    }

    public sealed partial class AssertStatement : IRStatement
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IRValue Condition { get; private set; }

        public AssertStatement(IRValue condition, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean, irBuilder);
        }

        private AssertStatement(IRValue condition, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Condition = condition;
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

        public IfElseValue(IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue, AstIRProgramBuilder irBuilder, IAstElement errorReportedElement)
        {
            ErrorReportedElement = errorReportedElement;
            Condition = ArithmeticCast.CastTo(condition, Primitive.Boolean, irBuilder);
            try
            {
                IfFalseValue = ArithmeticCast.CastTo(ifFalseValue, ifTrueValue.Type, irBuilder);
                IfTrueValue = ifTrueValue;
                Type = IfTrueValue.Type;
            }
            catch (UnexpectedTypeException)
            {
                IfTrueValue = ArithmeticCast.CastTo(ifTrueValue, ifFalseValue.Type, irBuilder);
                IfFalseValue = ifFalseValue;
                Type = IfFalseValue.Type;
            }
        }

        private IfElseValue(IType type, IRValue condition, IRValue ifTrueValue, IRValue ifFalseValue, IAstElement errorReportedElement)
        {
            Type = type;
            ErrorReportedElement = errorReportedElement;
            Condition = condition;
            IfTrueValue = ifTrueValue;
            IfFalseValue = ifFalseValue;
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
            scopedCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(false, SourceLocation);
            IAstStatement.ForwardDeclareBlock(irBuilder, IfTrueBlock);
            irBuilder.SymbolMarshaller.GoBack();

            if (NextIf != null)
            {
                scopedNextIf = irBuilder.SymbolMarshaller.NewCodeBlock(false, NextIf.SourceLocation);
                NextIf.ForwardDeclare(irBuilder);
                irBuilder.SymbolMarshaller.GoBack();
            }
            else if (NextElse != null)
                NextElse.ForwardDeclare(irBuilder);
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, false);

            RefinementContext? parentEntry = irBuilder.NewRefinmentContext();
            irBuilder.SymbolMarshaller.NavigateToScope(scopedCodeBlock);
            condition.RefineIfTrue(irBuilder);
            scopedCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, IfTrueBlock), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.Refinements.Pop();

            if (scopedNextIf != null)
            {
                irBuilder.NewRefinmentContext();
                irBuilder.SymbolMarshaller.NavigateToScope(scopedNextIf);
                condition.RefineIfFalse(irBuilder);
#pragma warning disable CS8602 // NextIf is never null when scopedNextIf isn't
                scopedNextIf.DelayedLinkSetStatements(new List<IRStatement>() { NextIf.GenerateIntermediateRepresentationForStatement(irBuilder) }, irBuilder);
#pragma warning restore CS8602
                irBuilder.SymbolMarshaller.GoBack();
                irBuilder.Refinements.Pop();

                if (scopedCodeBlock.CodeBlockAllCodePathsReturn() && scopedNextIf.CodeBlockAllCodePathsReturn())
                    parentEntry?.DiscardQueuedRefinements();
                else
                    parentEntry?.ApplyQueuedRefinements();
                return new IfElseBlock(condition, scopedCodeBlock, scopedNextIf, irBuilder, this);
            }
            else
            {
                if (NextElse != null)
                {
                    irBuilder.NewRefinmentContext();
                    condition.RefineIfFalse(irBuilder);
                    CodeBlock elseBlock = NextElse.GenerateIRCodeBlock(irBuilder);
                    irBuilder.Refinements.Pop();
                    if (scopedCodeBlock.CodeBlockAllCodePathsReturn() && elseBlock.CodeBlockAllCodePathsReturn())
                        parentEntry?.DiscardQueuedRefinements();
                    else
                        parentEntry?.ApplyQueuedRefinements();
                    return new IfElseBlock(condition, scopedCodeBlock, elseBlock, irBuilder, this);
                }
                else
                {
                    if (scopedCodeBlock.CodeBlockAllCodePathsReturn() || scopedCodeBlock.CodeBlockSomeCodePathsBreak())
                        condition.RefineIfFalse(irBuilder);

                    if (scopedCodeBlock.CodeBlockAllCodePathsReturn())
                        parentEntry?.DiscardQueuedRefinements();
                    else
                        parentEntry?.ApplyQueuedRefinements();
                    return new IntermediateRepresentation.Statements.IfBlock(condition, scopedCodeBlock, irBuilder, this);
                }
            }
        }
    }

    partial class ElseBlock
    {
        private CodeBlock scopedToExecute;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedToExecute = irBuilder.SymbolMarshaller.NewCodeBlock(false, SourceLocation);
            IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => throw new InvalidOperationException();

        public CodeBlock GenerateIRCodeBlock(AstIRProgramBuilder irBuilder)
        {
            irBuilder.NewRefinmentContext();
            irBuilder.SymbolMarshaller.NavigateToScope(scopedToExecute);
            scopedToExecute.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.Refinements.Pop();
            return scopedToExecute;
        }
    }

    partial class WhileBlock
    {
        private CodeBlock scopedCodeBlock;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(true, SourceLocation);
            IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, true);

            RefinementContext? parentContext = irBuilder.NewRefinmentContext();
            irBuilder.SymbolMarshaller.NavigateToScope(scopedCodeBlock);
            condition.RefineIfTrue(irBuilder);
            scopedCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.Refinements.Pop();

            if (scopedCodeBlock.CodeBlockAllCodePathsReturn() || (condition.IsTruey && !scopedCodeBlock.CodeBlockSomeCodePathsBreak()))
                parentContext?.DiscardQueuedRefinements();
            else
                parentContext?.ApplyQueuedRefinements();

            return new IntermediateRepresentation.Statements.WhileBlock(condition, scopedCodeBlock, irBuilder, this);
        }
    }

    partial class IterationForLoop
    {
        private CodeBlock scopedCodeBlock;

        public void ForwardTypeDeclare(AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclare(AstIRProgramBuilder irBuilder)
        {
            scopedCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(true, SourceLocation);
            IAstStatement.ForwardDeclareBlock(irBuilder, ToExecute);
            irBuilder.SymbolMarshaller.GoBack();
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue lowerBound = ArithmeticOperator.ComposeArithmeticOperation(ArithmeticOperator.ArithmeticOperation.Subtract, LowerBound.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, false), new IntegerLiteral(1, this), irBuilder, this);
            IRValue upperBound = ArithmeticCast.CastTo(UpperBound.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Integer, false), Primitive.Integer, irBuilder);

            irBuilder.NewRefinmentContext();
            irBuilder.SymbolMarshaller.NavigateToScope(scopedCodeBlock);
            VariableDeclaration iteratorDeclaration = new(IteratorIdentifier, lowerBound, false, irBuilder, this);
            scopedCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, ToExecute), irBuilder);
            irBuilder.SymbolMarshaller.GoBack();
            irBuilder.PopAndApplyRefinementContext();

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
                handlerCodeBlocks.Add(handler, irBuilder.SymbolMarshaller.NewCodeBlock(false, SourceLocation));
                IAstStatement.ForwardDeclareBlock(irBuilder, handler.Statements);
                irBuilder.SymbolMarshaller.GoBack();
            });
            if(DefaultHandler != null)
            {
                defaultHandlerCodeBlock = irBuilder.SymbolMarshaller.NewCodeBlock(false, SourceLocation);
                IAstStatement.ForwardDeclareBlock(irBuilder, DefaultHandler);
                irBuilder.SymbolMarshaller.GoBack();
            }
        }

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder)
        {
            IRValue matchValue = MatchedValue.GenerateIntermediateRepresentationForValue(irBuilder, null, false);
            RefinementContext? parentContext = null;
            if(matchValue.Type is EnumType enumType)
            {
                List<IntermediateRepresentation.Statements.MatchStatement.MatchHandler> matchHandlers = new(MatchHandlers.Count);
                foreach (MatchHandler handler in MatchHandlers)
                {
                    if(parentContext != null)
                        parentContext = irBuilder.NewRefinmentContext();
                    
                    matchHandlers.Add(new(matchValue, handler.MatchTypes.ConvertAll((type) => type.ToIRType(irBuilder, this)), handler.MatchIdentifier, handlerCodeBlocks[handler], handler.Statements, irBuilder, this));
                    irBuilder.Refinements.Pop();
                }

                if(defaultHandlerCodeBlock != null)
                {
                    if (parentContext != null)
                        parentContext = irBuilder.NewRefinmentContext();
                    irBuilder.SymbolMarshaller.NavigateToScope(defaultHandlerCodeBlock);
#pragma warning disable CS8604 // Default handler is not null when defaultHandlerCodeBlock isn't null
                    defaultHandlerCodeBlock.DelayedLinkSetStatements(IAstStatement.GenerateIntermediateRepresentationForBlock(irBuilder, DefaultHandler), irBuilder);
#pragma warning restore CS8604
                    irBuilder.SymbolMarshaller.GoBack();
                    irBuilder.Refinements.Pop();
                }

                var toreturn = new IntermediateRepresentation.Statements.MatchStatement(matchValue, matchHandlers, defaultHandlerCodeBlock, this);
                if (toreturn.AllCodePathsReturn())
                    parentContext?.DiscardQueuedRefinements();
                else
                    parentContext?.ApplyQueuedRefinements();
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

        public IRStatement GenerateIntermediateRepresentationForStatement(AstIRProgramBuilder irBuilder) => new IntermediateRepresentation.Statements.AssertStatement(Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, false), irBuilder, this);
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class IfElseValue
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate)
        {
            IRValue condition = Condition.GenerateIntermediateRepresentationForValue(irBuilder, Primitive.Boolean, willRevaluate);

            irBuilder.NewRefinmentContext();
            condition.RefineIfTrue(irBuilder);
            IRValue ifTrueValue = IfTrueValue.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);
            irBuilder.Refinements.Pop();

            irBuilder.NewRefinmentContext();
            condition.RefineIfFalse(irBuilder);
            IRValue ifFalseValue = IfFalseValue.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate);
            irBuilder.PopAndApplyRefinementContext();

            return new IntermediateRepresentation.Values.IfElseValue(condition, ifTrueValue, ifFalseValue, irBuilder, this);
        }
    }
}