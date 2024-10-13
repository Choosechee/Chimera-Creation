using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AnomalyAllies.Misc
{
    public static class OpcodeUtility
    {
        public static Dictionary<string, string> oppositePrefixes = new Dictionary<string, string>
        (
            new List<KeyValuePair<string, string>>()
            {
                KeyValuePair.Create("st", "ld"),
                KeyValuePair.Create("ld", "st")
            }
        );
        private const string noOppositeError = "Opcode {0} does not have a defined opposite";
        private const BindingFlags publicStatic = BindingFlags.Public | BindingFlags.Static;


        public static CodeInstruction Opposite(this CodeInstruction instruction)
        {
            if (instruction == null) throw new ArgumentNullException(nameof(instruction));

            OpCode opCode = instruction.opcode;

            string prefix = opCode.Name.Substring(0, 2);
            if (!oppositePrefixes.ContainsKey(prefix)) throw new ArgumentException(string.Format(noOppositeError, opCode.Name));
            string oppositePrefix = oppositePrefixes[prefix];

            string oppositeOpCodeName = opCode.Name.ReplaceFirst(prefix, oppositePrefix);
            string[] oppositeOpCodeSplit = oppositeOpCodeName.Split('.');
            for (int i = 0; i < oppositeOpCodeSplit.Length; i++)
                oppositeOpCodeSplit[i] = oppositeOpCodeSplit[i].CapitalizeFirst();

            string oppositeOpCodeFieldName = string.Join("_", oppositeOpCodeSplit);
            FieldInfo oppositeOpCodeField = typeof(OpCodes).GetField(oppositeOpCodeFieldName, publicStatic);
            if (oppositeOpCodeField is null) throw new ArgumentException(string.Format(noOppositeError, opCode.Name));
            OpCode oppositeOpCode = (OpCode)oppositeOpCodeField.GetValue(null);

            return new CodeInstruction(oppositeOpCode, instruction.operand);
        }
    }
}
