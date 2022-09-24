using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.IntermediateRepresentation.Statements
{
    partial class InterfaceDeclaration
    {
        private static List<InterfaceType> usedInterfaceTypes = new List<InterfaceType>();
        private static Dictionary<InterfaceDeclaration, List<InterfaceType>> interfaceTypeOverloads = new Dictionary<InterfaceDeclaration, List<InterfaceType>>();

        public static bool DeclareUsedInterfaceType(InterfaceType interfaceType)
        {
            foreach (InterfaceType usedInterface in usedInterfaceTypes)
                if (interfaceType.IsCompatibleWith(usedInterface))
                    return false;

            usedInterfaceTypes.Add(interfaceType);
            if (!interfaceTypeOverloads.ContainsKey(interfaceType.InterfaceDeclaration))
                interfaceTypeOverloads.Add(interfaceType.InterfaceDeclaration, new List<InterfaceType>());
            interfaceTypeOverloads[interfaceType.InterfaceDeclaration].Add(interfaceType);

            return true;
        }

        public static void ForwardDeclareInterfaceTypes(StringBuilder emitter)
        {
            foreach (InterfaceType usedInterface in usedInterfaceTypes)
                emitter.AppendLine($"typedef struct {usedInterface.GetStandardIdentifier()} {usedInterface.GetCName()};");
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs) { }

        public void ForwardDeclareType(StringBuilder emitter)
        {
            if (!interfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in interfaceTypeOverloads[this])
                interfaceType.EmitCStruct(emitter);
        }

        public void ForwardDeclare(StringBuilder emitter)
        {
            if (!interfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in interfaceTypeOverloads[this])
            {
                interfaceType.EmitMarshallerHeader(emitter);
                emitter.AppendLine($"void free_interface{interfaceType.GetStandardIdentifier()}({interfaceType.GetCName()} interface);");
                emitter.AppendLine($"{interfaceType.GetCName()} copy_interface{interfaceType.GetStandardIdentifier()}({interfaceType.GetCName()} interface);");
                emitter.AppendLine($"{interfaceType.GetCName()} move_interface{interfaceType.GetStandardIdentifier()}({interfaceType.GetCName()}* dest, {interfaceType.GetCName()} src);");
            }
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!interfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in interfaceTypeOverloads[this])
            {
                interfaceType.EmitMarshaller(emitter);
                interfaceType.EmitDestructor(emitter);
                interfaceType.EmitCopier(emitter);
                interfaceType.EmitMover(emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class InterfaceType
    {
        public bool RequiresDisposal => true;

        public string GetStandardIdentifier() => $"_nhp_interface_{IScopeSymbol.GetAbsolouteName(InterfaceDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetStandardIdentifier()))}_";

        public string GetCName() => $"{GetStandardIdentifier()}_t";

        public void EmitFreeValue(StringBuilder emitter, string valueCSource) => emitter.AppendLine($"free_interface{GetStandardIdentifier()}({valueCSource});");
        public void EmitCopyValue(StringBuilder emitter, string valueCSource) => emitter.Append($"copy_interface{GetStandardIdentifier()}({valueCSource})");
        public void EmitMoveValue(StringBuilder emitter, string destC, string valueCSource) => emitter.Append($"move_interface{GetStandardIdentifier()}(&{destC}, {valueCSource})");
        public void EmitClosureBorrowValue(StringBuilder emitter, string valueCSource) => EmitCopyValue(emitter, valueCSource);
        public void EmitRecordCopyValue(StringBuilder emitter, string valueCSource, string recordCSource) => EmitCopyValue(emitter, valueCSource);

        public void EmitGetProperty(StringBuilder emitter, string valueCSource, Property property) => emitter.Append($"{valueCSource}.{property.Name}");

        public void EmitMarshallerHeader(StringBuilder emitter) => emitter.AppendLine($"{GetCName()} marshal_interface{GetStandardIdentifier()}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName()} {prop.Name}"))});");

        public void ScopeForUsedTypes()
        {
            if (InterfaceDeclaration.DeclareUsedInterfaceType(this))
            {
                foreach (var property in requiredImplementedProperties.Value)
                    property.Type.ScopeForUsedTypes();
            }
        }

        public void EmitCStruct(StringBuilder emitter)
        {
            emitter.AppendLine("struct " + GetStandardIdentifier() + " {");
            foreach (var property in requiredImplementedProperties.Value)
                emitter.Append($"\t{property.Type.GetCName()} {property.Name};");
            emitter.AppendLine("};");
        }

        public void EmitMarshaller(StringBuilder emitter)
        {
            emitter.Append($"{GetCName()} marshal_interface{GetStandardIdentifier()}({string.Join(", ", requiredImplementedProperties.Value.ConvertAll((prop) => $"{prop.Type.GetCName()} {prop.Name}"))})");
            emitter.AppendLine(" {");
            
            emitter.AppendLine($"\t{GetCName()} marshalled_interface;");
            foreach(var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tmarshalled_interface->{property.Name} = ");
                property.Type.EmitCopyValue(emitter, property.Name);
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn marshalled_interface;");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(StringBuilder emitter)
        {
            emitter.Append($"void free_interface{GetStandardIdentifier()}({GetCName()} interface)");
            emitter.AppendLine(" {");

            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append('\t');
                property.Type.EmitFreeValue(emitter, $"interface.{property.Name}");
            }
            emitter.AppendLine("}");
        }

        public void EmitCopier(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} copy_interface{GetStandardIdentifier()}({GetCName()} interface)");
            emitter.AppendLine(" {");

            emitter.AppendLine($"\t{GetCName()} copied_interface;");
            foreach (var property in requiredImplementedProperties.Value)
            {
                emitter.Append($"\tcopied_interface.{property.Name} = ");
                property.Type.EmitCopyValue(emitter, $"interface.{property}");
                emitter.AppendLine(";");
            }
            emitter.AppendLine("\treturn copied_interface;");
            emitter.AppendLine("}");
        }

        public void EmitMover(StringBuilder emitter)
        {
            emitter.AppendLine($"{GetCName()} move_interface{GetStandardIdentifier()}({GetCName()}* dest, {GetCName()} src) {{");
            emitter.AppendLine($"\t{GetCName()} temp_buffer = *dest;");
            emitter.AppendLine($"\t*dest = src;");
            emitter.Append('\t');
            EmitFreeValue(emitter, "temp_buffer");
            emitter.AppendLine(";");
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

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs)
        {
            TargetType.SubstituteWithTypearg(typeargs).ScopeForUsedTypes();
            Value.ScopeForUsedTypes(typeargs);
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs)
        {
            InterfaceType realPrototype = (InterfaceType)TargetType.SubstituteWithTypearg(typeargs);

            List<Property> properties = realPrototype.GetProperties();
            List<string> emittedValues = new List<string>(properties.Count);
            foreach(Property property in properties)
            {
                StringBuilder valueEmitter = new StringBuilder();
                Value.Emit(valueEmitter, typeargs);

                StringBuilder getPropertyEmitter = new StringBuilder();
                if (Value.Type.SubstituteWithTypearg(typeargs) is IPropertyContainer propertyContainer)
                    propertyContainer.EmitGetProperty(getPropertyEmitter, valueEmitter.ToString(), property);
                else
                    throw new InvalidOperationException();
                
                emittedValues.Add(getPropertyEmitter.ToString());
            }

            emitter.Append($"marshal_interface{realPrototype.GetStandardIdentifier()}({string.Join(", ", emittedValues)})");
        }
    }
}