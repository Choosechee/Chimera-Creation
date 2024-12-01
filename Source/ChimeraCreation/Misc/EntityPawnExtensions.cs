using AnomalyAllies.DefModExtensions;
using RimWorld;
using Verse;

namespace AnomalyAllies.Misc
{
    public static class EntityPawnExtensions
    {
        // add more conditions later
        public static bool IsConnectedToHorax(this Pawn pawn)
        {
            return pawn.RaceProps.IsAnomalyEntity &&
                (Find.Anomaly.LevelDef != MonolithLevelDefOf.Disrupted ||
                pawn.health.hediffSet.hediffs.Any(h => h.def.HasModExtension<ConnectionToHorax>()));
        }
    }
}
