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

            typeDependencyTree.Add(interfaceType, new HashSet<IType>(interfaceType.GetProperties().ConvertAll((prop) => prop.Type), new ITypeComparer()));

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
        
        public void ForwardDeclareInterfaceTypes(StringBuilder emitter)
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

        public void ForwardDeclareType(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
                interfaceType.EmitCStruct(irProgram, emitter);
        }

        public void ForwardDeclare(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
            {
                interfaceType.EmitMarshallerHeader(irProgram, emitter);
                emitter.AppendLine($"void free_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface);");
                emitter.AppendLine($"{interfaceType.GetCName(irProgram)} copy_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)} interface);");
                emitter.AppendLine($"{interfaceType.GetCName(irProgram)} move_interface{interfaceType.GetStandardIdentifier(irProgram)}({interfaceType.GetCName(irProgram)}* dest, {interfaceType.GetCName(irProgram)} src);");
            }
        }

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!irProgram.InterfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in irProgram.InterfaceTypeOverloads[this])
            {
                interfaceType.EmitMarshaller(irProgram, emitter);
                interfaceType.EmitDestructor(irProgram, emitter);
                interfaceType.EmitCopier(irProgram, emitter);
                interfaceType.EmitMover(irProgram, emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class InterfaceType
    {
        public bool RequiresDisposal => true;

        public string GetStandardIdentifier(IRProgram irProgram) => $"_nhp_interface_{IScopeSymbol.GetAbsolouteName(InterfaceDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier(irProgram)))}_";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t";

        public void EmitFreeValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.AppendLine($"free_interface{GetStandardIdentifier(irProgram)}({valueCSource});");
        public void EmitCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => emitter.Append($"copy_interface{GetStandardIdentifier(irProgram)}({valueCSource})");
        public void EmitMoveValue(IRProgram irProgram, StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move_interface{GetStandardIdentifier(irProgram)}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(IRProgram irProgram, StringBuilder emitter, string valueCSource) => EmitCopyValue(irProgram, emitter, valueCSource);
        public void EmitRecordCopyValue(IRProgram irProgram, StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(irProgram, emitter, valueCSource);

        public void EmitGetProperty(IRProgram irProgram, StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}.{property.Name}");

        public void EmitMarshallerHeader(IRProgram irProgram, StringBuilder emitter) => emitter.AppendLine($"{GetCName(irProgram)} marshal_interface{GetStandardIdentifier(irProgram)}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName(irProgram)} {prop.Name}"))});");

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (irBuilder.DeclareUsedInterfaceType(this))
            {
                foreach (var property in requiredImplementedProperties.Value)
                    property.Type.ScopeForUsedTypes(irBuilder);
            }
        }

        public void EmitCStruct(IRProgram irProgram, StringBuilder emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendLine("struct " + GetStandardIdentifier(irProgram) + " {");
            foreach (var property in requiredImplementedProperties.Value)
                emitter.AppendLine($"\t{property.Type.GetCName(irProgram)} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.Append($"{GetCName(irProgram)} marshal_interface{GetStandardIdentifier(irProgram)}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName(irProgram)} {prop.Name}"))})");
            emitter.AppendLine(" {");
            
            emitter.AppendLine($"\t{GetCName(irProgram)} marshalled_interface;");
            foreach(var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tmarshalled_interface.{property.Name} = ");
                property.Type.EmitCopyValue(irProgram, emitter, property.Name);
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn marshalled_interface;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.Append($"void free_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface)");
            emitter.AppendLine(" {");

            foreach (var property in requiredImplementedProperties.Value)
                if (property.Type.RequiresDisposal)
                {
                    emitter.Append('\t');
                    property.Type.EmitFreeValue(irProgram, emitter, $"interface.{property.Name}");
                }
            emitter.AppendLine("}");
        }

        public void EmitCopier(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} copy_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} interface) {{");

            emitter.AppendLine($"\t{GetCName(irProgram)} copied_interface;");
            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tcopied_interface.{property.Name} = ");
                property.Type.EmitCopyValue(irProgram, emitter, $"interface.{property.Name}");
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn copied_interface;");
            emitter.AppendLine("}");
        }

        public void EmitMover(IRProgram irProgram, StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName(irProgram)} move_interface{GetStandardIdentifier(irProgram)}({GetCName(irProgram)}* dest, {GetCName(irProgram)} src) {{");
            emitter.AppendLine($"\t{GetCName(irProgram)} temp_buffer = *dest;");
            emitter.AppendLine($"\t*dest = src;");
            emitter.Append('\t');
            EmitFreeValue(irProgram, emitter, "temp_buffer");
            emitter.AppendLine("\treturn src;");
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

        public void Emit(IRProgram irProgram, StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            InterfaceType realPrototype = (InterfaceType)TargetType.SubstituteWithTypearg(typeargs);

            List<Property> properties = realPrototype.GetProperties();
            List<string> emittedValues = new List<string>(properties.Count);
            foreach(Property property in properties)
            {
                StringBuilder valueEmitter = new StringBuilder();
                Value.Emit(irProgram, valueEmitter, typeargs);

                StringBuilder getPropertyEmitter = new StringBuilder();
                if (Value.Type.SubstituteWithTypearg(typeargs) is IPropertyContainer propertyContainer)
                    propertyContainer.EmitGetProperty(irProgram, getPropertyEmitter, valueEmitter.ToString(), property);
                else
                    throw new InvalidOperationException();
                
                emittedValues.Add(getPropertyEmitter.ToString());
            }

            emitter.Append($"marshal_interface{realPrototype.GetStandardIdentifier(irProgram)}({string.Join(", ", emittedValues)})");
        }
    }
}