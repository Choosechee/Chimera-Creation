using AnomalyAllies.DefModExtensions;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace AnomalyAllies.Patches
{
    /*[HarmonyPatch(typeof(CompHoldingPlatformTarget), nameof(CompHoldingPlatformTarget.Escape))]
    static class ForcedAnimalGoesManhunterOnEscape
    {
        private static MethodInfo getDownedMethod = typeof(Pawn).GetProperty("Downed", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(false);
        private static MethodInfo manhunterIfForcedAnimalMethod = AccessTools.Method(typeof(ForcedAnimalGoesManhunterOnEscape), "ManhunterIfForcedAnimal");

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> oldInstructions)
        {
            List<CodeInstruction> instructionsList = new List<CodeInstruction>(oldInstructions);
            int indexOfGetDownedCall = instructionsList.FirstIndexOf((CodeInstruction ci) => ci.Calls(getDownedMethod));
            if (indexOfGetDownedCall < 0)
                throw new Exception("Could not find a call to CompProperties_HoldingPlatformTarget.lookForTargetOnEscape");

            int indexOfNewInstructions = -1;
            for (int i = indexOfGetDownedCall + 1; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.opcode == OpCodes.Brtrue_S)
                {
                    indexOfNewInstructions = i + 1;
                    break;
                }
            }
            if (indexOfNewInstructions < 0)
                throw new Exception("Could not find an appropriate place to put the new instructions");

            CodeInstruction accessPawnLocalVariable = null;
            for (int i = indexOfGetDownedCall - 1; i > 0; i--)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.IsLdloc())
                {
                    accessPawnLocalVariable = instruction;
                    break;
                }
            }
            if (accessPawnLocalVariable is null)
                throw new Exception("Could not find instruction for pushing the pawn variable to the stack");

            AnomalyAlliesMod.Logger.Message($"Information about the instruction currently at the index where instructions should be added:\nInstruction: {instructionsList[indexOfNewInstructions]}\nOpcode: {instructionsList[indexOfNewInstructions].opcode}\nOperand: {instructionsList[indexOfNewInstructions].operand}");
            AnomalyAlliesMod.Logger.Message($"Information about the instruction currently before the index where instructions should be added:\nInstruction: {instructionsList[indexOfNewInstructions - 1]}\nOpcode: {instructionsList[indexOfNewInstructions - 1].opcode}\nOperand: {instructionsList[indexOfNewInstructions - 1].operand}");
            AnomalyAlliesMod.Logger.Message($"Information about the instruction currently after the index where instructions should be added:\nInstruction: {instructionsList[indexOfNewInstructions + 1]}\nOpcode: {instructionsList[indexOfNewInstructions + 1].opcode}\nOperand: {instructionsList[indexOfNewInstructions + 1].operand}");
            AnomalyAlliesMod.Logger.Message($"The index is {indexOfNewInstructions}");

            List<CodeInstruction> newInstructions = new List<CodeInstruction>();
            newInstructions.Add(new CodeInstruction(accessPawnLocalVariable));
            newInstructions.Add(new CodeInstruction(OpCodes.Call, manhunterIfForcedAnimalMethod));

            instructionsList.InsertRange(indexOfNewInstructions, newInstructions);

            foreach (CodeInstruction instruction in instructionsList)
            {
                yield return instruction;
            }
        }

        public static void ManhunterIfForcedAnimal(Pawn pawn)
        {
            AnomalyAlliesMod.Logger.Message("ManhunterIfForcedAnimal is being run");
            if (pawn.RaceProps.ForceAnimal())
            {
                AnomalyAlliesMod.Logger.Message($"{pawn} is a forced animal");
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, transitionSilently: true);
            }
            else
                AnomalyAlliesMod.Logger.Message($"{pawn} is not a forced animal");
        }
    }*/
}
