﻿using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.IntermediateRepresentation.Values;
using NoHoPython.Scoping;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.Typing
{
    public sealed class TypeParameter : IScopeSymbol
    {
        public static List<IType> ValidateTypeArguments(List<TypeParameter> typeParameters, List<IType> typeArguments, Syntax.IAstElement errorReportedElement)
        {
            if (typeArguments.Count != typeParameters.Count)
                throw new UnexpectedTypeArgumentsException(typeParameters.Count, typeArguments.Count, errorReportedElement);
            for (int i = 0; i < typeArguments.Count; i++)
                if (!typeParameters[i].SupportsType(typeArguments[i]))
                    throw new UnexpectedTypeException(new TypeParameterReference(typeParameters[i]), typeArguments[i], errorReportedElement);
            return typeArguments;
        }

        public static Lazy<Dictionary<TypeParameter, IType>> GetTypeargMap(List<TypeParameter> typeParameters, List<IType> typeArguments)
        {
            Debug.Assert(typeParameters.Count == typeArguments.Count);
            return new Lazy<Dictionary<TypeParameter, IType>>(() =>
            {
                Dictionary<TypeParameter, IType> typeargMap = new(typeParameters.Count);
                for (int i = 0; i < typeParameters.Count; i++)
                    typeargMap.Add(typeParameters[i], typeArguments[i]);
                return typeargMap;
            });
        }

        public static void MatchTypeargs(Dictionary<TypeParameter, IType> typeargs, List<IType> existingTypeArguments, List<IType> arguments, Syntax.IAstElement errorReportedElement)
        {
            if (existingTypeArguments.Count != arguments.Count)
                throw new UnexpectedTypeArgumentsException(existingTypeArguments.Count, errorReportedElement);
            for (int i = 0; i < existingTypeArguments.Count; i++)
                existingTypeArguments[i].MatchTypeArgumentWithType(typeargs, arguments[i], errorReportedElement);
        }

        public string Name { get; private set; }
        public InterfaceType? RequiredImplementedInterface { get; private set; }

        public SymbolContainer ParentContainer { get; private set; }
        public Syntax.IAstElement ErrorReportedElement { get; private set; }

        public TypeParameter(string name, InterfaceType? requiredImplementedInterface, SymbolContainer parentContainer, Syntax.IAstElement errorReportedElement)
        {
            Name = name;
            RequiredImplementedInterface = requiredImplementedInterface;
            ParentContainer = parentContainer;
            ErrorReportedElement = errorReportedElement;
        }

        public bool SupportsType(IType type)
        {
            if (RequiredImplementedInterface == null)
                return true;

            if(type is IPropertyContainer propertyContainer)
                return RequiredImplementedInterface.SupportsProperties(propertyContainer.GetProperties());

            return false;
        }
    }

    public sealed partial class TypeParameterReference : IType, IPropertyContainer
    {
        sealed partial class TypeParameterProperty : Property
        {
            public override bool IsReadOnly => true;

            public TypeParameter TypeParameter { get; private set; }

            public TypeParameterProperty(TypeParameter typeParameter, Property interfaceProperty) : base(interfaceProperty.Name, interfaceProperty.Type)
            {
                TypeParameter = typeParameter;
            }
        }

        public string TypeName => TypeParameter.Name;
        public string Identifier => TypeName;
        public string PrototypeIdentifier => Identifier;
        public bool IsEmpty => false;

        public TypeParameter TypeParameter { get; private set; }

        public TypeParameterReference(TypeParameter typeParameter)
        {
            TypeParameter = typeParameter;
        }

        public bool IsCompatibleWith(IType type)
        {
            return type is TypeParameterReference typeParameterReference && TypeParameter == typeParameterReference.TypeParameter;
        }

        public bool HasProperty(string property) => TypeParameter.RequiredImplementedInterface != null && TypeParameter.RequiredImplementedInterface.HasProperty(property);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public Property FindProperty(string property) => new TypeParameterProperty(TypeParameter, TypeParameter.RequiredImplementedInterface.FindProperty(property));
#pragma warning restore CS8602 

        public List<Property> GetProperties() => TypeParameter.RequiredImplementedInterface == null ? new() : TypeParameter.RequiredImplementedInterface.GetProperties().ConvertAll((prop) => (Property)new TypeParameterProperty(TypeParameter, prop));

        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs)
        {
            if (!typeargs.ContainsKey(TypeParameter))
                return this;

            return typeargs[TypeParameter];
        }

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (typeargs.ContainsKey(TypeParameter))
            {
                if (!typeargs[TypeParameter].IsCompatibleWith(argument))
                    throw new UnexpectedTypeException(typeargs[TypeParameter], errorReportedElement);
            }
            else
            {
                if (!TypeParameter.SupportsType(argument))
                    throw new UnexpectedTypeException(argument, errorReportedElement);
                typeargs.Add(TypeParameter, argument);
            }
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (typeargs.ContainsKey(TypeParameter))
            {
                return typeargs[TypeParameter].IsCompatibleWith(argument.Type) ? argument : ArithmeticCast.CastTo(argument, typeargs[TypeParameter], irBuilder);
            }
            else
            {
#pragma warning disable CS8604 //not actually possible because supports type always returns true if null
                IRValue newArgument = TypeParameter.SupportsType(argument.Type) ? argument : ArithmeticCast.CastTo(argument, TypeParameter.RequiredImplementedInterface, irBuilder);
#pragma warning restore CS8604 
                typeargs.Add(TypeParameter, newArgument.Type);
                return newArgument;
            }
        }

        public bool ContainsType(IType type) => false;
    }

    partial class HandleType
    {
        public override IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new HandleType(ValueType.SubstituteWithTypearg(typeargs));

        public override void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is HandleType handleType)
                ValueType.MatchTypeArgumentWithType(typeargs, handleType.ValueType, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public override IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is HandleType handleType)
            {
                ValueType.MatchTypeArgumentWithType(typeargs, handleType.ValueType, argument.ErrorReportedElement);
                return argument;
            }
            else
                return MatchTypeArgumentWithValue(typeargs, ArithmeticCast.CastTo(argument, SubstituteWithTypearg(typeargs), irBuilder), irBuilder);
        }

        public override bool ContainsType(IType type) => ValueType.IsCompatibleWith(type) || ValueType.ContainsType(type);
    }

    partial class ArrayType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayType(ElementType.SubstituteWithTypearg(typeargs));

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is ArrayType arrayType)
                ElementType.MatchTypeArgumentWithType(typeargs, arrayType.ElementType, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is ArrayType arrayType)
            {
                ElementType.MatchTypeArgumentWithType(typeargs, arrayType.ElementType, argument.ErrorReportedElement);
                return argument;
            }
            else
                return MatchTypeArgumentWithValue(typeargs, ArithmeticCast.CastTo(argument, SubstituteWithTypearg(typeargs), irBuilder), irBuilder);
        }

        public bool ContainsType(IType type) => ElementType.IsCompatibleWith(type) || ElementType.ContainsType(type);
    }

    partial class ReferenceType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ReferenceType(ElementType.SubstituteWithTypearg(typeargs), Mode);

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is ReferenceType arrayType)
                ElementType.MatchTypeArgumentWithType(typeargs, arrayType.ElementType, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is ReferenceType arrayType)
            {
                ElementType.MatchTypeArgumentWithType(typeargs, arrayType.ElementType, argument.ErrorReportedElement);
                return argument;
            }
            else
                return MatchTypeArgumentWithValue(typeargs, ArithmeticCast.CastTo(argument, SubstituteWithTypearg(typeargs), irBuilder), irBuilder);
        }

        public bool ContainsType(IType type) => ElementType.IsCompatibleWith(type) || ElementType.ContainsType(type);
    }

    partial class MemorySpan
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MemorySpan(ElementType.SubstituteWithTypearg(typeargs), Length);

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is MemorySpan memorySpan)
                ElementType.MatchTypeArgumentWithType(typeargs, memorySpan.ElementType, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is MemorySpan memorySpan)
            {
                ElementType.MatchTypeArgumentWithType(typeargs, memorySpan.ElementType, argument.ErrorReportedElement);
                return argument;
            }
            else
                return MatchTypeArgumentWithValue(typeargs, ArithmeticCast.CastTo(argument, SubstituteWithTypearg(typeargs), irBuilder), irBuilder);
        }

        public bool ContainsType(IType type) => ElementType.IsCompatibleWith(type) || ElementType.ContainsType(type);
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

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (!IsCompatibleWith(argument))
                throw new UnexpectedTypeException(this, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder) => ArithmeticCast.CastTo(argument, this, irBuilder);

        public bool ContainsType(IType type) => false;
    }

    partial class EmptyEnumOption
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => this;

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (!argument.IsCompatibleWith(this))
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder) => ArithmeticCast.CastTo(argument, this, irBuilder);

        public bool ContainsType(IType type) => false;
    }

    partial class EnumType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new EnumType(EnumDeclaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is EnumType enumType && EnumDeclaration == enumType.EnumDeclaration)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, enumType.TypeArguments, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is EnumType enumType && EnumDeclaration == enumType.EnumDeclaration)
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, enumType.TypeArguments, argument.ErrorReportedElement);
                return argument;
            }
            else
                return MatchTypeArgumentWithValue(typeargs, ArithmeticCast.CastTo(argument, SubstituteWithTypearg(typeargs), irBuilder), irBuilder);
        }

        public bool ContainsType(IType type) => GetOptions().Any(option => option.IsCompatibleWith(type) || option.ContainsType(type));
    }

    partial class RecordType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new RecordType(RecordPrototype, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is RecordType recordType && RecordPrototype == recordType.RecordPrototype)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, recordType.TypeArguments, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is RecordType recordType && RecordPrototype == recordType.RecordPrototype)
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, recordType.TypeArguments, argument.ErrorReportedElement);
                return argument;
            }
            else
                return MatchTypeArgumentWithValue(typeargs, ArithmeticCast.CastTo(argument, SubstituteWithTypearg(typeargs), irBuilder), irBuilder);
        }

        public bool ContainsType(IType type) => GetProperties().Any(property => property.Type.IsCompatibleWith(type) || property.Type.ContainsType(type));
    }

    partial class InterfaceType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new InterfaceType(InterfaceDeclaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is InterfaceType interfaceType && InterfaceDeclaration == interfaceType.InterfaceDeclaration)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, interfaceType.TypeArguments, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is InterfaceType interfaceType && InterfaceDeclaration == interfaceType.InterfaceDeclaration)
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, interfaceType.TypeArguments, argument.ErrorReportedElement);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this, irBuilder);
        }

        public bool ContainsType(IType type) => GetProperties().Any(property => property.Type.IsCompatibleWith(type) || property.Type.ContainsType(type));
    }

    partial class ForeignCType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ForeignCType(Declaration, TypeArguments.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is ForeignCType foreignCType && Declaration == foreignCType.Declaration)
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, foreignCType.TypeArguments, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is ForeignCType foreignCType && Declaration == foreignCType.Declaration)
            {
                TypeParameter.MatchTypeargs(typeargs, TypeArguments, foreignCType.TypeArguments, argument.ErrorReportedElement);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this, irBuilder);
        }

        public bool ContainsType(IType type) => GetProperties().Any(property => property.Type.IsCompatibleWith(type) || property.Type.ContainsType(type));
    }

    partial class ProcedureType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ProcedureType(ReturnType.SubstituteWithTypearg(typeargs), ParameterTypes.Select((IType type) => type.SubstituteWithTypearg(typeargs)).ToList(), Purity);

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is ProcedureType procedureType)
            {
                ReturnType.MatchTypeArgumentWithType(typeargs, procedureType.ReturnType,errorReportedElement);
                TypeParameter.MatchTypeargs(typeargs, ParameterTypes, procedureType.ParameterTypes, errorReportedElement);
            }
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is ProcedureType procedureType)
            {
                ReturnType.MatchTypeArgumentWithType(typeargs, procedureType.ReturnType, argument.ErrorReportedElement);
                TypeParameter.MatchTypeargs(typeargs, ParameterTypes, procedureType.ParameterTypes, argument.ErrorReportedElement);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this, irBuilder);
        }

        public bool ContainsType(IType type) => false;
    }

    partial class TupleType
    {
        public IType SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new TupleType(orderedValueTypes.Select((type) => type.SubstituteWithTypearg(typeargs)).ToList());

        public void MatchTypeArgumentWithType(Dictionary<TypeParameter, IType> typeargs, IType argument, Syntax.IAstElement errorReportedElement)
        {
            if (argument is TupleType tupleType)
                TypeParameter.MatchTypeargs(typeargs, orderedValueTypes, tupleType.orderedValueTypes, errorReportedElement);
            else
                throw new UnexpectedTypeException(argument, errorReportedElement);
        }

        public IRValue MatchTypeArgumentWithValue(Dictionary<TypeParameter, IType> typeargs, IRValue argument, Syntax.AstIRProgramBuilder irBuilder)
        {
            if (argument.Type is TupleType tupleType)
            {
                TypeParameter.MatchTypeargs(typeargs, orderedValueTypes, tupleType.orderedValueTypes, argument.ErrorReportedElement);
                return argument;
            }
            else
                return ArithmeticCast.CastTo(argument, this, irBuilder);
        }

        public bool ContainsType(IType type) => orderedValueTypes.Any(tupleType => tupleType.IsCompatibleWith(type) || tupleType.ContainsType(type));
    }
}

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IntegerLiteral(Number, ErrorReportedElement);
    }

    partial class DecimalLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new DecimalLiteral(Number, ErrorReportedElement);
    }

    partial class CharacterLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new CharacterLiteral(Character, ErrorReportedElement);
    }

    partial class TrueLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new TrueLiteral(ErrorReportedElement);
    }

    partial class FalseLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new FalseLiteral(ErrorReportedElement);
    }

    partial class NullPointerLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new NullPointerLiteral((HandleType)Type.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class StaticCStringLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new StaticCStringLiteral(String, ErrorReportedElement);
    }

    partial class EmptyTypeLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new EmptyTypeLiteral(Type.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class ArrayLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayLiteral(ElementType.SubstituteWithTypearg(typeargs), Elements.Select((IRValue element) => element.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    partial class TupleLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new TupleLiteral(Elements.Select((IRValue element) => element.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    partial class ReferenceLiteral
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ReferenceLiteral(Input.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class MarshalIntoLowerTuple
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MarshalIntoLowerTuple((TupleType)TargetType.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class AllocArray
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocArray(ElementType.SubstituteWithTypearg(typeargs), Length.SubstituteWithTypearg(typeargs), ProtoValue.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class AllocMemorySpan
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocMemorySpan(ElementType.SubstituteWithTypearg(typeargs), Length, ProtoValue.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class AllocRecord
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AllocRecord((RecordType)RecordPrototype.SubstituteWithTypearg(typeargs), Arguments.Select((IRValue argument) => argument.SubstituteWithTypearg(typeargs)).ToList(), ErrorReportedElement);
    }

    partial class IfElseValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new IfElseValue(Type.SubstituteWithTypearg(typeargs), Condition.SubstituteWithTypearg(typeargs), IfTrueValue.SubstituteWithTypearg(typeargs), IfFalseValue.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class ArithmeticCast
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArithmeticCast(Operation, Input.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class HandleCast
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new HandleCast((HandleType)TargetHandleType.SubstituteWithTypearg(typeargs), Input.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class AutoCast
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new AutoCast(Type.SubstituteWithTypearg(typeargs), Input.SubstituteWithTypearg(typeargs));
    }

    partial class ArithmeticOperator
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArithmeticOperator(Type.SubstituteWithTypearg(typeargs), Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class PointerAddOperator
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new PointerAddOperator(Address.SubstituteWithTypearg(typeargs), Offset.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class ArrayOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ArrayOperator(Operation, ArrayValue.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }
    
    partial class SizeofOperator
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SizeofOperator(TypeToMeasure.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class MemoryGet
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MemoryGet(Type.SubstituteWithTypearg(typeargs), Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class MemorySet
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MemorySet(Type.SubstituteWithTypearg(typeargs), Address.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class MarshalHandleIntoArray
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MarshalHandleIntoArray(ElementType.SubstituteWithTypearg(typeargs), Length.SubstituteWithTypearg(typeargs), Address.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class MarshalMemorySpanIntoArray
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new MarshalMemorySpanIntoArray(Span.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class BinaryOperator
    {
        public abstract IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs);
    }

    partial class ComparativeOperator
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ComparativeOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class LogicalOperator
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new LogicalOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class BitwiseOperator
    {
        public override IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new BitwiseOperator(Operation, Left.SubstituteWithTypearg(typeargs), Right.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class GetValueAtIndex
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new GetValueAtIndex(Type.SubstituteWithTypearg(typeargs), Array.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class SetValueAtIndex
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetValueAtIndex(Array.SubstituteWithTypearg(typeargs), Index.SubstituteWithTypearg(typeargs), Value.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class GetPropertyValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs)
        {
            Debug.Assert(Refinements == null);
            return new GetPropertyValue(Record.SubstituteWithTypearg(typeargs), Property.Name, null, ErrorReportedElement);
        }
    }

    partial class SetPropertyValue
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetPropertyValue(Record.SubstituteWithTypearg(typeargs), Property.SubstituteWithTypeargs(typeargs), Value.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class ReleaseReferenceElement
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new ReleaseReferenceElement(ReferenceBox.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }

    partial class SetReferenceTypeElement
    {
        public IRValue SubstituteWithTypearg(Dictionary<TypeParameter, IType> typeargs) => new SetReferenceTypeElement(ReferenceBox.SubstituteWithTypearg(typeargs), NewElement.SubstituteWithTypearg(typeargs), ErrorReportedElement);
    }
}