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
        
        public void ForwardDeclareInterfaceTypes(StatementEmitter emitter)
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

            public override bool EmitGet(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, string valueCSource, string responsibleDestroyer)
            {
                Debug.Assert(propertyContainer is InterfaceType);
                emitter.Append($"{valueCSource}.{Name}");
                return false;
            }
        }

#pragma warning disable CS8604 // Possible null reference argument.
        public bool EmitMultipleCStructs => requiredImplementedProperties.Any((property) => property.Type.TypeParameterAffectsCodegen(new(new ITypeComparer())));
#pragma warning restore CS8604 // Possible null reference argument.

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StatementEmitter emitter)
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

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
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

                if (!irProgram.EmitExpressionStatements)
                {
                    interfaceType.EmitMarshallerHeader(irProgram, emitter);
                    emitter.AppendLine(";");
                    emitter.AppendLine($"{interfaceType.GetCName(irProgram)} move_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)}* dest, {interfaceType.GetCName(irProgram)} src, void* child_agent);");
                }

                if (!EmitMultipleCStructs)
                    return;
            }
        }

        public void Emit(IRProgram irProgram, StatementEmitter emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
            {
                interfaceType.EmitMarshaller(irProgram, emitter);
                interfaceType.EmitDestructor(irProgram, emitter);
                interfaceType.EmitCopier(irProgram, emitter);
                interfaceType.EmitMover(irProgram, emitter);

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

        public void EmitFreeValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string childAgent) => emitter.Append($"free_interface{GetStandardIdentifier(irProgram)}({valueCSource}, {childAgent});");
        public void EmitCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer)
        {
            if(MustSetResponsibleDestroyer)
                emitter.Append($"copy_interface{GetStandardIdentifier(irProgram)}({valueCSource}, {responsibleDestroyer})");
            else
                emitter.Append($"copy_interface{GetStandardIdentifier(irProgram)}({valueCSource})");
        }

        public void EmitMoveValue(IRProgram irProgram, IEmitter emitter, string destC, string valueCSource, string childAgent)
        {
            if (irProgram.EmitExpressionStatements)
                IType.EmitMove(this, irProgram, emitter, destC, valueCSource, childAgent);
            else
                emitter.Append($"move_interface{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource}, {childAgent})");
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, IEmitter emitter, string valueCSource, string newRecordCSource) => EmitCopyValue(irProgram, emitter, valueCSource, newRecordCSource);

        public void EmitMarshallerHeader(IRProgram irProgram, StatementEmitter emitter) => emitter.Append($"{GetCName(irProgram)} marshal_interface{GetStandardIdentifier(irProgram)}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName(irProgram)} {prop.Name}"))})");

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedInterfaceType(this))
            {
                foreach (var property in requiredImplementedProperties.Value)
                    property.Type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, StatementEmitter emitter)
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

        public void EmitCStructImmediatley(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            foreach (var property in requiredImplementedProperties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            EmitMarshallerHeader(irProgram, emitter);
            emitter.AppendLine(" {");

            emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_interface;");
            foreach (var property in requiredImplementedProperties.Value)
                emitter.AppendLine($"\tmarshalled_interface.{property.Name} = {property.Name};");
            
            emitter.AppendLine("\treturn marshalled_interface;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.AppendLine($"void free_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface, void* child_agent) {{");

            foreach (var property in requiredImplementedProperties.Value)
                if (property.Type.RequiresDisposal)
                {
                    emitter.Append('\t');
                    property.Type.EmitFreeValue(irProgram, emitter, $"interface.{property.Name}", "child_agent");
                    emitter.AppendLine();
                }
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} copy_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsibleDestroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} copied_interface;");
            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tcopied_interface.{property.Name} = ");
                property.Type.EmitCopyValue(irProgram, emitter, $"interface.{property.Name}", "responsibleDestroyer");
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn copied_interface;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StatementEmitter emitter)
        {
            if (irProgram.EmitExpressionStatements)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} move_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src, void* child_agent) {{");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "*dest", "child_agent");
            emitter.AppendLine();
            emitter.AppendLine($"\t*dest = src;");
            emitter.AppendLine("\treturn src;");
            emitter.AppendLine("}");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoInterface
    {
        IPropertyContainer? propertyContainer = null;

        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

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

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer, bool isTemporaryEval)
        {
            InterfaceType realPrototype = (InterfaceType)TargetType.SubstituteWithTypearg(typeargs);

            if (Value.RequiresDisposal(typeargs, true))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEmitDestructorError(Value);
                
                irProgram.ExpressionDepth++;
                emitter.Append($"({{{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} nhp_marshal_buf{irProgram.ExpressionDepth} = ");
                Value.Emit(irProgram, emitter, typeargs, "NULL", true);
                emitter.Append(';');
            }

            List<Property> properties = realPrototype.GetProperties();
            List<string> emittedValues = new(properties.Count);

            bool firstEmit = true;
            foreach (Property property in properties)
            {
                string valueEmitter;
                if (Value.RequiresDisposal(typeargs, true))
                    valueEmitter = $"nhp_marshal_buf{irProgram.ExpressionDepth}";
                else if (firstEmit)
                    valueEmitter = BufferedEmitter.EmittedBufferedMemorySafe(Value, irProgram, typeargs);
                else
                {
                    valueEmitter = BufferedEmitter.EmittedBufferedMemorySafe(Value.GetPostEvalPure(), irProgram, typeargs);
                    firstEmit = false;
                }

                BufferedEmitter getPropertyEmitter = new();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                Property accessProperty = propertyContainer.FindProperty(property.Name);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                if (!accessProperty.EmitGet(irProgram, getPropertyEmitter, typeargs, propertyContainer, valueEmitter, responsibleDestroyer))
                {
                    BufferedEmitter copyProperty = new();
                    property.Type.EmitCopyValue(irProgram, copyProperty, getPropertyEmitter.ToString(), responsibleDestroyer);
                    emittedValues.Add(copyProperty.ToString());
                }
                else
                    emittedValues.Add(getPropertyEmitter.ToString());
            }

            if (Value.RequiresDisposal(typeargs, true))
            {
                emitter.Append($"{realPrototype.GetCName(irProgram)} nhp_int_res{irProgram.ExpressionDepth} = ");
                emitter.Append($"({realPrototype.GetCName(irProgram)}){{");
                for (int i = 0; i < properties.Count; i++)
                {
                    if (i > 0)
                        emitter.Append(", ");
                    emitter.Append($".{properties[i].Name} = {emittedValues[i]}");
                }
                emitter.Append("};");

                Value.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, $"nhp_marshal_buf{irProgram.ExpressionDepth}", "NULL");
                emitter.Append($"nhp_int_res{irProgram.ExpressionDepth};}})");
                irProgram.ExpressionDepth--;
            }
            else
            {
                if(irProgram.EmitExpressionStatements)
                {
                    emitter.Append($"({realPrototype.GetCName(irProgram)}){{");
                    for(int i = 0; i < properties.Count; i++)
                    {
                        if (i > 0)
                            emitter.Append(", ");
                        emitter.Append($".{properties[i].Name} = {emittedValues[i]}");
                    }
                    emitter.Append('}');
                }
                else
                    emitter.Append($"marshal_interface{realPrototype.GetStandardIdentifier(irProgram)}({string.Join(", ", emittedValues)})");
            }
        }
    }
}