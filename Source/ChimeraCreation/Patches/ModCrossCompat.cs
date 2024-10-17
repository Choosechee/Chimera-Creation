using AnomalyAllies.Comps;
using AnomalyAllies.DefOfs;
using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace AnomalyAllies.Patches
{
    [StaticConstructorOnStartup]
    public static class ModCrossCompat
    {
        public const string requiresMoreArchotechGarbage = "Requires_MoreArchotechGarbage";

        static ModCrossCompat()
        {
            PotentiallyPatchMoreArchotechGarbage();
        }

        static void PotentiallyPatchMoreArchotechGarbage()
        {
            ModMetaData moreArchotechGarbageMetaData = ModLister.AllInstalledMods.FirstOrDefault((ModMetaData mmd) => mmd.packageIdLowerCase.Contains("morearchotechgarbage") && mmd.Active);
            if (moreArchotechGarbageMetaData is null)
                return;

            AnomalyAlliesMod.Logger.Message("Found More Archotech Garbage. Attempting to patch CompTargetEffect_ForcedRecruit");
                
            ModContentPack moreArchotechGarbageContent = LoadedModManager.RunningMods.FirstOrDefault((ModContentPack mcp) => mcp.ModMetaData == moreArchotechGarbageMetaData);
            if (moreArchotechGarbageContent is null)
            {
                AnomalyAlliesMod.Logger.Error("Found metadata for More Archotech Garbage, but couldn't find its content pack");
                return;
            }

            Assembly moreArchotechGarbageAssembly = moreArchotechGarbageContent.assemblies.loadedAssemblies.FirstOrDefault();
            Type forcedReruit = moreArchotechGarbageAssembly?.GetType("ForcerRecruit.CompTargetEffect_ForcedRecruit");
            if (forcedReruit is null)
            {
                AnomalyAlliesMod.Logger.Error("Found metadata and content pack for More Archotech Garbage, but couldn't find CompTargetEffect_ForcedRecruit");
                return;
            }

            MethodInfo forcedRecruitDoEffectOn = forcedReruit.GetMethod("DoEffectOn", BindingFlags.Public | BindingFlags.Instance);
            MoreArchotechGarbagePatch.targetMethod = forcedRecruitDoEffectOn;

            // this fixes a null reference exception from an effect attempting
            // to be applied at the location of the chimera after it despawns
            // and is replaced by the tame version
            ThingDef psychicRecruiter = DefDatabase<ThingDef>.GetNamed("RecruitArtifact");
            int forcedRecruitCompIndex = psychicRecruiter.comps.FindIndex((CompProperties comp) => comp.compClass == forcedReruit);
            psychicRecruiter.comps.Swap(forcedRecruitCompIndex, psychicRecruiter.comps.Count - 1);

            AnomalyAlliesMod.Harmony.PatchCategory(requiresMoreArchotechGarbage);
            AnomalyAlliesMod.Logger.Message("Successfully patched More Archotech Garbage");
        }

        [HarmonyPatch]
        [HarmonyPatchCategory(requiresMoreArchotechGarbage)]
        static class MoreArchotechGarbagePatch
        {
            internal static MethodInfo targetMethod;

            static MethodBase TargetMethod()
            {
                return targetMethod;
            }

            static void Prefix(ref Thing target)
            {
                Pawn pawn = target as Pawn;
                if (pawn.kindDef == AlliedEntityDefOf.AnAl_ChimeraTame && pawn.Faction == Faction.OfEntities)
                    pawn.SetFaction(null);
                else if (pawn.IsEntity && pawn.TryGetComp(out CompTransform compTransform))
                {
                    pawn = compTransform.TransformPawn();
                    pawn.SetFaction(null);
                    target = pawn;
                }
            }
        }
    }
}
