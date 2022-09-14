using System.Text;

namespace NoHoPython.Typing
{
    partial class ProcedureType
    {
        private static List<Tuple<ProcedureType, string>> uniqueProcedureTypes = new();

        private static string defineCType(ProcedureType procedureType)
        {
            foreach (var uniqueProcedure in uniqueProcedureTypes)
                if (procedureType.IsCompatibleWith(uniqueProcedure.Item1))
                    return uniqueProcedure.Item2;
            string name = $"anonProcTypeNo{uniqueProcedureTypes.Count}";
            uniqueProcedureTypes.Add(new(procedureType, name));
            return name;
        }

        public static void EmitTypedefs(StringBuilder emitter)
        {
            static void EmitProcTypedef(StringBuilder emitter, Tuple<ProcedureType, string> procedureInfo)
            {
                static void emitType(StringBuilder emitter, IType type)
                {
                    if (type is RecordType ||
                       type is EnumType ||
                       type is InterfaceType ||
                       type is ProcedureType)
                        emitter.Append("void*");
                    else if (type is TypeParameterReference)
                        throw new InvalidOperationException();
                    else
                        emitter.Append(type.GetCName());
                }

                emitter.Append("typedef ");
                emitType(emitter, procedureInfo.Item1.ReturnType);
                emitter.Append(" (*");
                emitter.Append(procedureInfo.Item2);
                emitter.Append(")(");

                for (int i = 0; i < procedureInfo.Item1.ParameterTypes.Count; i++)
                {
                    if (i > 0)
                        emitter.Append(", ");
                    emitter.Append(procedureInfo.Item1.ParameterTypes[i]);
                }
                emitter.Append(");");
            }

            foreach (var uniqueProc in uniqueProcedureTypes)
            {
                EmitProcTypedef(emitter, uniqueProc);
                emitter.AppendLine();
            }
        }

        private Lazy<string> cName;

        public string GetCName() => cName.Value;
    }
}
