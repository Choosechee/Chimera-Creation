//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using RimWorld;
using AnomalyAllies.ChimeraTame;
using HarmonyLib;
using Verse;

namespace AnomalyAllies.Patches
{
    // why does "Comment selection" comment it out like this instead of using a multiline comment

    //[HarmonyPatch(typeof(Recipe_Surgery), nameof(Recipe_Surgery.AvailableOnNow))]
    //static class DebugSurgeries
    //{
    //    static void Postfix(Recipe_Surgery __instance, bool __result, Thing thing)
    //    {
    //        if (__result)
    //            return;

    //        StringBuilder reasonNotAvailable = new StringBuilder();
    //        if (!(thing is Pawn pawn))
    //        {
    //            reasonNotAvailable.AppendLine($"{thing} is not a pawn");
    //        }
    //        else
    //        {
    //            if ((__instance.recipe.genderPrerequisite ?? pawn.gender) != pawn.gender)
    //            {
    //                reasonNotAvailable.AppendLine($"Recipe requires gender {__instance.recipe.genderPrerequisite}, but {pawn} is {pawn.gender}");
    //            }
    //            if (__instance.recipe.mustBeFertile && pawn.Sterile())
    //            {
    //                reasonNotAvailable.AppendLine($"Recipe requires fertile pawn, but {pawn} is sterile");
    //            }
    //            if (!__instance.recipe.allowedForQuestLodgers && pawn.IsQuestLodger())
    //            {
    //                reasonNotAvailable.AppendLine($"Recipe requires pawn to not be a quest lodger, but {pawn} is one");
    //            }
    //            if (__instance.recipe.minAllowedAge > 0 && pawn.ageTracker.AgeBiologicalYears < __instance.recipe.minAllowedAge)
    //            {
    //                reasonNotAvailable.AppendLine($"Recipe requires a minimum age of {__instance.recipe.minAllowedAge}, but {pawn} is only {pawn.ageTracker.AgeBiologicalYears}");
    //            }
    //            if (__instance.recipe.developmentalStageFilter.HasValue && !__instance.recipe.developmentalStageFilter.Value.Has(pawn.DevelopmentalStage))
    //            {
    //                reasonNotAvailable.AppendLine($"{pawn} is {pawn.DevelopmentalStage}, but this recipe can't be done on this developmental stage");
    //            }
    //            if (__instance.recipe.humanlikeOnly && !pawn.RaceProps.Humanlike)
    //            {
    //                reasonNotAvailable.AppendLine($"Recipe requires a humanlike, but {pawn} isn't one");
    //            }
    //            if (ModsConfig.AnomalyActive)
    //            {
    //                if (__instance.recipe.mutantBlacklist is not null && pawn.IsMutant && __instance.recipe.mutantBlacklist.Contains(pawn.mutant.Def))
    //                {
    //                    reasonNotAvailable.AppendLine($"{pawn} is a {pawn.mutant.Def.defName}, but this recipe blacklists them");
    //                }
    //                if (__instance.recipe.mutantPrerequisite is not null && (!pawn.IsMutant || !__instance.recipe.mutantPrerequisite.Contains(pawn.mutant.Def)))
    //                {
    //                    reasonNotAvailable.AppendLine($"{pawn} is not a mutant required for the recipe. The eligible mutant types are: ");
    //                    foreach (MutantDef mutantDef in __instance.recipe.mutantPrerequisite)
    //                        reasonNotAvailable.AppendWithComma(mutantDef.defName);
    //                }
    //            }
    //        }

    //        AnomalyAlliesMod.Logger.Message($"{thing} cannot have operation {__instance.recipe.defName} because:\n{reasonNotAvailable}");
    //    }
    //}

    [HarmonyPatch(typeof(PlayDataLoader), nameof(PlayDataLoader.HotReloadDefs))]
    static class ResetupDefs
    {
        static void Postfix()
        {
            Setup.Run(); 
        }
    }
}
