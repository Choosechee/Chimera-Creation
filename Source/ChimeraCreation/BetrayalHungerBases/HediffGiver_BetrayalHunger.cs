using Verse;

namespace AnomalyAllies.BetrayalHungerBases
{
    public class HediffGiver_BetrayalHunger : HediffGiver_MeatHunger
    {
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            if (AnomalyAlliesMod.Settings.entitiesCanBetray)
                base.OnIntervalPassed(pawn, cause);
            else if (pawn.health.hediffSet.TryGetHediff(hediff, out Hediff hediffInstance))
                pawn.health.RemoveHediff(hediffInstance);
        }
    }
}
