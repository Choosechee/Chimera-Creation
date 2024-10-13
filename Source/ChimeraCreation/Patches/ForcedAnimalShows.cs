using HarmonyLib;
using InterfacesForModularity;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AnomalyAllies.Patches
{
    static class ForcedAnimalShows
    {
        [HarmonyPatch(typeof(ITab_Pawn_Social), nameof(ITab_Pawn_Social.IsVisible), MethodType.Getter)]
        static class SocialTab
        {
            static bool Postfix(bool __result, ITab_Pawn_Social __instance)
            {
                Pawn selPawnForSocialInfo = __instance.ForceGetProperty<Pawn>("SelPawnForSocialInfo");
                return __result || AnomalyAlliesMod.FieldProvider.IsForcedAnimal(selPawnForSocialInfo.RaceProps);
            }
        }

        [HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.SpecialDisplayStats))]
        static class AnimalStats
        {
            static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> __result, RaceProperties __instance)
            {
                if (!AnomalyAlliesMod.FieldProvider.IsForcedAnimal(__instance))
                    return __result;

                List<StatDrawEntry> statDrawEntries = new List<StatDrawEntry>(__result);
                
                string dietLabel = "Diet".Translate();
                string dietText = __instance.foodType.ToHumanString().CapitalizeFirst();
                string dietDesc = "Stat_Race_Diet_Desc".Translate(dietText);
                statDrawEntries.Add(new StatDrawEntry(StatCategoryDefOf.BasicsPawn, dietLabel, dietText, dietDesc, StatDisplayOrder.Race_Diet));

                if ((int)__instance.intelligence < 2 && __instance.trainability != null)
                {
                    string trainabilityLabel = "Trainability".Translate();
                    string trainabilityText = __instance.trainability.LabelCap;
                    string trainabilityDesc = "Stat_Race_Trainability_Desc".Translate();
                    statDrawEntries.Add(new StatDrawEntry(StatCategoryDefOf.BasicsPawn, trainabilityLabel, trainabilityText, trainabilityDesc, StatDisplayOrder.Race_Trainability));
                }

                return statDrawEntries;
            }
        }

        [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.ShouldShowFor))]
        static class StatsNotForEntities
        {
            private static MethodInfo isAnomalyEntityGetter = AccessTools.PropertyGetter(typeof(RaceProperties), "IsAnomalyEntity");
            private static MethodInfo fieldProviderGetter = AccessTools.PropertyGetter(typeof(AnomalyAlliesMod), "FieldProvider");
            private static MethodInfo isForcedAnimalMethod = AccessTools.Method(typeof(ICustomFieldsProvider), "IsForcedAnimal");

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
                    new CodeInstruction(OpCodes.Callvirt, isForcedAnimalMethod),
                    new CodeInstruction(OpCodes.Brtrue_S, escapeReturningFalseLabel)
                };
                codeMatcher.Insert(newInstructions);

                return codeMatcher.InstructionEnumeration();
            }
        }
    }
}
