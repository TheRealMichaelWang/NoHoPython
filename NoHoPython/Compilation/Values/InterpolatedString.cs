using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;

namespace NoHoPython.Typing
{
    partial interface IType
    {
        public bool HasFormatSpecifier { get; }

        public string GetFormatSpecifier(IRProgram irProgram);
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value);
    }

    partial class Primitive
    {
        public bool HasFormatSpecifier => true;

        public abstract string GetFormatSpecifier(IRProgram irProgram);
        public virtual void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value) => value(emitter);
    }

    partial class ArrayType
    {
        public bool HasFormatSpecifier => true;

        public string GetFormatSpecifier(IRProgram irProgram) => ElementType.IsCompatibleWith(Primitive.Character) ? "%.*s" : "%p";

        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            if (!ElementType.IsCompatibleWith(Primitive.Character))
                emitter.Append("(void*)");

            value(emitter);
            if (ElementType.IsCompatibleWith(Primitive.Character))
            {
                emitter.Append(".length, ");
                value(emitter);
            }
            emitter.Append(".buffer");
        }
    }

    partial class MemorySpan
    {
        public bool HasFormatSpecifier => true;

        public string GetFormatSpecifier(IRProgram irProgram) => ElementType.IsCompatibleWith(Primitive.Character) ? $"%.{Length}s" : "%p";

        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            if (!ElementType.IsCompatibleWith(Primitive.Character))
                emitter.Append("(void*)");

            value(emitter);
        }
    }

    partial class BooleanType
    {
        public override string GetFormatSpecifier(IRProgram irProgram) => "%s";

        public override void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            emitter.Append("((");
            value(emitter);
            emitter.Append(") ? \"true\" : \"false\")");
        }
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
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value) => throw new InvalidOperationException();
    }

    partial class EnumType
    {
        public bool HasFormatSpecifier => true;

        public string GetFormatSpecifier(IRProgram irProgram) => irProgram.NameRuntimeTypes ? "%s" : throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            if (irProgram.NameRuntimeTypes)
                throw new InvalidOperationException();

            emitter.Append($"{GetStandardIdentifier(irProgram)}_typenames[(int)");
            value(emitter);
            emitter.Append(".option]");
        }
    }

    partial class RecordType
    {
        public bool HasFormatSpecifier => HasProperty("cstr") && identifierPropertyMap.Value["cstr"].Type.IsCompatibleWith(new HandleType(Primitive.Character));

        public string GetFormatSpecifier(IRProgram irProgram) => HasFormatSpecifier ? "%s" : throw new NoFormatSpecifierForType(this);
        
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            if (!HasFormatSpecifier)
                throw new InvalidOperationException();

            value(emitter);
            emitter.Append("->cstr");
        }
    }

    partial class InterfaceType
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value) => throw new InvalidOperationException();
    }

    partial class ForeignCType
    {
        public bool HasFormatSpecifier => Declaration.FormatSpecifier != null;

        public string GetFormatSpecifier(IRProgram irProgram) => Declaration.FormatSpecifier ?? throw new NoFormatSpecifierForType(this);
        
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            if (Declaration.Formatter == null)
                throw new InvalidOperationException();

            emitter.Append(GetSource(Declaration.Formatter, irProgram, value));
        }
    }

    partial class TupleType
    {
        public bool HasFormatSpecifier => orderedValueTypes.All((type) => type.HasFormatSpecifier);

        public string GetFormatSpecifier(IRProgram irProgram) => $"({string.Join(", ", orderedValueTypes.ConvertAll((type) => type.GetFormatSpecifier(irProgram)))})";

        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value)
        {
            Property[] properties = this.properties.Values.ToArray();
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                    emitter.Append(", ");
                properties[i].Type.EmitFormatValue(irProgram, emitter, (e) =>
                {
                    value(e);
                    emitter.Append($".{properties[i].Name}");
                });
            }
        }
    }

    partial class ProcedureType
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value) => throw new InvalidOperationException();
    }

    partial class NothingType
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value) => throw new InvalidOperationException();
    }

    partial class TypeParameterReference
    {
        public bool HasFormatSpecifier => false;

        public string GetFormatSpecifier(IRProgram irProgram) => throw new NoFormatSpecifierForType(this);
        public void EmitFormatValue(IRProgram irProgram, Emitter emitter, Emitter.Promise value) => throw new InvalidOperationException();
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

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => TargetArrayChar;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            string format;
            using (Emitter formatBuilder = new Emitter())
            {
                formatBuilder.Append('\"');
                foreach (object value in InterpolatedValues)
                    if (value is IRValue irValue)
                        formatBuilder.Append(irValue.Type.GetFormatSpecifier(irProgram));
                    else
#pragma warning disable CS8604 //Interpolated values are always non-null strings or IRValues
                        CharacterLiteral.EmitCString(formatBuilder, value as string, true, false);
#pragma warning restore CS8604
                formatBuilder.Append('\"');
                format = formatBuilder.GetBuffered();
            }

            int indirection = primaryEmitter.AppendStartBlock();

            void EmitFormatArgs()
            {
                for (int i = 0; i < InterpolatedValues.Count; i++)
                    if (InterpolatedValues[i] is IRValue)
                        primaryEmitter.Append($", arg{i}{indirection}");
                primaryEmitter.AppendLine(");");
            }

            for (int i = 0; i < InterpolatedValues.Count; i++)
                if (InterpolatedValues[i] is IRValue irValue)
                {
                    primaryEmitter.AppendLine($"{irValue.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} arg{i}{indirection};");
                    irValue.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) =>
                    {
                        primaryEmitter.Append($"arg{i}{indirection} = ");
                        valuePromise(primaryEmitter);
                        primaryEmitter.AppendLine(';');
                    }, Emitter.NullPromise, true);
                }

            primaryEmitter.Append($"int count{indirection} = snprintf(NULL, 0, {format}");
            EmitFormatArgs();
            primaryEmitter.AppendLine($"char* cstr{indirection} = {irProgram.MemoryAnalyzer.Allocate($"count{indirection} + 1")};");
            primaryEmitter.Append($"snprintf(cstr{indirection}, count{indirection} + 1, {format}");
            EmitFormatArgs();
            if (TargetArrayChar)
                destination((emitter) => emitter.Append($"({Type.GetCName(irProgram)}){{.buffer = cstr{indirection}, .length=count{indirection}}}"));
            else
                destination((emitter) => emitter.Append($"cstr{indirection}"));

            for (int i = 0; i < InterpolatedValues.Count; i++)
                if (InterpolatedValues[i] is IRValue irValue && irValue.RequiresDisposal(irProgram, typeargs, true))
                    irValue.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, primaryEmitter, (emitter) => emitter.Append($"arg{i}{indirection}"), Emitter.NullPromise);
            primaryEmitter.AppendEndBlock();
        }
    }
}
