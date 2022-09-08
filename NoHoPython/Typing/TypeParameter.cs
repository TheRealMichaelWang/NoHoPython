using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;

namespace NoHoPython.Typing
{
    public sealed class TypeParameter : IScopeSymbol
    {
        public static void ValidateTypeArguments(List<TypeParameter> typeParameters, List<IType> typeArguments)
        {
            if (typeArguments.Count != typeParameters.Count)
                throw new UnexpectedTypeArgumentsException(typeParameters.Count, typeArguments.Count);
            for (int i = 0; i < typeArguments.Count; i++)
                if (!typeParameters[i].SupportsType(typeArguments[i]))
                    throw new UnexpectedTypeException(new TypeParameterReference(typeParameters[i]), typeArguments[i]);
        }

        public static void MatchTypeargs(Dictionary<TypeParameter, IType> typeargs, List<IType> existingTypeArguments, List<IType> arguments)
        {
            if (existingTypeArguments.Count != arguments.Count)
                throw new UnexpectedTypeArgumentsException(existingTypeArguments.Count, arguments.Count);
            for (int i = 0; i < existingTypeArguments.Count; i++)
                existingTypeArguments[i].MatchTypeArgumentWithType(typeargs, arguments[i]);
        }

        public static string GetMangledTypeArgumentNames(List<IType> typeArguments) => string.Join('_', typeArguments.Select((IType type) => type.TypeName));

        public bool IsGloballyNavigable => false;

        public string Name { get;private set; }
        public IType? RequiredImplementedInterface { get; private set; }

        public TypeParameter(string name, IType? requiredImplementedInterface)
        {
            Name = name;
            RequiredImplementedInterface = requiredImplementedInterface;
        }

        public bool SupportsType(IType type)
        {
            if (RequiredImplementedInterface == null)
                return true;
            return RequiredImplementedInterface.IsCompatibleWith(type);
        }
    }

#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public sealed partial class TypeParameterReference : IType
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    {
        public string TypeName { get => TypeParameter.Name; }

        public TypeParameter TypeParameter { get; private set; }

        public TypeParameterReference(TypeParameter typeParameter)
        {
            TypeParameter = typeParameter;
        }

        public bool IsCompatibleWith(IType type)
        {
            if (type is TypeParameterReference typeParameterReference)
                return TypeParameter == typeParameterReference.TypeParameter;
            return false;
        }

        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => typeargs[TypeParameter].Clone();

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (typeargs.ContainsKey(TypeParameter))
            {
                if (!typeargs[TypeParameter].IsCompatibleWith(argument))
                    throw new UnexpectedTypeException(typeargs[TypeParameter], argument);
            }
            else
            {
                if (!TypeParameter.SupportsType(argument))
                    throw new UnexpectedTypeException(argument);
                typeargs.Add(TypeParameter, argument);
            }
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument)
        {
            if (typeargs.ContainsKey(TypeParameter))
            {
                if (typeargs[TypeParameter].IsCompatibleWith(argument.Type))
                    return argument;
                else
                    return ArithmeticCast.CastTo(argument, typeargs[TypeParameter]);
            }
            else
            {
#pragma warning disable CS8604 //not actually possible because supports type always returns true if null
                IRValue newArgument = TypeParameter.SupportsType(argument.Type) ? argument : ArithmeticCast.CastTo(argument, TypeParameter.RequiredImplementedInterface);
#pragma warning restore CS8604 
                typeargs.Add(TypeParameter, newArgument.Type);
                return newArgument;
            }
        }
    }

    partial class ArrayType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayType(ElementType.SubstituteWithTypearg(typeargs));

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (argument is ArrayType arrayType)
                ElementType.MatchTypeArgumentWithType(typeargs, arrayType.ElementType);
            else
                throw new UnexpectedTypeException(argument);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument)
        {
            if (argument.Type is ArrayType arrayType)
            {
                ElementType.MatchTypeArgumentWithType(typeargs, arrayType.ElementType);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this.SubstituteWithTypearg(typeargs));
        }
    }

    partial class BooleanType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new BooleanType();
    }

    partial class CharacterType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new CharacterType();
    }

    partial class DecimalType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new DecimalType();
    }

    partial class IntegerType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IntegerType();
    }

    partial class NothingType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new NothingType();

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument) => ArithmeticCast.CastTo(argument, this);

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (!this.IsCompatibleWith(argument))
                throw new UnexpectedTypeException(this, argument);
        }
    }

    partial class EnumType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new EnumType(EnumDeclaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList()); 

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (argument is EnumType enumType && EnumDeclaration == enumType.EnumDeclaration)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, enumType.TypeArguments);
            else
                throw new UnexpectedTypeException(argument);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument)
        {
            if (argument.Type is EnumType enumType && EnumDeclaration == enumType.EnumDeclaration) 
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, enumType.TypeArguments);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this.SubstituteWithTypearg(typeargs));
        }
    }

    partial class RecordType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new RecordType(RecordPrototype, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (argument is RecordType recordType && RecordPrototype == recordType.RecordPrototype)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, recordType.TypeArguments);
            else
                throw new UnexpectedTypeException(argument);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument)
        {
            if(argument.Type is RecordType recordType && RecordPrototype == recordType.RecordPrototype)
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, recordType.TypeArguments);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this.SubstituteWithTypearg(typeargs));
        }
    }

    partial class InterfaceType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new InterfaceType(InterfaceDeclaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (argument is InterfaceType interfaceType && InterfaceDeclaration == interfaceType.InterfaceDeclaration)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, interfaceType.TypeArguments);
            else
                throw new UnexpectedTypeException(argument);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument)
        {
            if (argument.Type is InterfaceType interfaceType && InterfaceDeclaration == interfaceType.InterfaceDeclaration)
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, interfaceType.TypeArguments);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this);
        }
    }

    partial class ProcedureType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ProcedureType(ReturnType.SubstituteWithTypearg(typeargs), ParameterTypes.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument)
        {
            if (argument is ProcedureType procedureType)
            {
                ReturnType.MatchTypeArgumentWithType(typeargs, procedureType.ReturnType);
                TypeParameter.MatchTypeargs(typeargs, ParameterTypes, procedureType.ParameterTypes);
            }
            else
                throw new UnexpectedTypeException(argument);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument)
        {
            if(argument.Type is ProcedureType procedureType)
            {
                ReturnType.MatchTypeArgumentWithType(typeargs, procedureType.ReturnType);
                TypeParameter.MatchTypeargs(typeargs, ParameterTypes, procedureType.ParameterTypes);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this);
        }
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IntegerLiteral(Number);
    }

    partial class DecimalLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new DecimalLiteral(Number);
    }

    partial class CharacterLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new CharacterLiteral(Character);
    }

    partial class ArrayLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayLiteral(ElementType.SubstituteWithTypearg(typeargs), Elements.Select((IRValue element) => element.SubstituteWithTypearg(typeargs)).ToList());
    }

    partial class AllocArray
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocArray(ElementType.SubstituteWithTypearg(typeargs), Size.SubstituteWithTypearg(typeargs));
    }

    partial class AllocRecord
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocRecord((RecordType)RecordPrototype.SubstituteWithTypearg(typeargs), ConstructorArguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList());
    }

    partial class IfElseValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IfElseValue(Type.SubstituteWithTypearg(typeargs), Condition.SubstituteWithTypearg(typeargs), IfTrueValue.SubstituteWithTypearg(typeargs), IfFalseValue.SubstituteWithTypearg(typeargs));
    }

    partial class ArithmeticCast
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArithmeticCast(Operation, Input.SubstituteWithTypearg(typeargs));
    }

    partial class ArithmeticOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArithmeticOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs));
    }

    partial class ComparativeOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ComparativeOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs));
    }

    partial class LogicalOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LogicalOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs));
    }

    partial class GetValueAtIndex
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new GetValueAtIndex(Array.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs));
    } 

    partial class SetValueAtIndex
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetValueAtIndex(Array.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs));
    }

    partial class GetPropertyValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new GetPropertyValue(Record.SubstituteWithTypearg(typeargs), Property.Name);
    }

    partial class SetPropertyValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetPropertyValue(Record.SubstituteWithTypearg(typeargs), Property.Name, Value.SubstituteWithTypearg(typeargs));
    }
}