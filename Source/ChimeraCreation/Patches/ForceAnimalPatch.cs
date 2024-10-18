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
            return __result || AnomalyAlliesMod.FieldProvider.EntityAnimal(__instance);
        }
    }

    [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.ResolveReferences))]
    static class InitializeEntityAnimalField
    {
        static void Postfix(ThingDef __instance)
        {
            if (__instance.HasModExtension<EntityAnimal>() && __instance.race is not null)
            {
                AnomalyAlliesMod.FieldProvider.EntityAnimal(__instance.race) = true;
                Log.Message($"Set EntityAnimal to true for {__instance.defName}");
            }
        }
    }
}
