using AnomalyAllies.Comps;
using RimWorld;
using Verse;

namespace AnomalyAllies.GeneralHediffs
{
    public class Hediff_ReconnectToHorax : Hediff_Implant
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            AnomalyAlliesMod.Logger.Message($"PostAdd is being run for {this}");
            AnomalyAlliesMod.Logger.Message(System.Environment.StackTrace);

            AttemptTransformPawn();
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            AttemptTransformPawn();
        }

        private void AttemptTransformPawn()
        {
            if (pawn.TryGetComp(out CompTransform compTransform) && compTransform.AffectedByHoraxConnection)
            {
                AnomalyAlliesMod.Logger.Message($"Attempting to transform {pawn}");
                compTransform.TransformPawnHoraxConnectionChange();
            }
        }
    }
}
