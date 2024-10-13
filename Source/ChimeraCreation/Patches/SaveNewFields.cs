using HarmonyLib;
using Verse;

namespace AnomalyAllies.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.ExposeData))]
    static class SaveNewFields
    {
        static void Postfix(Pawn __instance)
        {
            AnomalyAlliesMod.FieldProvider.ExposeData(__instance);
        }
    }
}
