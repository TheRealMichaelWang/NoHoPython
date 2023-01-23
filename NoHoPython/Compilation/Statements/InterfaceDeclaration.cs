using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private List<InterfaceType> usedInterfaceTypes = new();
        private Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverloads = new();

        public bool DeclareUsedInterfaceType(InterfaceType interfaceType)
        {
            foreach (InterfaceType usedInterface in usedInterfaceTypes)
                if (interfaceType.IsCompatibleWith(usedInterface))
                    return false;

            usedInterfaceTypes.Add(interfaceType);
            if (!interfaceTypeOverloads.ContainsKey(interfaceType.InterfaceDeclaration))
                interfaceTypeOverloads.Add(interfaceType.InterfaceDeclaration, new List<InterfaceType>());
            interfaceTypeOverloads[interfaceType.InterfaceDeclaration].Add(interfaceType);

            typeDependencyTree.Add(interfaceType, new HashSet<IType>(interfaceType.GetProperties().ConvertAll((prop) => prop.Type).Where((type) => type is not RecordType), new ITypeComparer()));

            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        private List<InterfaceType> usedInterfaceTypes;
        public readonly Dictionary<InterfaceDeclaration, List<InterfaceType>> InterfaceTypeOverloads;
        
        public void ForwardDeclareInterfaceTypes(StatementEmitter emitter)
        {
            foreach (InterfaceType usedInterface in usedInterfaceTypes)
                emitter.AppendLine($"typedef struct {usedInterface.GetStandardIdentifier(this)} {usedInterface.GetCName(this)};");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class InterfaceDeclaration
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void ForwardDeclareType(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
                interfaceType.EmitCStruct(irProgram, emitter);
        }

        public void ForwardDeclare(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
            {
                interfaceType.EmitMarshallerHeader(irProgram, emitter);
                emitter.AppendLine($"void free_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface, void* child_agent);");
                emitter.AppendLine($"{interfaceType.GetCName(irProgram)} copy_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface, void* responsibleDestroyer);");
                emitter.AppendLine($"{interfaceType.GetCName(irProgram)} change_resp_owner{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface, void* responsibleDestroyer);");
                if (!irProgram.EmitExpressionStatements)
                    emitter.AppendLine($"{interfaceType.GetCName(irProgram)} move_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)}* dest, {interfaceType.GetCName(irProgram)} src, void* child_agent);");
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
                interfaceType.EmitResponsibleDestroyerMutator(irProgram, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class InterfaceType
    {
        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => !requiredImplementedProperties.Value.TrueForAll((property) => !property.Type.MustSetResponsibleDestroyer);

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_interface_{IScopeSymbol.GetAbsolouteName(InterfaceDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

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
        public void EmitMutateResponsibleDestroyer(IRProgram irProgram, IEmitter emitter, string valueCSource, string newResponsibleDestroyer) => emitter.Append(MustSetResponsibleDestroyer ? $"change_resp_owner{GetStandardIdentifier(irProgram)}({valueCSource}, {newResponsibleDestroyer})" : valueCSource);

        public void EmitGetProperty(IRProgram irProgram, IEmitter emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}.{property.Name}");

        public void EmitMarshallerHeader(IRProgram irProgram, StatementEmitter emitter) => emitter.AppendLine($"{GetCName(irProgram)} marshal_interface{GetStandardIdentifier(irProgram)}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName(irProgram)} {prop.Name}"))}{(MustSetResponsibleDestroyer ? ", void* responsibleDestroyer" : string.Empty)});");

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

            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            foreach (var property in requiredImplementedProperties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, StatementEmitter emitter)
        {
            emitter.Append($"{GetCName(irProgram)} marshal_interface{GetStandardIdentifier(irProgram)}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName(irProgram)} {prop.Name}"))}");

            if (MustSetResponsibleDestroyer)
                emitter.Append(", void* responsibleDestroyer");

            emitter.AppendLine(") {");
            emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_interface;");
            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tmarshalled_interface.{property.Name} = ");
                property.Type.EmitCopyValue(irProgram, emitter, property.Name, "responsibleDestroyer");
                emitter.AppendLine(";");
            }
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

        public void EmitResponsibleDestroyerMutator(IRProgram irProgram, StatementEmitter emitter)
        {
            if (!MustSetResponsibleDestroyer)
                return;

            emitter.AppendLine($"{GetCName(irProgram)} change_resp_owner{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface, void* responsibleDestroyer) {{");

            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tinterface.{property.Name} = ");
                property.Type.EmitMutateResponsibleDestroyer(irProgram, emitter, $"interface.{property.Name}", "responsibleDestroyer");
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn interface;");
            emitter.AppendLine("}");
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class MarshalIntoInterface
    {
        public bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => true;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Value.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public void Emit(IRProgram irProgram, IEmitter emitter, Dictionary<TypeParameter, IType> typeargs, string responsibleDestroyer)
        {
            InterfaceType realPrototype = (InterfaceType)TargetType.SubstituteWithTypearg(typeargs);

            if (Value.RequiresDisposal(typeargs))
            {
                if (!irProgram.EmitExpressionStatements)
                    throw new CannotEmitDestructorError(Value);
                emitter.Append($"({{{Value.Type.SubstituteWithTypearg(typeargs).GetCName(irProgram)} _nhp_marshal_buf = ");
                Value.Emit(irProgram, emitter, typeargs, "NULL");
                emitter.Append(';');
            }

            List<Property> properties = realPrototype.GetProperties();
            List<string> emittedValues = new(properties.Count);

            bool firstEmit = true;
            foreach (Property property in properties)
            {
                string valueEmitter;
                if (Value.RequiresDisposal(typeargs))
                    valueEmitter = "_nhp_marshal_buf";
                else if (firstEmit)
                    valueEmitter = BufferedEmitter.EmittedBufferedMemorySafe(Value, irProgram, typeargs);
                else
                {
                    valueEmitter = BufferedEmitter.EmittedBufferedMemorySafe(Value.GetPostEvalPure(), irProgram, typeargs);
                    firstEmit = false;
                }

                BufferedEmitter getPropertyEmitter = new();
                if (Value.Type.SubstituteWithTypearg(typeargs) is IPropertyContainer propertyContainer)
                    propertyContainer.EmitGetProperty(irProgram, getPropertyEmitter, valueEmitter.ToString(), property);
                else
                    throw new InvalidOperationException();

                emittedValues.Add(getPropertyEmitter.ToString() + ", ");
            }

            if (Value.RequiresDisposal(typeargs))
            {
                emitter.Append($"{realPrototype.GetCName(irProgram)} _nhp_int_res = marshal_interface{realPrototype.GetStandardIdentifier(irProgram)}({string.Join("", emittedValues)}{responsibleDestroyer}); ");
                Value.Type.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, "_nhp_marshal_buf", "NULL");
                emitter.Append("_nhp_int_res;})");
            }
            else
                emitter.Append($"marshal_interface{realPrototype.GetStandardIdentifier(irProgram)}({string.Join("", emittedValues)}{responsibleDestroyer})");
        }
    }
}