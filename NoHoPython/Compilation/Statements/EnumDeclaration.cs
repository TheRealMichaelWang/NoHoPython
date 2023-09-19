using NoHoPython.Compilation;
using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private List<EnumType> usedEnumTypes = new();
        private Dictionary<EnumDeclaration, List<EnumType>> enumTypeOverloads = new();
        
        public bool DeclareUsedEnumType(EnumType enumType)
        {
            foreach (EnumType usedEnum in usedEnumTypes)
                if (enumType.IsCompatibleWith(usedEnum))
                    return false;

            usedEnumTypes.Add(enumType);
            if (!enumTypeOverloads.ContainsKey(enumType.EnumDeclaration))
                enumTypeOverloads.Add(enumType.EnumDeclaration, new List<EnumType>());
            enumTypeOverloads[enumType.EnumDeclaration].Add(enumType);

            DeclareTypeDependencies(enumType, enumType.GetOptions().ToArray());
            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public readonly Dictionary<EnumDeclaration, List<EnumType>> EnumTypeOverloads;

        public void ForwardDeclareEnumTypes(Emitter emitter)
        {
            foreach(EnumDeclaration enumDeclaration in EnumTypeOverloads.Keys)
            {
                if (enumDeclaration.IsCEnum)
                    continue;

                enumDeclaration.EmitOptionsCEnum(this, emitter);

                if (enumDeclaration.EmitMultipleCStructs)
                {
                    foreach (EnumType enumType in EnumTypeOverloads[enumDeclaration])
                        emitter.AppendLine($"typedef struct {enumType.GetStandardIdentifier(this)} {enumType.GetCName(this)};");
                }
                else
                    emitter.AppendLine($"typedef struct {EnumTypeOverloads[enumDeclaration].First().GetStandardIdentifier(this)} {EnumTypeOverloads[enumDeclaration].First().GetCName(this)};");
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class EnumDeclaration
    {
#pragma warning disable CS8602 // Options already linked
#pragma warning disable CS8604
        public bool IsEmpty => options.TrueForAll((option) => option.IsEmpty);
        public bool EmitMultipleCStructs => options.Any((option) => option.TypeParameterAffectsCodegen(new(new ITypeComparer())));
        public bool IsCEnum => Attributes.ContainsKey("CEnum") && Attributes["CEnum"] != null && IsEmpty;
#pragma warning restore CS8604
#pragma warning restore CS8602

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public string GetCEnumOptionForType(IRProgram irProgram, IType type)
        {
            if (IsCEnum)
            {
                Debug.Assert(type is EmptyEnumOption);
                EmptyEnumOption emptyEnumOption = (EmptyEnumOption)type;
                Debug.Assert(emptyEnumOption.EnumDeclaration == this);

                if (Attributes.ContainsKey("CPrefix") && Attributes["CPrefix"] != null)
                    return $"{Attributes["CPrefix"]}{emptyEnumOption.Name}";
                else
                    return emptyEnumOption.Name;
            }

            return $"nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_OPTION_{type.GetStandardIdentifier(irProgram)}";
        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public string GetCEnumOptionForType(IRProgram irProgram, int optionNumber) => GetCEnumOptionForType(irProgram, options[optionNumber]);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public void ForwardDeclareType(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this) || IsCEnum)
                return;

            if (EmitMultipleCStructs)
            {
                foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
                    enumType.EmitCStruct(irProgram, emitter);
            }
            else
            {
                foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
                {
                    if (enumType == irProgram.EnumTypeOverloads[this].First())
                    {
                        if (!irProgram.DeclareCompiledType(emitter, enumType))
                            return;

                        enumType.EmitCStructImmediatley(irProgram, emitter);
                    }
                    else
                        irProgram.DeclareCompiledType(emitter, enumType);
                }
            }
        }

        public void ForwardDeclare(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this) || IsCEnum)
                return;

            foreach(EnumType usedEnum in irProgram.EnumTypeOverloads[this])
            {
                //usedEnum.EmitMarshallerHeaders(irProgram, emitter);
                usedEnum.EmitPropertyGetHeaders(irProgram, emitter);
                if (usedEnum.RequiresDisposal)
                {
                    emitter.AppendLine($"void free_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} nhp_enum, void* child_agent);");
                    emitter.AppendLine($"{usedEnum.GetCName(irProgram)} copy_enum{usedEnum.GetStandardIdentifier(irProgram)}({usedEnum.GetCName(irProgram)} nhp_enum, void* responsible_destroyer);");
                }

                if (!EmitMultipleCStructs)
                    return;
            }
        }

        public void EmitOptionsCEnum(IRProgram irProgram, Emitter emitter)
        {
            if (IsCEnum)
                return;

            emitter.AppendLine($"typedef enum nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_options {{");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            for (int i = 0; i < options.Count; i++)
            {
                if (i > 0)
                    emitter.AppendLine(",");
                emitter.Append('\t');
                emitter.Append(GetCEnumOptionForType(irProgram, options[i]));
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            emitter.AppendLine();
            emitter.Append("} ");
            emitter.AppendLine($"nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_options_t;");
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this) || IsCEnum)
                return;

            foreach (EnumType enumType in irProgram.EnumTypeOverloads[this])
            {
                //enumType.EmitMarshallers(irProgram, emitter);
                enumType.EmitPropertyGetters(irProgram, primaryEmitter);
                if (enumType.RequiresDisposal)
                {
                    enumType.EmitDestructor(irProgram, primaryEmitter);
                    enumType.EmitCopier(irProgram, primaryEmitter);
                }
                enumType.EmitOptionTypeNames(irProgram, primaryEmitter);

                if (!EmitMultipleCStructs)
                    return;
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class EmptyEnumOption
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => false;
        public bool MustSetResponsibleDestroyer => false;
        public bool IsTypeDependency => throw new InvalidOperationException();

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => false;

        public string GetStandardIdentifier(IRProgram irProgram) => $"nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_empty_option_{Name}";
        public string GetCName(IRProgram irProgram) => throw new CannotCompileEmptyTypeError(null);

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)=> throw new CannotCompileEmptyTypeError(null);
        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCStruct(IRProgram irProgram, Emitter emitter) { }

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder) { }
    }

    partial class EnumType
    {
        partial class EnumProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => EnumType.GetOptions().Any((option) => ((IPropertyContainer)option).FindProperty(Name).RequiresDisposal(typeargs));
            
            public bool IsUsed
            {
                get
                {
                    if(EnumType.EnumDeclaration.EmitMultipleCStructs)
                    {
                        if (typePropertyInUse.ContainsKey(EnumType))
                            return typePropertyInUse[EnumType].Contains(this);
                    }
                    else if(declarationPropertyInUse.ContainsKey(EnumType.EnumDeclaration))
                        return declarationPropertyInUse[EnumType.EnumDeclaration].Contains(this);
                    return false;
                }
            }

            public override void ScopeForUse(bool optimizedMessageRecieverCall, Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
            {
                Debug.Assert(!optimizedMessageRecieverCall);

                EnumType enumType = (EnumType)EnumType.SubstituteWithTypearg(typeargs);

                foreach(IType type in globalSupportedOptions[enumType].Value.Keys)
                {
                    Property accessProperty = ((IPropertyContainer)type).FindProperty(Name);
                    accessProperty.ScopeForUse(false, typeargs, irBuilder);
                }

                if (!declarationPropertyInUse.ContainsKey(enumType.EnumDeclaration))
                    declarationPropertyInUse.Add(enumType.EnumDeclaration, new());
                declarationPropertyInUse[enumType.EnumDeclaration].Add(this);

                if (!typePropertyInUse.ContainsKey(enumType))
                    typePropertyInUse.Add(enumType, new());
                typePropertyInUse[enumType].Add(this);
            }

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer)
            {
                Debug.Assert(propertyContainer is EnumType enumType && EnumType.IsCompatibleWith(enumType));

                emitter.Append($"get_{Name}{EnumType.GetStandardIdentifier(irProgram)}(");
                value(emitter);
                if (RequiresDisposal(typeargs))
                    emitter.Append($", {responsibleDestroyer}");
                emitter.Append(')');

                return false;
            }
        }

        private static Dictionary<EnumDeclaration, HashSet<EnumProperty>> declarationPropertyInUse = new();
        private static Dictionary<EnumType, HashSet<EnumProperty>> typePropertyInUse = new(new ITypeComparer());

        public bool RequiresDisposal => globalSupportedOptions[this].Value.Keys.Any((option) => option.RequiresDisposal);
        public bool MustSetResponsibleDestroyer => globalSupportedOptions[this].Value.Keys.Any((option) => option.MustSetResponsibleDestroyer);
        public bool IsTypeDependency => false;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => globalSupportedOptions[this].Value.Keys.Any((option) => option.TypeParameterAffectsCodegen(effectInfo));

        public string GetStandardIdentifier(IRProgram irProgram) => EnumDeclaration.EmitMultipleCStructs ? $"nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}" : $"nhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}";

        public string GetCName(IRProgram irProgram)
        {
            if (EnumDeclaration.IsCEnum)
#pragma warning disable CS8603 //Null checked in IsCEnum
                return EnumDeclaration.Attributes["CEnum"];
#pragma warning restore CS8603
            else if (EnumDeclaration.IsEmpty)
                return $"{GetStandardIdentifier(irProgram)}_options_t";
            return $"{GetStandardIdentifier(irProgram)}_t";
        }

        public string GetCEnumOptionForType(IRProgram irProgram, IType type) => EnumDeclaration.GetCEnumOptionForType(irProgram, globalSupportedOptions[this].Value[type]);

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            if (RequiresDisposal)
            {
                emitter.Append($"free_enum{GetStandardIdentifier(irProgram)}(");
                valuePromise(emitter);
                emitter.Append(", ");
                childAgent(emitter);
                emitter.AppendLine(");");
            }
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter primaryEmitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)
        {
            if (RequiresDisposal)
            {
                primaryEmitter.Append($"copy_enum{GetStandardIdentifier(irProgram)}(");
                valueCSource(primaryEmitter);
                if (MustSetResponsibleDestroyer)
                {
                    primaryEmitter.Append(", ");
                    responsibleDestroyer(primaryEmitter);
                }
                primaryEmitter.Append(')');
            }
            else
                valueCSource(primaryEmitter);
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedEnumType(this))
            {
                foreach (IType options in globalSupportedOptions[this].Value.Keys)
                    options.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this) || EnumDeclaration.IsEmpty)
                return;

            if(!EnumDeclaration.EmitMultipleCStructs)
            {
                EnumDeclaration.ForwardDeclareType(irProgram, emitter);
                return;
            }

            EmitCStructImmediatley(irProgram, emitter);
        }

        public void EmitCStructImmediatley(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            emitter.AppendLine($"\tnhp_enum_{IScopeSymbol.GetAbsolouteName(EnumDeclaration)}_options_t option;");

            emitter.AppendLine($"\tunion {GetStandardIdentifier(irProgram)}_data {{");
            foreach (IType option in globalSupportedOptions[this].Value.Keys)
            {
                if (!option.IsEmpty)
                    emitter.AppendLine($"\t\t{option.GetCName(irProgram)} {option.GetStandardIdentifier(irProgram)}_set;");
            }
            emitter.AppendLine("\t} data;");

            emitter.AppendLine("};");
        }

        public void EmitPropertyGetHeaders(IRProgram irProgram, Emitter emitter)
        {
            foreach (EnumProperty property in globalSupportedProperties[this].Value.Values)
            {
                if (property.IsUsed)
                {
                    emitter.Append($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} nhp_enum");
                    if (property.RequiresDisposal(typeargMap.Value))
                        emitter.Append(", void* responsible_destroyer");
                    emitter.AppendLine(");");
                }
            }
        }

        public void EmitPropertyGetters(IRProgram irProgram, Emitter emitter)
        {
            foreach (EnumProperty property in globalSupportedProperties[this].Value.Values)
            {
                if (property.IsUsed)
                {
                    emitter.Append($"{property.Type.GetCName(irProgram)} get_{property.Name}{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} nhp_enum");
                    if (property.RequiresDisposal(typeargMap.Value))
                        emitter.Append(", void* responsible_destroyer");
                    emitter.AppendLine(") {");

                    emitter.AppendLine("\tswitch(nhp_enum.option) {");
                    foreach (KeyValuePair<IType, int> option in globalSupportedOptions[this].Value)
                    {
                        emitter.AppendLine($"\tcase {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                        emitter.Append("\t\treturn ");

                        IPropertyContainer propertyContainer = (IPropertyContainer)option.Key;
                        Property accessProperty = propertyContainer.FindProperty(property.Name);

                        if (!accessProperty.RequiresDisposal(typeargMap.Value) && property.RequiresDisposal(typeargMap.Value))
                        {
                            property.Type.EmitCopyValue(irProgram, emitter, (e) =>
                            {
                                accessProperty.EmitGet(irProgram, e, typeargMap.Value, propertyContainer, (k) => k.Append($"nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set"), (k) => k.Append("responsible_destroyer"));
                            }, (k) => k.Append("responsible_destroyer"));
                        }
                        else
                        {
                            accessProperty.EmitGet(irProgram, emitter, typeargMap.Value, propertyContainer, (e) => e.Append($"nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set"), (e) => e.Append("responsible_destroyer"));
                        }
                        emitter.AppendLine(";");
                    }
                    emitter.AppendLine("\t}");
                    emitter.AppendLine("}");
                }
            }
        }

        public void EmitDestructor(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine($"void free_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} nhp_enum, void* child_agent) {{");
            emitter.AppendLine("\tswitch(nhp_enum.option) {");

            foreach(KeyValuePair<IType, int> option in globalSupportedOptions[this].Value)
                if (option.Key.RequiresDisposal)
                {
                    emitter.AppendLine($"\tcase {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                    emitter.Append("\t\t");
                    option.Key.EmitFreeValue(irProgram, emitter, (e) => e.Append($"nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set"), (e) => e.Append("child_agent"));
                    emitter.AppendLine();
                    emitter.AppendLine("\t\tbreak;");
                }

            emitter.AppendLine("\t}");
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} nhp_enum");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_enum;");
            emitter.AppendLine("\tcopied_enum.option = nhp_enum.option;");

            emitter.AppendLine("\tswitch(nhp_enum.option) {");
            foreach (KeyValuePair<IType, int> option in globalSupportedOptions[this].Value)
                if (!option.Key.IsEmpty)
                {
                    emitter.AppendLine($"\tcase {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                    emitter.Append($"\t\tcopied_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set = ");
                    option.Key.EmitCopyValue(irProgram, emitter, (e) => e.Append($"nhp_enum.data.{option.Key.GetStandardIdentifier(irProgram)}_set"), (e) => e.Append("responsible_destroyer"));
                    emitter.AppendLine(";");
                    emitter.AppendLine("\t\tbreak;");
                }
            emitter.AppendLine("\t}");
            
            emitter.AppendLine("\treturn copied_enum;");
            emitter.AppendLine("}");
        }

        public void EmitOptionTypeNames(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.NameRuntimeTypes || EnumDeclaration.IsCEnum)
                return;

            emitter.Append($"static const char* {GetStandardIdentifier(irProgram)}_typenames[] = {{");

            foreach (IType option in globalSupportedOptions[this].Value.Keys)
            {
                emitter.Append($"\t\"{option.TypeName}\"");
                if (option != globalSupportedOptions[this].Value.Keys.Last())
                    emitter.Append(',');
                emitter.AppendLine();
            }
            emitter.AppendLine("};");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoEnum
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Value.RequiresDisposal(irProgram, typeargs, isTemporaryEval);

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => !Value.Type.IsEmpty && Value.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            EnumType realPrototype = (EnumType)TargetType.SubstituteWithTypearg(typeargs);

            if (realPrototype.EnumDeclaration.IsEmpty)
                destination((emitter) => emitter.Append(realPrototype.GetCEnumOptionForType(irProgram, Value.Type)));
            else if (Value.Type.IsEmpty)
                destination((emitter) => emitter.Append($"(({realPrototype.GetCName(irProgram)}) {{.option = {realPrototype.GetCEnumOptionForType(irProgram, Value.Type.SubstituteWithTypearg(typeargs))}}})"));
            else
                Value.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) => destination((emitter) => 
                {
                    emitter.Append($"(({realPrototype.GetCName(irProgram)}) {{.option = {realPrototype.GetCEnumOptionForType(irProgram, Value.Type.SubstituteWithTypearg(typeargs))}, .data = {{ .{Value.Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_set = ");
                    valuePromise(emitter);
                    emitter.Append("}})");
                }), responsibleDestroyer, isTemporaryEval);
        }
    }

    partial class UnwrapEnumValue
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => EnumValue.RequiresDisposal(irProgram, typeargs, isTemporaryEval);

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            EnumValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            EnumType enumType = (EnumType)EnumValue.Type.SubstituteWithTypearg(typeargs);

            int indirection = primaryEmitter.AppendStartBlock();
            primaryEmitter.AppendLine($"{enumType.GetCName(irProgram)} enum{indirection};");
            EnumValue.Emit(irProgram, primaryEmitter, typeargs, (enumValuePromise) =>
            {
                primaryEmitter.Append($"enum{indirection} = ");
                enumValuePromise(primaryEmitter);
                primaryEmitter.AppendLine(';');
            }, responsibleDestroyer, isTemporaryEval);

            primaryEmitter.AppendStartBlock($"if(enum{indirection}.option != {enumType.GetCEnumOptionForType(irProgram, Type.SubstituteWithTypearg(typeargs))})");
            if (irProgram.DoCallStack)
            {
                CallStackReporting.EmitErrorLoc(primaryEmitter, ErrorReportedElement);
                CallStackReporting.EmitPrintStackTrace(primaryEmitter);
                if (irProgram.NameRuntimeTypes)
                {
                    primaryEmitter.Append("printf(\"Unwrapping Error: Expected ");
                    ErrorReportedElement.EmitSrcAsCString(primaryEmitter, true, false);
                    primaryEmitter.Append($"to be {Type.SubstituteWithTypearg(typeargs).TypeName}, but got %s instead.\\n\", {enumType.GetStandardIdentifier(irProgram)}_typenames[(int)enum{indirection}.option]);");
                }
                else
                {
                    primaryEmitter.Append("puts(\"Unwrapping Error: ");
                    ErrorReportedElement.EmitSrcAsCString(primaryEmitter, false, false);
                    primaryEmitter.Append(" failed.\");");
                }
            }
            else
            {
                if (irProgram.NameRuntimeTypes)
                {
                    primaryEmitter.Append($"printf(\"Failed to unwrap {Type.SubstituteWithTypearg(typeargs).TypeName} from ");
                    CharacterLiteral.EmitCString(primaryEmitter, ErrorReportedElement.SourceLocation.ToString(), true, false);
                    primaryEmitter.Append($", got %s instead.\\n\", {enumType.GetStandardIdentifier(irProgram)}_typenames[(int)enum{indirection}.option]); ");
                }
                else
                {
                    primaryEmitter.Append("puts(\"Failed to unwrap enum from value, ");
                    CharacterLiteral.EmitCString(primaryEmitter, ErrorReportedElement.SourceLocation.ToString(), false, false);
                    primaryEmitter.Append(".\\n\\t");
                    ErrorReportedElement.EmitSrcAsCString(primaryEmitter, true, false);
                    primaryEmitter.Append("\");");
                }
            }
            primaryEmitter.AppendLine("abort();");
            primaryEmitter.AppendEndBlock();
            
            destination((emitter) => emitter.Append($"enum{indirection}.data.{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}_set"));
            primaryEmitter.AppendEndBlock();
        }
    }

    partial class CheckEnumOption
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Option.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            EnumValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => EnumValue.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            EnumType enumType = (EnumType)EnumValue.Type.SubstituteWithTypearg(typeargs);

            EnumValue.Emit(irProgram, primaryEmitter, typeargs, (enumValuePromise) => destination((emitter) =>
            {
                emitter.Append('(');
                enumValuePromise(emitter);
                if (!enumType.EnumDeclaration.IsEmpty)
                    emitter.Append(".option");
                emitter.Append(" == ");
                emitter.Append(enumType.GetCEnumOptionForType(irProgram, Option.SubstituteWithTypearg(typeargs)));
                emitter.Append(')');
            }), Emitter.NullPromise, true);
        }
    }
}