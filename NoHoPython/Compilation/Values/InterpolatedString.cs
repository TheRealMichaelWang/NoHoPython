using NoHoPython.IntermediateRepresentation;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public string GetFormatSpecifier(IRProgram irProgram);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource);
    }

    partial class Primitive
    {
        public abstract string GetFormatSpecifier(IRProgram irProgram);
        public virtual void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.Append(valueCSource);
    }

    partial class ArrayType
    {
        public string GetFormatSpecifier(IRProgram irProgram) => ElementType.IsCompatibleWith(Primitive.Character) ? "%.*s" : "%p";

        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource)
        {
            if (!ElementType.IsCompatibleWith(Primitive.Character))
                emitter.Append("(void*)");

            emitter.Append(valueCSource);
            if (ElementType.IsCompatibleWith(Primitive.Character))
            {
                emitter.Append(".length, ");
                emitter.Append(valueCSource);
            }
            emitter.Append(".buffer");
        }
    }

    partial class BooleanType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%s";

        public override void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.Append($"(({valueCSource}) ? \"true\" : \"false\")");
    }

    partial class CharacterType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%c";
    }

    partial class IntegerType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%li";
    }

    partial class DecimalType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%lf";
    }

    partial class HandleType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%p";
    }

    partial class EmptyEnumOption
    {
        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class EnumType
    {
        public string GetFormatSpecifier(IRProgram irProgram) => irProgram.NameRuntimeTypes ? "%s" : throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.Append(irProgram.NameRuntimeTypes ? $"{GetStandardIdentifier(irProgram)}_typenames[(int){valueCSource}.option]" : throw new InvalidOperationException());
    }

    partial class RecordType
    {
        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class InterfaceType
    {
        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class ProcedureType
    {
        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class NothingType
    {
        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class TypeParameterReference
    {
        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => throw new InvalidOperationException();
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class InterpolatedString
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.ScopeForUsedTypes(irBuilder);
            foreach (object value in InterpolatedValues)
                if (value is IRValue irValue)
                    irValue.ScopeForUsedTypes(new(), irBuilder);
        }

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            if (!irProgram.EmitExpressionStatements)
                throw new CannotEmitInterpolatedString(this);

            irProgram.ExpressionDepth++;
            StringBuilder formatBuilder = new();
            List<IRValue> arguments = new();
            SortedSet<int> bufferedArguments = new();
            void emitFormatValues()
            {
                for (int i = 0; i < arguments.Count; i++)
                {
                    emitter.Append(", ");
                    if (bufferedArguments.Contains(i))
                        arguments[i].Type.EmitFormatValue(irProgram, emitter, $"intpd_buffered_arg{i}{irProgram.ExpressionDepth}");
                    else
                    {
                        StringBuilder valueBuilder = new();
                        arguments[i].Emit(irProgram, valueBuilder, typeargs, "NULL");
                        arguments[i].Type.EmitFormatValue(irProgram, emitter, valueBuilder.ToString());
                    }
                }
            }

            foreach (object value in InterpolatedValues)
            {
                if (value is IRValue irValue)
                {
                    if (!irValue.IsPure || irValue.RequiresDisposal(typeargs))
                        bufferedArguments.Add(arguments.Count);

                    formatBuilder.Append(irValue.Type.GetFormatSpecifier(irProgram));
                    arguments.Add(irValue);
                }
                else
#pragma warning disable CS8604 //Interpolated values are always non-null strings or IRValues
                    CharacterLiteral.EmitCString(formatBuilder, value as string, true, false);
#pragma warning restore CS8604 
            }

            emitter.Append($"({{{Type.GetCName(irProgram)} intpd_str{irProgram.ExpressionDepth}; ");
            foreach (int bufferedArg in bufferedArguments)
            {
                emitter.Append($"{arguments[bufferedArg].Type.GetCName(irProgram)} intpd_buffered_arg{bufferedArg}{irProgram.ExpressionDepth} = ");
                arguments[bufferedArg].Emit(irProgram, emitter, typeargs, "NULL");
                emitter.Append(';');
            }

            emitter.Append($"intpd_str{irProgram.ExpressionDepth}.length = snprintf(NULL, 0, \"{formatBuilder.ToString()}\"");
            emitFormatValues();
            emitter.Append($"); intpd_str{irProgram.ExpressionDepth}.buffer = {irProgram.MemoryAnalyzer.Allocate($"intpd_str{irProgram.ExpressionDepth}.length + 1")}; snprintf(intpd_str{irProgram.ExpressionDepth}.buffer, intpd_str{irProgram.ExpressionDepth}.length + 1, \"{formatBuilder.ToString()}\"");
            emitFormatValues();
            emitter.Append(");");

            foreach (int bufferedArg in bufferedArguments)
                if (arguments[bufferedArg].RequiresDisposal(typeargs))
                    arguments[bufferedArg].Type.EmitFreeValue(irProgram, emitter, $"intpd_buffered_arg{bufferedArg}{irProgram.ExpressionDepth}", "NULL");

            emitter.Append($"intpd_str{irProgram.ExpressionDepth};}})");
            irProgram.ExpressionDepth--;
        }
    }
}
