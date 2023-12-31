using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Syntax;
using NoHoPython.Typing;
using System.Text;

namespace NoHoPython.Typing
{
    public sealed partial class TupleType : IType, IPropertyContainer
    {
        sealed partial class TupleProperty : Property
        {
            public override bool IsReadOnly => true;

            public int TypeNumber { get; private set; }

            public TupleProperty(IType type, int typeNumber) : base($"{type.Identifier}{typeNumber}", type)
            {
                TypeNumber = typeNumber;
            }
        }

        public string TypeName => $"tuple<{string.Join(", ", orderedValueTypes.ConvertAll((type) => type.TypeName))}>";
        public string Identifier => IType.GetIdentifier("tuple", orderedValueTypes.ToArray());
        public string PrototypeIdentifier
        {
            get
            {
                StringBuilder builder = new();
                builder.Append("tuple");
                for (int i = 0; i < orderedValueTypes.Count; i++)
                    builder.Append("_T");
                return builder.ToString();
            }
        }
        public bool IsEmpty => false;
        public bool HasMutableChildren => false;
        public bool IsReferenceType => false;

        public readonly Dictionary<IType, int> ValueTypes;
        private readonly List<IType> orderedValueTypes;
        private readonly Dictionary<string, Property> properties; 

        public IRValue GetDefaultValue(IAstElement errorReportedElement, AstIRProgramBuilder irBuilder) => new TupleLiteral(orderedValueTypes.ConvertAll((type) => type.GetDefaultValue(errorReportedElement, irBuilder)), errorReportedElement);

        public TupleType(List<IType> valueTypes)
        {
            ValueTypes = new(valueTypes.Count, new ITypeComparer());
            orderedValueTypes = valueTypes;
            orderedValueTypes.Sort(new ITypeComparer());
            properties = new(valueTypes.Count);

            foreach (IType type in valueTypes)
            {
                TupleProperty tupleProperty;
                if (ValueTypes.ContainsKey(type))
                    tupleProperty = new TupleProperty(type, ValueTypes[type]++);
                else
                {
                    tupleProperty = new TupleProperty(type, 0);
                    ValueTypes.Add(type, 1);
                }

                properties.Add(tupleProperty.Name, tupleProperty);
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

namespace NoHoPython.IntermediateRepresentation.Values
{
    public sealed partial class MarshalIntoLowerTuple : IRValue
    {
        public IAstElement ErrorReportedElement { get; private set; }

        public IType Type => TargetType;
        public bool IsTruey => false;
        public bool IsFalsey => false;

        public TupleType TargetType { get; private set; }
        public IRValue Value { get; private set; }

        public MarshalIntoLowerTuple(TupleType targetType, IRValue value, IAstElement errorReportedElement)
        {
            TargetType = targetType;
            Value = value;
            ErrorReportedElement = errorReportedElement;

            if (Value.Type is TupleType valueTupleType)
            {
                foreach(KeyValuePair<IType, int> valueType in targetType.ValueTypes)
                {
                    if (!valueTupleType.ValueTypes.ContainsKey(valueType.Key) || valueTupleType.ValueTypes[valueType.Key] < TargetType.ValueTypes[valueType.Key])
                        throw new CannotMarshalIntoLowerTupleError(value, targetType, valueType, errorReportedElement);
                }
            }
            else
                throw new UnexpectedTypeException(Value.Type, errorReportedElement);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    public sealed class CannotMarshalIntoLowerTupleError : IRGenerationError
    {
        public IRValue Value { get; private set; }
        public TupleType TargetType { get; private set; }

        public KeyValuePair<IType, int> ExpectedSupportedValueType { get; private set; }

        public CannotMarshalIntoLowerTupleError(IRValue value, TupleType targetType, KeyValuePair<IType, int> expectedSupportedValueType, IAstElement errorReportedElement) : base(errorReportedElement, $"Unable to marshal tuple {value} into {targetType.TypeName} because it doesn't support {expectedSupportedValueType.Value} {expectedSupportedValueType.Key.TypeName}(s).")
        {
            Value = value;
            TargetType = targetType;
            ExpectedSupportedValueType = expectedSupportedValueType;
        }
    }
}

namespace NoHoPython.Syntax.Values
{
    partial class TupleLiteral
    {
        public IRValue GenerateIntermediateRepresentationForValue(AstIRProgramBuilder irBuilder, IType? expectedType, bool willRevaluate) => new IntermediateRepresentation.Values.TupleLiteral(TupleElements.ConvertAll((element) => element.GenerateIntermediateRepresentationForValue(irBuilder, null, willRevaluate)), this);
    }
}