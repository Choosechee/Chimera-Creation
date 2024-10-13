using AnomalyAllies.DefModExtensions;
using HarmonyLib;
using Verse;

namespace AnomalyAllies.Patches
{
    [HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.Animal), MethodType.Getter)]
    static class ForceAnimalPatch
    {
        static bool Postfix(bool __result, RaceProperties __instance)
        {
            return __result || AnomalyAlliesMod.FieldProvider.IsForcedAnimal(__instance);
        }
    }

    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.ResolveReferences))]
    static class InitializeForceAnimalField
    {
        static void Postfix(ThingDef __instance)
        {
            if (__instance.HasModExtension<ForceAnimal>() && __instance.race != null)
            {
                AnomalyAlliesMod.FieldProvider.SetForcedAnimal(__instance.race, true);
                Log.Message($"Set ForceAnimal to true for {__instance.defName}");
            }
        }
    }
}
