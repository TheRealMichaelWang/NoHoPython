using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System.Diagnostics;

namespace NoHoPython.IntermediateRepresentation.Values
{
    partial class IntegerLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append(Number.ToString()));
    }

    partial class DecimalLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append(Number.ToString()));
    }

    partial class CharacterLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public static void EmitCChar(Emitter emitter, char c, bool formatChar)
        {
            switch (c)
            {
                case '\\':
                    emitter.Append("\\\\");
                    break;
                case '\"':
                    emitter.Append("\\\"");
                    break;
                case '\'':
                    emitter.Append("\\\'");
                    break;
                case '\a':
                    emitter.Append("\\a");
                    break;
                case '\b':
                    emitter.Append("\\b");
                    break;
                case '\f':
                    emitter.Append("\\f");
                    break;
                case '\t':
                    emitter.Append("\\t");
                    break;
                case '\r':
                    emitter.Append("\\r");
                    break;
                case '\n':
                    emitter.Append("\\n");
                    break;
                case '\0':
                    emitter.Append("\\0");
                    break;
                case '%':
                    emitter.Append(formatChar ? "%%" : "%");
                    break;
                default:
                    Debug.Assert(!char.IsControl(c));
                    emitter.Append(c);
                    break;
            }
        }

        public static void EmitCString(Emitter emitter, string str, bool formatStr, bool quoteEncapsulate)
        {
            if(quoteEncapsulate)
                emitter.Append('\"');

            foreach (char c in str)
                EmitCChar(emitter, c, formatStr);
            
            if(quoteEncapsulate)
                emitter.Append('\"');
        }

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
            emitter.Append('\'');
            EmitCChar(emitter, Character, false);
            emitter.Append('\'');
        });
    }

    partial class TrueLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append('1'));
    }

    partial class FalseLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append('0'));
    }

    partial class NullPointerLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        
        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) => emitter.Append("NULL"));
    }

    partial class StaticCStringLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
            emitter.Append('\"');
            foreach (char c in String)
                CharacterLiteral.EmitCChar(emitter, c, false);
            emitter.Append('\"');
        });
    }

    partial class EmptyTypeLiteral
    {
        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;
        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder) { }
        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) { }
    }

    partial class ArrayLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Elements.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => !(isTemporaryEval && !Elements.Any((elem) => elem.RequiresDisposal(irProgram, typeargs, true)) && !Elements.Any((elem) => elem.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval)) && IRValue.EvaluationOrderGuarenteed(Elements.ToArray()));

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => RequiresDisposal(irProgram, typeargs, isTemporaryEval);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();

                primaryEmitter.AppendLine($"{ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}* res{indirection} = {irProgram.MemoryAnalyzer.Allocate($"{Elements.Count} * sizeof({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)})")};");
                for (int i = 0; i < Elements.Count; i++)
                {
                    Elements[i].Emit(irProgram, primaryEmitter, typeargs, (elemPromise) =>
                    {
                        primaryEmitter.Append($"res{indirection}[{i}] = ");

                        if (Elements[i].RequiresDisposal(irProgram, typeargs, isTemporaryEval) || !RequiresDisposal(irProgram, typeargs, isTemporaryEval))
                            elemPromise(primaryEmitter);
                        else
                            ElementType.EmitCopyValue(irProgram, primaryEmitter, elemPromise, responsibleDestroyer, Elements[i]);

                        primaryEmitter.AppendLine(';');
                    }, responsibleDestroyer, isTemporaryEval);
                    if (ElementType.SubstituteWithTypearg(typeargs).RequiresDisposal)
                        primaryEmitter.AddResourceDestructor((emitter) => ElementType.SubstituteWithTypearg(typeargs).EmitFreeValue(irProgram, emitter, (e) => e.Append($"res[{i}]"), Emitter.NullPromise));
                }
                primaryEmitter.AssumeBlockResources();
                destination((emitter) => primaryEmitter.Append($"res{indirection}"));
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    if (Elements.All((elem) => elem is CharacterLiteral))
                    {
                        emitter.Append('\"');
                        Elements.ForEach((elem) => CharacterLiteral.EmitCChar(emitter, ((CharacterLiteral)elem).Character, false));
                        emitter.Append('\"');
                    }
                    else
                    {
                        emitter.Append($"({ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}[])");
                        emitter.Append('{');

                        for (int i = 0; i < Elements.Count; i++)
                        {
                            if (i > 0)
                                emitter.Append(", ");

                            IRValue.EmitDirect(irProgram, emitter, Elements[i], typeargs, responsibleDestroyer, true);
                        }
                        emitter.Append('}');
                    }
                });
        }
    }

    partial class TupleLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Elements.ForEach((element) => element.ScopeForUsedTypes(typeargs, irBuilder));
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => !(isTemporaryEval && !Elements.Any(element => element.RequiresDisposal(irProgram, typeargs, true)));

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Elements.Any((elem) => elem.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval)) || !IRValue.EvaluationOrderGuarenteed(Elements.ToArray());

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            List<Property> initializeProperties = ((TupleType)TupleType.SubstituteWithTypearg(typeargs)).GetProperties();
            ITypeComparer typeComparer = new ITypeComparer();
            initializeProperties.Sort((a, b) => typeComparer.Compare(a.Type, b.Type));
            Elements.Sort((a, b) => typeComparer.Compare(a.Type, b.Type));

            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();

                primaryEmitter.AppendLine($"{TupleType.SubstituteWithTypearg(typeargs).GetCName(irProgram)} res{indirection};");
                for (int i = 0; i < initializeProperties.Count; i++)
                {
                    Elements[i].Emit(irProgram, primaryEmitter, typeargs, (elemPromise) =>
                    {
                        primaryEmitter.Append($"res{indirection}.{initializeProperties[i].Name} = ");
                        if (!RequiresDisposal(irProgram, typeargs, isTemporaryEval) || Elements[i].RequiresDisposal(irProgram, typeargs, isTemporaryEval))
                            elemPromise(primaryEmitter);
                        else
                            initializeProperties[i].Type.EmitCopyValue(irProgram, primaryEmitter, elemPromise, responsibleDestroyer, Elements[i]);
                        primaryEmitter.AppendLine(';');
                    }, responsibleDestroyer, isTemporaryEval);

                    if (RequiresDisposal(irProgram, typeargs, isTemporaryEval) && initializeProperties[i].Type.RequiresDisposal)
                        primaryEmitter.AddResourceDestructor((emitter) => initializeProperties[i].Type.EmitFreeValue(irProgram, emitter, (e) => e.Append($"res{indirection}.{initializeProperties[i].Name}"), Emitter.NullPromise));
                }

                primaryEmitter.AssumeBlockResources();
                destination((emitter) => primaryEmitter.Append($"res{indirection}"));
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append($"({TupleType.SubstituteWithTypearg(typeargs).GetCName(irProgram)}) {{");
            
                    for(int i = 0; i < initializeProperties.Count; i++)
                    {
                        if (i > 0)
                            emitter.Append(", ");

                        emitter.Append($".{initializeProperties[i].Name} = ");

                        Debug.Assert(!Elements[i].MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval));
                        if (!RequiresDisposal(irProgram, typeargs, isTemporaryEval))
                            IRValue.EmitDirect(irProgram, emitter, Elements[i], typeargs, Emitter.NullPromise, true);
                        else
                        {
                            if (Elements[i].RequiresDisposal(irProgram, typeargs, false))
                                IRValue.EmitDirect(irProgram, emitter, Elements[i], typeargs, responsibleDestroyer, false);
                            else
                                initializeProperties[i].Type.EmitCopyValue(irProgram, emitter, IRValue.EmitDirectPromise(irProgram, Elements[i], typeargs, responsibleDestroyer, false), responsibleDestroyer, Elements[i]);
                        }
                    }

                    emitter.Append('}');
                });
        }
    }

    partial class ReferenceLiteral
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Input.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            ReferenceType referenceType = (ReferenceType)Type.SubstituteWithTypearg(typeargs);

            int indirection = primaryEmitter.AppendStartBlock();
            primaryEmitter.AppendLine($"{referenceType.GetCName(irProgram)} rc{indirection} = {irProgram.MemoryAnalyzer.Allocate($"sizeof({referenceType.GetStandardIdentifier(irProgram)}_t)")};");
            if (referenceType.IsCircularDataStructure)
            {
                primaryEmitter.AppendLine($"rc{indirection}->trace_unit.nhp_parent_count = 0;");
                primaryEmitter.AppendLine($"rc{indirection}->trace_unit.nhp_lock = 0;");
                primaryEmitter.AppendLine($"nhp_trace_add_parent((nhp_trace_obj_t*)rc{indirection}, parent_record);");
            }
            else
                primaryEmitter.AppendLine($"rc{indirection}->rc_unit.nhp_count = 0;");
            
            if(referenceType.ElementType.RequiresDisposal)
                primaryEmitter.AppendLine($"rc{indirection}->is_released = 0;");
            
            Input.Emit(irProgram, primaryEmitter, typeargs, promise =>
            {
                primaryEmitter.Append($"rc{indirection}->elem = ");
                if (Input.RequiresDisposal(irProgram, typeargs, false))
                    promise(primaryEmitter);
                else
                    Input.Type.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, promise, e => e.Append($"rc{indirection}->elem"), Input);
                primaryEmitter.AppendLine(';');
            }, e => e.Append($"rc{indirection}->elem"), isTemporaryEval);
            destination(emitter => emitter.Append($"rc{indirection}"));
            primaryEmitter.AppendEndBlock();
        }
    }

    partial class AllocArray
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            Length.ScopeForUsedTypes(typeargs, irBuilder);
            ProtoValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => Length.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval) || ProtoValue.MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval) || !IRValue.EvaluationOrderGuarenteed(Length, ProtoValue);

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval)
        {
            if (MustUseDestinationPromise(irProgram, typeargs, isTemporaryEval))
            {
                int indirection = primaryEmitter.AppendStartBlock();
                primaryEmitter.AppendLine($"long length{indirection}; {ElementType.SubstituteWithTypearg(typeargs).GetCName(irProgram)} proto_val{indirection}");
                primaryEmitter.SetArgument(Length, $"length{indirection}", irProgram, typeargs, false);
                ProtoValue.Emit(irProgram, primaryEmitter, typeargs, (protoPromise) =>
                {
                    primaryEmitter.Append($"proto_val{indirection} = ");

                    if (ProtoValue.RequiresDisposal(irProgram, typeargs, true))
                        protoPromise(primaryEmitter);
                    else
                        ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, protoPromise, Emitter.NullPromise, ProtoValue);

                    primaryEmitter.AppendLine(';');
                }, Emitter.NullPromise, true);
                destination((emitter) =>
                {
                    emitter.Append($"marshal_proto{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(len{indirection}, proto_val{indirection}");

                    if (ElementType.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
                    {
                        primaryEmitter.Append(", ");
                        responsibleDestroyer(primaryEmitter);
                    }
                    primaryEmitter.Append(')');
                });
                primaryEmitter.AppendEndBlock();
            }
            else
                destination((emitter) =>
                {
                    emitter.Append($"marshal_proto{Type.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");
                    IRValue.EmitDirect(irProgram, emitter, Length, typeargs, Emitter.NullPromise, true);
                    emitter.Append(", ");
                    ProtoValue.Emit(irProgram, primaryEmitter, typeargs, (protoValPromise) =>
                    {
                        if (ProtoValue.RequiresDisposal(irProgram, typeargs, true))
                            protoValPromise(primaryEmitter);
                        else
                            ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, primaryEmitter, protoValPromise, Emitter.NullPromise, ProtoValue);
                    }, Emitter.NullPromise, true);

                    if (ElementType.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
                    {
                        primaryEmitter.Append(", ");
                        responsibleDestroyer(primaryEmitter);
                    }
                    primaryEmitter.Append(')');
                });
        }
    }

    partial class AllocMemorySpan
    {
        public void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            Type.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            ProtoValue.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public bool RequiresDisposal(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => true;

        public bool MustUseDestinationPromise(IRProgram irProgram, Dictionary<TypeParameter, IType> typeargs, bool isTemporaryEval) => false;

        public void Emit(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, Emitter.SetPromise destination, Emitter.Promise responsibleDestroyer, bool isTemporaryEval) => destination((emitter) =>
        {
            emitter.Append($"buffer_alloc_{ElementType.SubstituteWithTypearg(typeargs).GetStandardIdentifier(irProgram)}(");

            ProtoValue.Emit(irProgram, emitter, typeargs, (protoValPromise) =>
            {
                if (ProtoValue.RequiresDisposal(irProgram, typeargs, true))
                    protoValPromise(emitter);
                else
                    ElementType.SubstituteWithTypearg(typeargs).EmitCopyValue(irProgram, emitter, protoValPromise, Emitter.NullPromise, ProtoValue);
            }, Emitter.NullPromise, true);

            emitter.Append($", {Length}");

            if (ElementType.SubstituteWithTypearg(typeargs).MustSetResponsibleDestroyer)
            {
                primaryEmitter.Append(", ");
                responsibleDestroyer(primaryEmitter);
            }
            primaryEmitter.Append(')');
        });
    }

    partial class AllocRecord
    {
        public override void ScopeForUsedTypes(Dictionary<TypeParameter, IType> typeargs, Syntax.AstIRProgramBuilder irBuilder)
        {
            RecordPrototype.SubstituteWithTypearg(typeargs).ScopeForUsedTypes(irBuilder);
            base.ScopeForUsedTypes(typeargs, irBuilder);
        }

        public override void EmitCall(IRProgram irProgram, Emitter primaryEmitter, Dictionary<TypeParameter, IType> typeargs, List<Emitter.Promise> argPromises, Emitter.Promise responsibleDestroyer)
        {
            RecordType recordType = (RecordType)RecordPrototype.SubstituteWithTypearg(typeargs);
            primaryEmitter.Append($"construct_{recordType.GetOriginalStandardIdentifer(irProgram)}(");
            EmitArguments(primaryEmitter, argPromises);

            if (recordType.IsCircularDataStructure)
            {
                if (Arguments.Count > 0)
                    primaryEmitter.Append(", ");
                primaryEmitter.Append($"(nhp_trace_obj_t*)");
                responsibleDestroyer(primaryEmitter);
            }
            primaryEmitter.Append(')');
        }
    }
}
