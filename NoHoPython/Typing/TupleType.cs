using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Typing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.Typing
{
    public sealed partial class TupleType : IType, IPropertyContainer
    {
        public string TypeName => $"tuple<{string.Join(", ", orderedValueTypes.ConvertAll((type) => type.TypeName))}>";
        public string Identifier => $"tuple_{string.Join("_", orderedValueTypes.ConvertAll((type) => type.Identifier))}";
        public bool IsEmpty => false;

        public readonly Dictionary<IType, int> ValueTypes;
        private readonly List<IType> orderedValueTypes;
        private readonly Dictionary<string, Property> properties; 

        public IRValue GetDefaultValue(Syntax.IAstElement errorReportedElement) => new TupleLiteral(orderedValueTypes.ConvertAll((type) => type.GetDefaultValue(errorReportedElement)), errorReportedElement);

        public TupleType(List<IType> valueTypes)
        {
            ValueTypes = new(valueTypes.Count, new ITypeComparer());
            orderedValueTypes = valueTypes;
            orderedValueTypes.Sort(new ITypeComparer());
            properties = new(valueTypes.Count);

            foreach (IType type in valueTypes)
            {
                if (ValueTypes.ContainsKey(type))
                {
                    ValueTypes[type]++;
                    string identifier = $"{type.Identifier}{ValueTypes[type]}";
                    properties.Add(identifier, new Property(identifier, type));
                }
                else
                {
                    ValueTypes.Add(type, 1);
                    string identifier = $"{type.Identifier}1";
                    properties.Add(identifier, new Property(identifier, type));
                }
            }
        }

        public bool HasProperty(string identifier) => properties.ContainsKey(identifier);
        public Property FindProperty(string identifier) => properties[identifier];

        public List<Property> GetProperties() => properties.Values.ToList();

        public bool IsCompatibleWith(IType type)
        {
            if(type is TupleType tupleType)
            {
                if (ValueTypes.Count != tupleType.ValueTypes.Count)
                    return false;

                foreach(KeyValuePair<IType, int> valuePair in ValueTypes)
                {
                    if (!tupleType.ValueTypes.ContainsKey(valuePair.Key))
                        return false;
                    if (tupleType.ValueTypes[valuePair.Key] != valuePair.Value)
                        return false;
                }
                return true;
            }
            return false;
        }
    }
}