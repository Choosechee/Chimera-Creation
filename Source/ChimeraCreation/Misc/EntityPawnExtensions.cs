using AnomalyAllies.GeneralHediffs;
using RimWorld;
using Verse;

namespace AnomalyAllies.Misc
{
    public static class EntityPawnExtensions
    {
        // add one more condition later
        public static bool IsConnectedToHorax(this Pawn pawn)
        {
            return pawn.RaceProps.IsAnomalyEntity &&
                (Find.Anomaly.LevelDef != MonolithLevelDefOf.Disrupted ||
                pawn.health.hediffSet.HasHediff<Hediff_ReconnectToHorax>());
        }

        public static bool IsConnectedToHoraxWithLog(this Pawn pawn)
        {
            AnomalyAlliesMod.Logger.Message($"Is an entity: {pawn.RaceProps.IsAnomalyEntity}");
            AnomalyAlliesMod.Logger.Message($"Monolith isn't disrupted: {Find.Anomaly.LevelDef != MonolithLevelDefOf.Disrupted}");
            AnomalyAlliesMod.Logger.Message($"Has monolith fragment: {pawn.health.hediffSet.HasHediff<Hediff_ReconnectToHorax>()}");

            return IsConnectedToHorax(pawn);
        }
    }
}
