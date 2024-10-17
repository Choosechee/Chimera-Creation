using HarmonyLib;
using Verse;

namespace AnomalyAllies.Patches
{
    [HarmonyPatch(typeof(PawnGraphicUtils), nameof(PawnGraphicUtils.TryGetAlternate))]
    static class ForceSpecificGraphic
    {
        static bool Postfix(bool __result, Pawn pawn, ref AlternateGraphic ag, ref int index)
        {
            int? potentialForcedGraphic = AnomalyAlliesMod.FieldProvider.ForcedGraphic(pawn);
            if (potentialForcedGraphic.HasValue
                && (pawn.kindDef.alternateGraphics is not null && pawn.kindDef.alternateGraphics.Count > 0))
            {
                index = potentialForcedGraphic.Value;
                if (index > -1)
                {
                    ag = pawn.kindDef.alternateGraphics[index];
                    return true;
                }
                else
                {
                    ag = null;
                    return false;
                }
            }

            return __result;
        }
    }
}
