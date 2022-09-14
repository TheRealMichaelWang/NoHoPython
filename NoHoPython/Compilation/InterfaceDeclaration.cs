using NoHoPython.IntermediateRepresentation;
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
            foreach (InterfaceType usedRecord in usedInterfaceTypes)
                if (interfaceType.IsCompatibleWith(usedRecord))
                    return false;

            usedInterfaceTypes.Add(interfaceType);
            if (!interfaceTypeOverloads.ContainsKey(interfaceType.InterfaceDeclaration))
                interfaceTypeOverloads.Add(interfaceType.InterfaceDeclaration, new List<InterfaceType>());
            interfaceTypeOverloads[interfaceType.InterfaceDeclaration].Add(interfaceType);

            return true;
        }

        public static void ForwardDeclareInterfaceTypes(StringBuilder emitter)
        {
            foreach (InterfaceType interfaceType in usedInterfaceTypes)
                emitter.AppendLine($"typedef struct {interfaceType.GetStandardIdentifier()} {interfaceType.GetCName()};");
        }

        public void ScopeForUsedTypes() { }

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
                emitter.AppendLine($"void free_interface{interfaceType.GetStandardIdentifier()}({interfaceType.GetCName()}* interface);");
                emitter.AppendLine($"{interfaceType.GetCName()} copy_interface{interfaceType.GetStandardIdentifier()}({interfaceType.GetCName()}* record);");
            }
        }

        public void Emit(StringBuilder emitter, Dictionary<TypeParameter, IType> typeargs, int indent)
        {
            if (!interfaceTypeOverloads.ContainsKey(this))
                return;

            foreach (InterfaceType interfaceType in interfaceTypeOverloads[this])
            {
                interfaceType.EmitDestructor(emitter);
                interfaceType.EmitCopier(emitter);
            }
        }
    }
}

namespace NoHoPython.Typing
{
    partial class InterfaceType
    {
        public string GetStandardIdentifier() => $"_nhp_interface_{IScopeSymbol.GetAbsolouteName(InterfaceDeclaration)}_{string.Join('_', TypeArguments.ConvertAll((typearg) => typearg.GetCName()))}_";

        public string GetCName() => $"{GetStandardIdentifier()}_t";

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
                emitter.Append($"\t{GetCName()} {property.Name};");
            emitter.AppendLine("}");
        }

        public void EmitDestructor(StringBuilder emitter)
        {
            emitter.Append($"void free_interface{GetStandardIdentifier()}({GetCName()}* interface) ");
            emitter.AppendLine(" {");

            foreach (var property in requiredImplementedProperties.Value)
                IRValue.EmitFreeValue(emitter, $"interface->{property.Name}", property.Type);
            emitter.AppendLine("}");
        }

        public void EmitCopier(StringBuilder emitter)
        {

        }
    }
}