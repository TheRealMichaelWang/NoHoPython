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

                if (enumDeclaration.IsEmpty)
                    continue;

                if (enumDeclaration.EmitMultipleCStructs)
                {
                    foreach (EnumType enumType in EnumTypeOverloads[enumDeclaration])
                        if(!(enumType.IsOptionEnum && enumType.GetOptions().First(option => !option.IsEmpty).GetInvalidState() != null))
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
                string optionName = type is EmptyEnumOption emptyEnumOption ? emptyEnumOption.Name : "None";

                if (Attributes.ContainsKey("CPrefix") && Attributes["CPrefix"] != null)
                {
                    if (Attributes.ContainsKey($"{optionName}_CSrc") && Attributes[$"{optionName}_CSrc"] != null)
#pragma warning disable CS8603 // Possible null reference return.
                        return Attributes[$"{optionName}_CSrc"];
#pragma warning restore CS8603 // Possible null reference return.
                    if(Attributes.ContainsKey("UpperCase"))
                        return $"{Attributes["CPrefix"]}{optionName.ToUpper()}";
                    return $"{Attributes["CPrefix"]}{optionName}";
                }
                else
                    return optionName;
            }

            return $"nhp_enum_{IScopeSymbol.GetAbsolouteName(this)}_OPTION_{type.GetStandardIdentifier(irProgram)}";
        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public string GetCEnumOptionForType(IRProgram irProgram, int optionNumber) => GetCEnumOptionForType(irProgram, options[optionNumber]);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public void ForwardDeclareType(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.EnumTypeOverloads.ContainsKey(this) || IsCEnum || IsEmpty)
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
        public string? GetInvalidState() => null;
        public Emitter.SetPromise? IsInvalid(Emitter emitter) => null;

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent) => throw new CannotCompileEmptyTypeError(null);
        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)=> throw new CannotCompileEmptyTypeError(null);
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

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
            {
                Debug.Assert(propertyContainer is EnumType enumType && EnumType.IsCompatibleWith(enumType));

                emitter.Append($"get_{Name}{EnumType.GetStandardIdentifier(irProgram)}(");
                value(emitter);
                if (RequiresDisposal(typeargs))
                {
                    emitter.Append(", ");
                    responsibleDestroyer(emitter);
                }
                emitter.Append(')');

                return false;
            }
        }

        public delegate void MatchPromise(Emitter emitter, IType option, Emitter.Promise optionValuePromise);
        public delegate bool MatchPredicate(IType option);

        private static Dictionary<EnumDeclaration, HashSet<EnumProperty>> declarationPropertyInUse = new();
        private static Dictionary<EnumType, HashSet<EnumProperty>> typePropertyInUse = new(new ITypeComparer());

        public bool RequiresDisposal => globalSupportedOptions[this].Value.Keys.Any((option) => option.RequiresDisposal);
        public bool MustSetResponsibleDestroyer => globalSupportedOptions[this].Value.Keys.Any((option) => option.MustSetResponsibleDestroyer);
        public bool IsTypeDependency => false;
        public bool IsOptionEnum => globalSupportedOptions[this].Value.Count == 2 && globalSupportedOptions[this].Value.Any(option => option.Key.IsEmpty) && globalSupportedOptions[this].Value.Any(option => !option.Key.IsEmpty);

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
            else if (IsOptionEnum)
            {
                IType nonEmptyOption = globalSupportedOptions[this].Value.First(option => !option.Key.IsEmpty).Key;
                if (nonEmptyOption.GetInvalidState() != null)
                    return nonEmptyOption.GetCName(irProgram);
            }
            return $"{GetStandardIdentifier(irProgram)}_t";
        }

        public string? GetInvalidState()
        {
            if (EnumDeclaration.IsCEnum)
            {
                if (EnumDeclaration.Attributes.ContainsKey("InvalidState"))
                    return EnumDeclaration.Attributes["InvalidState"];
                else if (EnumDeclaration.Attributes.ContainsKey("NullState"))
                    return EnumDeclaration.Attributes["NullState"];
            }
            return null;
        }

        public Emitter.SetPromise? IsInvalid(Emitter emitter) => null;

        private string GetCEnumOptionForType(IRProgram irProgram, IType type) => EnumDeclaration.GetCEnumOptionForType(irProgram, globalSupportedOptions[this].Value[type]);

        public void EmitComposeLiteral(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, IType type)
        {
            if (EnumDeclaration.IsEmpty)
            {
                emitter.Append(GetCEnumOptionForType(irProgram, type));
                return;
            }
            if (IsOptionEnum)
            {
                IType nonEmptyOption = globalSupportedOptions[this].Value.First(option => !option.Key.IsEmpty).Key;
                string? csrc = nonEmptyOption.GetInvalidState();
                if (csrc != null)
                {
                    if (type.IsCompatibleWith(nonEmptyOption))
                        valuePromise(emitter);
                    else
                        emitter.Append(csrc);
                    return;
                }
            }
            emitter.Append($"({GetCName(irProgram)}){{ .option = {GetCEnumOptionForType(irProgram, type)}");
            if (!type.IsEmpty)
            {
                emitter.Append($", .data = {{ .{type.GetStandardIdentifier(irProgram)}_set = ");
                valuePromise(emitter);
                emitter.Append('}');
            }
            emitter.Append('}');
        }

        public void EmitAccessElement(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, IType type)
        {
            if (type.IsEmpty)
                EmitComposeLiteral(irProgram, emitter, valuePromise, type);
            else if (IsOptionEnum && type.GetInvalidState() != null)
                valuePromise(emitter);
            else
            {
                valuePromise(emitter);
                emitter.Append($".data.{type.GetStandardIdentifier(irProgram)}_set");
            }
        }

        public void EmitCheckType(IRProgram irProgram, Emitter emitter, Emitter.Promise enumPromise, IType type, bool checkIfNot = false)
        {
            enumPromise(emitter);
            if (IsOptionEnum)
            {
                IType nonEmptyOption = globalSupportedOptions[this].Value.Where(option => !option.Key.IsEmpty).First().Key;
                string? csrc = nonEmptyOption.GetInvalidState();
                if (csrc != null)
                {
                    bool isValue = type.IsCompatibleWith(nonEmptyOption);
                    if ((!checkIfNot && isValue) || (checkIfNot && !isValue))
                        emitter.Append(" != ");
                    else
                        emitter.Append(" == ");
                    emitter.Append(csrc);
                    return;
                }
            }
            if (!EnumDeclaration.IsEmpty)
                emitter.Append(".option");
            emitter.Append(' ');
            if (checkIfNot)
                emitter.Append("!=");
            else
                emitter.Append("==");
            emitter.Append($" {GetCEnumOptionForType(irProgram, type)}");
        }

        public void EmitMatchOptions(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, MatchPromise matchPromise, Emitter.Promise? defaultHandler = null, MatchPredicate? predicate = null, bool allReturnOrBreak = false)
        {
            if (predicate != null && !globalSupportedOptions[this].Value.Keys.Any(option => predicate(option)))
                return;

            if (IsOptionEnum)
            {
                IType nonEmptyOption = globalSupportedOptions[this].Value.Where(option => !option.Key.IsEmpty).First().Key;
                string? csrc = nonEmptyOption.GetInvalidState();
                if(csrc != null)
                {
                    IType emptyOption = globalSupportedOptions[this].Value.Where(option => option.Key.IsEmpty).First().Key;

                    if(predicate != null && (!predicate(emptyOption) || !predicate(nonEmptyOption)))
                    {
                        emitter.Append("if(");
                        valuePromise(emitter);
                        if (!predicate(emptyOption))
                        {
                            emitter.AppendStartBlock($" != {csrc})");
                            matchPromise(emitter, nonEmptyOption, valuePromise);
                        }
                        else if (!predicate(nonEmptyOption))
                        {
                            emitter.AppendStartBlock($" == {csrc})");
                            matchPromise(emitter, emptyOption, valuePromise);
                        }
                        emitter.AppendEndBlock();
                        if(defaultHandler != null)
                        {
                            emitter.AppendStartBlock("else");
                            defaultHandler(emitter);
                            emitter.AppendEndBlock();
                        }
                        return;
                    }

                    emitter.Append("if(");
                    valuePromise(emitter);
                    emitter.AppendStartBlock($" == {csrc})");
                    matchPromise(emitter, emptyOption, valuePromise);
                    emitter.AppendEndBlock();
                    emitter.AppendStartBlock("else");
                    matchPromise(emitter, nonEmptyOption, valuePromise);
                    emitter.AppendEndBlock();
                    return;
                }
            }
            emitter.Append("switch(");
            valuePromise(emitter);
            if (!EnumDeclaration.IsEmpty)
                emitter.Append(".option");
            emitter.AppendStartBlock(")");
            bool uncovered = false;
            foreach (KeyValuePair<IType, int> option in globalSupportedOptions[this].Value) {
                if (predicate != null && !predicate(option.Key))
                {
                    uncovered = true;
                    continue;
                }
                emitter.AppendLine($"case {EnumDeclaration.GetCEnumOptionForType(irProgram, option.Value)}:");
                matchPromise(emitter, option.Key, e =>
                {
                    valuePromise(e);
                    if (option.Key.IsEmpty)
                    {
                        if(!EnumDeclaration.IsEmpty)
                            e.Append(".option");
                        return;
                    }
                    e.Append($".data.{option.Key.GetStandardIdentifier(irProgram)}_set");
                });
                if(!allReturnOrBreak)
                    emitter.AppendLine("break;");
            }
            if(uncovered && defaultHandler != null)
            {
                emitter.AppendLine("default:");
                defaultHandler(emitter);
            }
            emitter.AppendEndBlock();
        }

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

        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
        {
            if (RequiresDisposal)
            {
                emitter.Append($"copy_enum{GetStandardIdentifier(irProgram)}(");
                valueCSource(emitter);
                if (MustSetResponsibleDestroyer)
                {
                    emitter.Append(", ");
                    responsibleDestroyer(emitter);
                }
                emitter.Append(')');
            }
            else
                valueCSource(emitter);
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer, null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord, null);

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
            if (!irProgram.DeclareCompiledType(emitter, this))
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
            if (IsOptionEnum && globalSupportedOptions[this].Value.Where(option => !option.Key.IsEmpty).First().Key.GetInvalidState() != null)
                return;

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
                    emitter.AppendStartBlock(")");

                    EmitMatchOptions(irProgram, emitter, e => e.Append("nhp_enum"), (optionEmitter, option, optionValuePromise) =>
                    {
                        IPropertyContainer propertyContainer = (IPropertyContainer)option;
                        Property accessProperty = propertyContainer.FindProperty(property.Name);
                        optionEmitter.Append("return ");

                        if (!accessProperty.RequiresDisposal(typeargMap.Value) && property.RequiresDisposal(typeargMap.Value))
                        {
                            property.Type.EmitCopyValue(irProgram, optionEmitter, (e) =>
                            {
                                accessProperty.EmitGet(irProgram, e, typeargMap.Value, propertyContainer, optionValuePromise, (k) => k.Append("responsible_destroyer"), null);
                            }, (k) => k.Append("responsible_destroyer"), null);
                        }
                        else
                            accessProperty.EmitGet(irProgram, optionEmitter, typeargMap.Value, propertyContainer, optionValuePromise, (e) => e.Append("responsible_destroyer"), null);

                        optionEmitter.AppendLine(";");
                    }, null, null, true);

                    emitter.AppendEndBlock();
                }
            }
        }

        public void EmitDestructor(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendStartBlock($"void free_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} nhp_enum, void* child_agent)");
            EmitMatchOptions(irProgram, emitter, e => e.Append("nhp_enum"), (optionEmitter, option, optionValuePromise) =>
            {
                option.EmitFreeValue(irProgram, emitter, optionValuePromise, e => e.Append("child_agent"));
                emitter.AppendLine();
            }, null, option => option.RequiresDisposal);
            emitter.AppendEndBlock();
        }

        public void EmitCopier(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_enum{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} nhp_enum");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsible_destroyer");

            emitter.AppendStartBlock(")");

            //emitter.AppendLine($"{GetCName(irProgram)} copied_enum;");
            //emitter.AppendLine("copied_enum.option = nhp_enum.option;");
            EmitMatchOptions(irProgram, emitter, e => e.Append("nhp_enum"), (optionEmitter, option, optionValuePromise) =>
            {
                emitter.Append("return ");
                EmitComposeLiteral(irProgram, emitter, k => option.EmitCopyValue(irProgram, k, optionValuePromise, e => e.Append("responsible_destroyer"), null), option);
                emitter.AppendLine(';');
            }, null, null);
            
            //emitter.AppendLine("return copied_enum;");
            emitter.AppendEndBlock();
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
            Value.Emit(irProgram, primaryEmitter, typeargs, (valuePromise) => destination(emitter =>
            {
                realPrototype.EmitComposeLiteral(irProgram, emitter, valuePromise, Value.Type.SubstituteWithTypearg(typeargs));
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
            IType targetType = Type.SubstituteWithTypearg(typeargs);

            int indirection = primaryEmitter.AppendStartBlock();
            primaryEmitter.AppendLine($"{enumType.GetCName(irProgram)} enum{indirection};");
            primaryEmitter.SetArgument(EnumValue, $"enum{indirection}", irProgram, typeargs, isTemporaryEval, responsibleDestroyer);

            primaryEmitter.Append("if(");
            enumType.EmitCheckType(irProgram, primaryEmitter, e => e.Append($"enum{indirection}"), targetType, true);
            primaryEmitter.AppendStartBlock(")");
            if (ErrorReturnType != null)
            {
                EnumType errorReturn = (EnumType)ErrorReturnType.SubstituteWithTypearg(typeargs);

                enumType.EmitMatchOptions(irProgram, primaryEmitter, e => e.Append($"enum{indirection}"), (optionEmitter, option, optionValuePromise) =>
                {
                    if (option.IsEmpty)
                    {
                        primaryEmitter.DestroyFunctionResources();
                        primaryEmitter.Append("return ");
                    }
                    else
                        primaryEmitter.Append($"{errorReturn.GetCName(irProgram)} toret{indirection} = ");

                    if (errorReturn.SupportsType(option))
                        errorReturn.EmitComposeLiteral(irProgram, optionEmitter, e =>
                        {
                            option.EmitCopyValue(irProgram, e, optionValuePromise, k => k.Append("ret_responsible_det"), null);
                        }, option);
                    else
                        errorReturn.EmitComposeLiteral(irProgram, optionEmitter, Emitter.NullPromise, Primitive.Nothing);

                    if (!option.IsEmpty)
                    {
                        primaryEmitter.AppendLine(';');
                        primaryEmitter.DestroyFunctionResources();
                        primaryEmitter.Append($"return toret{indirection}");
                    }

                    primaryEmitter.AppendLine(';');
                }, null, option => !option.IsCompatibleWith(targetType));
            }
            else
            {
                if (irProgram.DoCallStack)
                {
                    CallStackReporting.EmitErrorLoc(primaryEmitter, ErrorReportedElement);
                    CallStackReporting.EmitPrintStackTrace(primaryEmitter);
                    primaryEmitter.Append("puts(\"Unwrapping Error: Expected ");
                    ErrorReportedElement.EmitSrcAsCString(primaryEmitter, true, false);
                    primaryEmitter.Append($"to be {targetType.TypeName}\");");
                }
                else
                {
                    primaryEmitter.Append($"puts(\"Failed to unwrap {targetType.TypeName} from enum, ");
                    CharacterLiteral.EmitCString(primaryEmitter, ErrorReportedElement.SourceLocation.ToString(), false, false);
                    primaryEmitter.Append(".\\n\\t");
                    ErrorReportedElement.EmitSrcAsCString(primaryEmitter, true, false);
                    primaryEmitter.Append("\");");
                }
                primaryEmitter.AppendLine("abort();");
            }
            primaryEmitter.AppendEndBlock();
            primaryEmitter.AssumeBlockResources();

            destination((emitter) => enumType.EmitAccessElement(irProgram, emitter, e => e.Append($"enum{indirection}"), targetType));
            
            primaryEmitter.AppendEndBlock();
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs) => IRValue.EmitAsStatement(irProgram, primaryEmitter, this, typeargs);
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
                enumType.EmitCheckType(irProgram, emitter, enumValuePromise, Option.SubstituteWithTypearg(typeargs));
            }), Emitter.NullPromise, true);
        }
    }
}