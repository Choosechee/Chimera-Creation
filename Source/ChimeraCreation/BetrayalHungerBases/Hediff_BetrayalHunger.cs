using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace AnomalyAllies.BetrayalHungerBases
{
    public abstract class Hediff_BetrayalHunger : Hediff_MeatHunger
    {
        protected static Dictionary<int, int> StageIndexToBetrayalMTBHours = typeof(Hediff_MeatHunger).ForceGetStaticField<Dictionary<int, int>>("StageIndexToBetrayalMTBHours");
        protected abstract string BetrayalLabel { get; }
        protected abstract string BetrayalText { get; }

        protected abstract void Betrayal();

        public override void Tick()
        {
            if (!(pawn.ParentHolder is Map || pawn.ParentHolder is Caravan || pawn.ParentHolder is Pawn_CarryTracker))
                return;

            if (!pawn.health.capacities.CanBeAwake)
                return;
            
            int num = StageIndexToBetrayalMTBHours[CurStageIndex];
            if (num > 0 && Rand.MTBEventOccurs(num, 2500f, 1f) && pawn.Faction == Faction.OfPlayer)
            {
                Betrayal();
                // Find.LetterStack.ReceiveLetter(BetrayalLabel, BetrayalText, LetterDefOf.ThreatBig, pawn);
            }
        }
    }
}