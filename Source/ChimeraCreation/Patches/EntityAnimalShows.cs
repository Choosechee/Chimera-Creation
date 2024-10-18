using HarmonyLib;
using InterfacesForModularity;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AnomalyAllies.Patches
{
    static class EntityAnimalShows
    {
        private static readonly MethodInfo isAnomalyEntityGetter = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.IsAnomalyEntity));
        private static readonly MethodInfo fieldProviderGetter = AccessTools.PropertyGetter(typeof(AnomalyAlliesMod), nameof(AnomalyAlliesMod.FieldProvider));
        private static readonly MethodInfo forcedAnimalMethod = AccessTools.Method(typeof(ICustomFieldsProvider), nameof(ICustomFieldsProvider.EntityAnimal));

        [HarmonyPatch(typeof(ITab_Pawn_Social), nameof(ITab_Pawn_Social.IsVisible), MethodType.Getter)]
        static class SocialTab
        {
            static bool Postfix(bool __result, ITab_Pawn_Social __instance)
            {
                Pawn selPawnForSocialInfo = __instance.ForceGetProperty<Pawn>("SelPawnForSocialInfo");
                return __result || AnomalyAlliesMod.FieldProvider.EntityAnimal(selPawnForSocialInfo.RaceProps);
            }
        }

        [HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.SpecialDisplayStats), MethodType.Enumerator)]
        //[HarmonyDebug]
        static class AnimalStats
        {
            private static readonly FieldInfo raceField = typeof(ThingDef).Field(nameof(ThingDef.race));
            
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                codeMatcher.Start();

                codeMatcher.MatchStartForward(CodeMatch.Calls(isAnomalyEntityGetter));
                codeMatcher.ThrowIfInvalid("Could not find call for IsAnomalyEntity getter");

                codeMatcher.MatchStartBackwards(new CodeMatch((ci) => ci.opcode == OpCodes.Ldfld && !ci.OperandIs(raceField)));
                codeMatcher.ThrowIfInvalid("Could not find parentDef field");
                AnomalyAlliesMod.Logger.Message(codeMatcher.Instruction.operand);
                CodeInstruction loadParentDef = codeMatcher.Instruction.Clone();

                codeMatcher.MatchStartForward(CodeMatch.Calls(isAnomalyEntityGetter));
                codeMatcher.MatchStartForward(CodeMatch.Branches());
                codeMatcher.ThrowIfInvalid("Could not find branch after IsAnomalyEntity getter");

                Label showAnimalStats = generator.DefineLabel();
                codeMatcher.InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse_S, showAnimalStats));
                
                List<CodeInstruction> newInstructions = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Call, fieldProviderGetter),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    loadParentDef,
                    new CodeInstruction(OpCodes.Ldfld, raceField),
                    new CodeInstruction(OpCodes.Callvirt, forcedAnimalMethod),
                    new CodeInstruction(OpCodes.Ldind_I1),
                };
                codeMatcher.InsertAndAdvance(newInstructions);
                codeMatcher.SetOpcodeAndAdvance(OpCodes.Brfalse_S);
                codeMatcher.Instruction.labels.Add(showAnimalStats);

                return codeMatcher.InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.ShouldShowFor))]
        static class StatsNotForEntities
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                codeMatcher.Start();

                codeMatcher.MatchStartForward(CodeMatch.Calls(isAnomalyEntityGetter));
                codeMatcher.ThrowIfInvalid("Could not find call for IsAnomalyEntity getter");

                codeMatcher.MatchStartBackwards(CodeMatch.IsLdloc());
                codeMatcher.ThrowIfInvalid("Could not find ldloc instruction");
                CodeInstruction loadThingDefVar = codeMatcher.Instruction.Clone();

                codeMatcher.MatchStartForward(CodeMatch.WithOpcodes(new HashSet<OpCode> { OpCodes.Ldfld }));
                codeMatcher.ThrowIfInvalid("Could not find ldfld instruction");
                CodeInstruction loadRacePropertiesField = codeMatcher.Instruction.Clone();

                codeMatcher.MatchStartForward(CodeMatch.Branches());
                codeMatcher.ThrowIfInvalid("Could not find branch after ldfld instruction");
                Label escapeReturningFalseLabel = (Label)codeMatcher.Instruction.operand;

                codeMatcher.Advance(1);

                List<CodeInstruction> newInstructions = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Call, fieldProviderGetter),
                    loadThingDefVar,
                    loadRacePropertiesField,
                    new CodeInstruction(OpCodes.Callvirt, forcedAnimalMethod),
                    new CodeInstruction(OpCodes.Ldind_I1),
                    new CodeInstruction(OpCodes.Brtrue_S, escapeReturningFalseLabel)
                };
                codeMatcher.Insert(newInstructions);

                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}
