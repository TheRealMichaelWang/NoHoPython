using NoHoPython.IntermediateRepresentation;
using NoHoPython.IntermediateRepresentation.Statements;
using NoHoPython.Typing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.Syntax
{
    partial class AstIRProgramBuilder
    {
        private HashSet<ReferenceType> usedReferenceTypes = new(new ITypeComparer());

        public bool DeclareUsedReferenceType(ReferenceType referenceType)
        {
            if (usedReferenceTypes.Contains(referenceType))
                return false;
            usedReferenceTypes.Add(referenceType);

            DeclareTypeDependencies(referenceType, referenceType.ElementType);
            return true;
        }
    }
}

namespace NoHoPython.IntermediateRepresentation
{
    partial class IRProgram
    {
        public List<ReferenceType> usedReferenceTypes;

        public void EmitReferenceTypedefs(Emitter emitter)
        {
            foreach (ReferenceType referenceType in usedReferenceTypes)
                emitter.AppendLine($"typedef struct {referenceType.GetStandardIdentifier(this)} {referenceType.GetStandardIdentifier(this)}_t;");
        }

        public void EmitReferenceTypeCStructs(Emitter emitter)
        {
            foreach (ReferenceType referenceType in usedReferenceTypes)
                referenceType.EmitCStruct(this, emitter);
        }

        public void ForwardDeclareReferenceTypes(Emitter emitter)
        {
            foreach (ReferenceType referenceType in usedReferenceTypes)
            {
                referenceType.EmitDestructorHeader(this, emitter);
                emitter.AppendLine(";");
            }
        }

        public void EmitReferenceTypeMarshallers(Emitter emitter)
        {
            foreach (ReferenceType referenceType in usedReferenceTypes)
                referenceType.EmitDestructor(this, emitter);
        }
    }
}

namespace NoHoPython.Typing
{
    partial class ReferenceType
    {
        public sealed class CannotAccessElement : CodegenError
        {
            public CannotAccessElement() : base(null, "Cannot access element from a reference type that has potentially been already released.")
            {

            }
        }

        partial class ElementProperty
        {
            public override bool RequiresDisposal(Dictionary<TypeParameter, IType> typeargs) => false;

            public override bool EmitGet(IRProgram irProgram, Emitter emitter, Dictionary<TypeParameter, IType> typeargs, IPropertyContainer propertyContainer, Emitter.Promise value, Emitter.Promise responsibleDestroyer)
            {
                if (ReferenceType.Mode >= ReferenceMode.Released)
                    throw new CannotAccessElement();

                value(emitter);
                emitter.Append("->elem");
                return false;
            }
        }

        public bool IsNativeCType => false;
        public bool RequiresDisposal => true;
        public bool MustSetResponsibleDestroyer => ElementType.MustSetResponsibleDestroyer;
        public bool IsTypeDependency => false;
        public bool IsCircularDataStructure => ContainsType(this);

        public bool TypeParameterAffectsCodegen(Dictionary<IType, bool> effectInfo) => ElementType.TypeParameterAffectsCodegen(effectInfo);

        public string GetStandardIdentifier(IRProgram irProgram) => $"rc_{ElementType.GetStandardIdentifier(irProgram)}";

        public string GetCName(IRProgram irProgram) => $"{GetStandardIdentifier(irProgram)}_t*";

        public void EmitCStruct(IRProgram irProgram, Emitter emitter)
        {
            if (!irProgram.DeclareCompiledType(emitter, this))
                return;

            emitter.AppendStartBlock($"struct {GetStandardIdentifier(irProgram)}");
            if (IsCircularDataStructure)
                emitter.AppendLine("nhp_trace_obj_t trace_unit;");
            else
                emitter.AppendLine("nhp_rc_obj_t rc_unit;");
            emitter.AppendLine("int is_released;");
            emitter.AppendLine($"{ElementType.GetCName(irProgram)} elem;");
            emitter.AppendEndBlock();
        }
        
        public void EmitFreeValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valuePromise, Emitter.Promise childAgent)
        {
            emitter.Append($"free_{GetStandardIdentifier(irProgram)}(");
            valuePromise(emitter);
            if (IsCircularDataStructure)
            {
                emitter.Append(", (nhp_trace_obj_t*)");
                childAgent(emitter);
            }
            emitter.AppendLine(");");
        }

        public void EmitCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer)
        {
            if (IsCircularDataStructure)
            {
                emitter.Append($"({GetCName(irProgram)})nhp_trace_add_parent((nhp_trace_obj_t*)");
                valueCSource(emitter);
                emitter.Append(", (nhp_trace_obj_t*)");
                responsibleDestroyer(emitter);
            }
            else
            {
                emitter.Append($"({GetCName(irProgram)})nhp_rc_ref((nhp_rc_obj_t*)");
                valueCSource(emitter);
            }
            emitter.Append(')');
        }

        public void EmitClosureBorrowValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise responsibleDestroyer) => EmitCopyValue(irProgram, emitter, valueCSource, responsibleDestroyer);
        public void EmitRecordCopyValue(IRProgram irProgram, Emitter emitter, Emitter.Promise valueCSource, Emitter.Promise newRecord) => EmitCopyValue(irProgram, emitter, valueCSource, newRecord);

        public void ScopeForUsedTypes(Syntax.AstIRProgramBuilder irBuilder)
        {
            if (!irBuilder.DeclareUsedReferenceType(this))
                return;
            ElementType.ScopeForUsedTypes(irBuilder);
        }

        public void EmitDestructorHeader(IRProgram irProgram, Emitter emitter)
        {
            emitter.Append($"void free_{GetStandardIdentifier(irProgram)}({GetCName(irProgram)} ref_obj");
            if (IsCircularDataStructure)
                emitter.Append(", nhp_trace_obj_t* freeing_parent");
            emitter.Append(")");
        }

        public void EmitDestructor(IRProgram irProgram, Emitter emitter)
        {
            EmitDestructorHeader(irProgram, emitter);
            emitter.AppendStartBlock();

            if (IsCircularDataStructure)
            {
                emitter.AppendLine($"if(!nhp_trace_del_parent((nhp_trace_obj_t*)ref_obj, freeing_parent)) {{ return; }}");
                emitter.AppendLine($"if(nhp_trace_reachable((nhp_trace_obj_t*)ref_obj)) {{ return; }}");
                emitter.AppendLine("if(ref_obj->trace_unit.nhp_lock) { return; } //lock for circular deletions");
                emitter.AppendLine("ref_obj->trace_unit.nhp_lock = 1;");
            }
            else
            {
                emitter.AppendStartBlock("if(ref_obj->rc_unit.nhp_count)");
                emitter.AppendLine("ref_obj->rc_unit.nhp_count--;");
                emitter.AppendLine("return;");
                emitter.AppendEndBlock();
            }

            emitter.AppendStartBlock("if(ref_obj->is_released)");
            ElementType.EmitFreeValue(irProgram, emitter, e => e.Append("ref_obj->elem"), e => e.Append("ref_obj"));
            emitter.AppendEndBlock();

            emitter.AppendLine(irProgram.MemoryAnalyzer.Dealloc("ref_obj", $"sizeof({GetStandardIdentifier(irProgram)}_t)"));
            emitter.AppendEndBlock();
        }
    }
}