using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<InterfaceType> usedInterfaceTypes = new(new ITypeComparer());
        private Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverloads = new();

        public bool DeclareUsedInterfaceType(InterfaceType interfaceType)
        {
            if (usedInterfaceTypes.Contains(interfaceType))
                return false;

            usedInterfaceTypes.Add(interfaceType);
            if (!interfaceTypeOverloads.ContainsKey(interfaceType.InterfaceDeclaration))
                interfaceTypeOverloads.Add(interfaceType.InterfaceDeclaration, new List<InterfaceType>());
            interfaceTypeOverloads[interfaceType.InterfaceDeclaration].Add(interfaceType);

            DeclareTypeDependencies(interfaceType, interfaceType.GetProperties().ConvertAll((prop) => prop.Type).ToArray());
            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public readonly Dictionary<InterfaceDeclaration, List<InterfaceType>> InterfaceTypeOverloads;
        
        public void ForwardDeclareInterfaceTypes(Emitter emitter)
        {
            foreach(InterfaceDeclaration interfaceDeclaration in InterfaceTypeOverloads.Keys)
            {
                if(interfaceDeclaration.EmitMultipleCStructs)
                {
                    foreach(InterfaceType interfaceType in InterfaceTypeOverloads[interfaceDeclaration])
                        emitter.AppendLine($"typedef struct {interfaceType.GetStandardIdentifier(this)} {interfaceType.GetCName(this)};");
                }
                else
                    emitter.AppendLine($"typedef struct {InterfaceTypeOverloads[interfaceDeclaration].First().GetStandardIdentifier(this)} {InterfaceTypeOverloads[interfaceDeclaration].First().GetCName(this)};");
            }
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class InterfaceDeclaration
    {
        partial class InterfaceProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
            {
                Debug.Assert(propertyContainer is InterfaceType);
                value(emitter);
                emitter.Append($".{Name}");
                return false;
            }
        }

#pragma warning disable CS8604 // Possible null reference argument.
        public bool EmitMultipleCStructs => requiredImplementedProperties.Any((property) => property.Type.TypeParameterAffectsCodegen(new(new ITypeComparer())));
#pragma warning restore CS8604 // Possible null reference argument.

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            if (EmitMultipleCStructs)
            {
                foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
                    interfaceType.EmitCStruct(irProgram, emitter);
            }
            else
            {
                foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
                {
                    if (interfaceType == irProgram.InterfaceTypeOverloads[this].First())
                    {
                        if (!irProgram.DeclareCompiledType(emitter, interfaceType))
                            return;

                        interfaceType.EmitCStructImmediatley(irProgram, emitter);
                    }
                    else
                        irProgram.DeclareCompiledType(emitter, interfaceType);
                }
            }
        }

        public void ForwardDeclare(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
            {
                emitter.AppendLine($"void free_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface, void* child_agent);");
                
                emitter.Append($"{interfaceType.GetCName(irProgram)} copy_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface");
                if (interfaceType.MustSetResponsibleDestroyer)
                    emitter.Append(", void* responsibleDestroyer");
                emitter.AppendLine(");");

                if (!EmitMultipleCStructs)
                    return;
            }
        }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
            {
                interfaceType.EmitDestructor(irProgram, primaryEmitter);
                interfaceType.EmitCopier(irProgram, primaryEmitter);

                if (!EmitMultipleCStructs)
                    return;
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class InterfaceType
    {
        public bool IsNativeCType => false;
        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => requiredImplementedProperties.Value.Any((property) => property.Type.MustSetResponsibleDestroyer);
        public bool IsTypeDependency => true;

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => requiredImplementedProperties.Value.Any((property) => property.Type.TypeParameterAffectsCodegen(effectInfo));

        public string GetStandardIdentifier(IRProgram irProgram) => InterfaceDeclaration.EmitMultipleCStructs ? $"nhp_interface_{IScopeSymbol.GetAbsolouteName(InterfaceDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}" : $"nhp_interface_{IScopeSymbol.GetAbsolouteName(InterfaceDeclaration)}";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public string? GetInvalidState() => null;
        public Emitter.SetPromise? IsInvalid(Emitter emitter) => null;

        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            emitter.Append($"free_interface{GetStandardIdentifier(irProgram)}(");
            valuePromise(emitter);
            emitter.Append(", ");
            childAgent(emitter);
            emitter.AppendLine(");");
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer, IRElement? errorReportedElement)
        {
            emitter.Append($"copy_interface{GetStandardIdentifier(irProgram)}(");
            valueCSource(emitter);
            if (MustSetResponsibleDestroyer) {
                emitter.Append(", ");
                responsibleDestroyer(emitter);
            }
            emitter.Append(')');
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer, null);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord, null);

        public void EmitMarshallerHeader(IRProgram irProgram, Emitter emitter) => emitter.Append($"{GetCName(irProgram)} marshal_interface{GetStandardIdentifier(irProgram)}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName(irProgram)} {prop.Name}"))})");

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedInterfaceType(this))
            {
                foreach (var property in requiredImplementedProperties.Value)
                    property.Type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            if (InterfaceDeclaration.EmitMultipleCStructs)
            {
                InterfaceDeclaration.ForwardDeclareType(irProgram, emitter);
                return;
            }

            EmitCStructImmediatley(irProgram, emitter);
        }

        public void EmitCStructImmediatley(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            foreach (var property in requiredImplementedProperties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitDestructor(IRProgram irProgram, Emitter emitter)
        {
            emitter.AppendStartBlock($"void free_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface, void* child_agent)");

            foreach (var property in requiredImplementedProperties.Value)
                if (property.Type.RequiresDisposal)
                    property.Type.EmitFreeValue(irProgram, emitter, (e) => e.Append($"interface.{property.Name}"), (e) => e.Append("child_agent"));

            emitter.AppendEndBlock();
        }

        public void EmitCopier(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsibleDestroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_interface;");
            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tcopied_interface.{property.Name} = ");
                property.Type.EmitCopyValue(irProgram, emitter, (e) => e.Append($"interface.{property.Name}"), (e) => e.Append("responsibleDestroyer"), null);
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn copied_interface;");
            emitter.AppendLine("}");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoInterface
    {
        IPropertyContainer? propertyContainer = null;

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Value.RequiresDisposal(irProgram, typeargs, isTemporaryEval) || propertyContainer.GetProperties().Any((prop) => prop.RequiresDisposal(typeargs));
#pragma warning restore CS8602

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);

            InterfaceType realPrototype = (InterfaceType)TargetType.SubstituteWithTypearg(typeargs);
            List<Property> properties = realPrototype.GetProperties();
            propertyContainer = (IPropertyContainer)Value.Type.SubstituteWithTypearg(typeargs);
            foreach (Property property in properties)
                propertyContainer.FindProperty(property.Name).ScopeForUse(false, typeargs, irBuilder);
        }

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => RequiresDisposal(irProgram, typeargs, isTemporaryEval) || Value.IsPure || Value.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            InterfaceType realPrototype = (InterfaceType)TargetType.SubstituteWithTypearg(typeargs);

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} value{indirection}; {realPrototype.GetCName(irProgram)} result{indirection};");
                primaryEmitter.SetArgument(Value, $"value{indirection}", irProgram, typeargs, isTemporaryEval);

                foreach(Property property in realPrototype.GetProperties())
                {
                    primaryEmitter.Append($"result{indirection}.{property.Name} = ");

#pragma warning disable CS8602 // property container set in scope for used types
                    Property accessProperty = propertyContainer.FindProperty(property.Name);
                    if (RequiresDisposal(irProgram, typeargs, isTemporaryEval) && !accessProperty.RequiresDisposal(typeargs))
                        property.Type.EmitCopyValue(irProgram, primaryEmitter, (emitter) => accessProperty.EmitGet(irProgram, emitter, typeargs, propertyContainer, (e) => e.Append($"value{indirection}"), Emitter.NullPromise, this), responsibleDestroyer, Value);
                    else
                        accessProperty.EmitGet(irProgram, primaryEmitter, typeargs, propertyContainer, (emitter) => emitter.Append($"value{indirection}"), responsibleDestroyer, this);
                    primaryEmitter.AppendLine(';');
#pragma warning restore CS8602
                }

                primaryEmitter.DestroyBlockResources();

                destination((emitter) => emitter.Append($"result{indirection}"));

                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) => Value.Emit(irProgram, emitter, typeargs, (valuePromise) =>
                {
                    emitter.Append($"({realPrototype.GetCName(irProgram)}){{");
                    foreach(Property property in realPrototype.GetProperties())
                    {
                        if (property != realPrototype.GetProperties().First())
                            emitter.Append(", ");

                        emitter.Append($".{property.Name} = ");
#pragma warning disable CS8604 // Possible null reference argument.
                        property.EmitGet(irProgram, emitter, typeargs, propertyContainer, valuePromise, responsibleDestroyer, this);
#pragma warning restore CS8604 // Possible null reference argument.
                    }
                    emitter.Append('}');
                }, Emitter.NullPromise, isTemporaryEval));
        }
    }
}