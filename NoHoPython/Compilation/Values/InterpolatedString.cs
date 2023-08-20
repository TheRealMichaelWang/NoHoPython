﻿using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public bool HasFormatSpecifier { get; }

        public string GetFormatSpecifier(IRProgram irProgram);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource);
    }

    partial class Primitive
    {
        public bool HasFormatSpecifier => true;

        public abstract string GetFormatSpecifier(IRProgram irProgram);
        public virtual void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => emitter.Append(valueCSource);
    }

    partial class ArrayType
    {
        public bool HasFormatSpecifier => true;

        public string GetFormatSpecifier(IRProgram irProgram) => ElementType.IsCompatibleWith(Primitive.Character) ? "%.*s" : "%p";

        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource)
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

    partial class MemorySpan
    {
        public bool HasFormatSpecifier => true;

        public string GetFormatSpecifier(IRProgram irProgram) => ElementType.IsCompatibleWith(Primitive.Character) ? $"%.{Length}s" : "%p";

        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource)
        {
            if (!ElementType.IsCompatibleWith(Primitive.Character))
                emitter.Append("(void*)");

            emitter.Append(valueCSource);
        }
    }

    partial class BooleanType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%s";

        public override void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => emitter.Append($"(({valueCSource}) ? \"true\" : \"false\")");
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
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class EnumType
    {
        public bool HasFormatSpecifier => true;

        public string GetFormatSpecifier(IRProgram irProgram) => irProgram.NameRuntimeTypes ? "%s" : throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => emitter.Append(irProgram.NameRuntimeTypes ? $"{GetStandardIdentifier(irProgram)}_typenames[(int){valueCSource}.option]" : throw new InvalidOperationException());
    }

    partial class RecordType
    {
        public bool HasFormatSpecifier => HasProperty("cstr");

        public string GetFormatSpecifier(IRProgram irProgram) => HasFormatSpecifier ? "%s" : throw new NoFormatSpecifierForType(this);
        
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource)
        {
            if (HasFormatSpecifier)
                emitter.Append($"(char*){valueCSource}->cstr");
            else
                throw new InvalidOperationException();
        }
    }

    partial class InterfaceType
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class ForeignCType
    {
        public bool HasFormatSpecifier => Declaration.FormatSpecifier != null;

        public string GetFormatSpecifier(IRProgram irProgram) => Declaration.FormatSpecifier ?? throw new NoFormatSpecifierForType(this);
        
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource)
        {
            if (Declaration.Formatter == null)
                throw new InvalidOperationException();

            emitter.Append(GetSource(Declaration.Formatter, irProgram, valueCSource));
        }
    }

    partial class TupleType
    {
        public bool HasFormatSpecifier => orderedValueTypes.All((type) => type.HasFormatSpecifier);

        public string GetFormatSpecifier(IRProgram irProgram) => $"({string.Join(", ", orderedValueTypes.ConvertAll((type) => type.GetFormatSpecifier(irProgram)))})";

        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource)
        {
            Property[] properties = this.properties.Values.ToArray();
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    emitter.Append(", ");
                properties[i].Type.EmitFormatValue(irProgram, emitter, $"{valueCSource}.{properties[i].Name}");
            }
        }
    }

    partial class ProcedureType
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class NothingType
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => throw new InvalidOperationException();
    }

    partial class TypeParameterReference
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, IEmitter emitter, string valueCSource) => throw new InvalidOperationException();
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

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => TargetArrayChar;

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            if (!irProgram.EmitExpressionStatements)
                throw new CannotEmitInterpolatedString(this);

            irProgram.ExpressionDepth++;
            BufferedEmitter formatBuilder = new();
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
                        arguments[i].Type.EmitFormatValue(irProgram, emitter, BufferedEmitter.EmitBufferedValue(arguments[i], irProgram, typeargs, "NULl"));
                }
            }

            foreach (object value in InterpolatedValues)
            {
                if (value is IRValue irValue)
                {
                    if (!irValue.IsPure || irValue.RequiresDisposal(typeargs, true))
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
                arguments[bufferedArg].Emit(irProgram, emitter, typeargs, "NULL", true);
                emitter.Append(';');
            }

            if (TargetArrayChar)
                emitter.Append($"intpd_str{irProgram.ExpressionDepth}.length");
            else
                emitter.Append("int length");
            emitter.Append($" = snprintf(NULL, 0, \"{formatBuilder}\"");
            emitFormatValues();
            emitter.Append(");");

            if (TargetArrayChar)
                emitter.Append($"intpd_str{irProgram.ExpressionDepth}.buffer = {irProgram.MemoryAnalyzer.Allocate($"intpd_str{irProgram.ExpressionDepth}.length + 1")}; snprintf(intpd_str{irProgram.ExpressionDepth}.buffer, intpd_str{irProgram.ExpressionDepth}.length + 1, \"{formatBuilder}\"");
            else
                emitter.Append($"intpd_str{irProgram.ExpressionDepth} = malloc(length + 1); snprintf(intpd_str{irProgram.ExpressionDepth}, length + 1 + 1, \"{formatBuilder}\"");
            emitFormatValues();
            emitter.Append(");");

            foreach (int bufferedArg in bufferedArguments)
                if (arguments[bufferedArg].RequiresDisposal(typeargs, true))
                    arguments[bufferedArg].Type.EmitFreeValue(irProgram, emitter, $"intpd_buffered_arg{bufferedArg}{irProgram.ExpressionDepth}", "NULL");

            emitter.Append($"intpd_str{irProgram.ExpressionDepth};}})");
            irProgram.ExpressionDepth--;
        }
    }
}
