using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;


namespace AnomalyAllies.ChimeraTame
{
    public class JobGiver_DeliverAnimalToPsychicRitualCell : JobGiver_DeliverPawnToPsychicRitualCell
    {
        // same as base, except it checks if the pawn can go through fences instead of checking they're a prisoner
        protected override Job TryGiveJob(Pawn pawn)
        {
            Pawn pawnToCarry = pawn.mindState.duty.focusSecond.Pawn;

            if (pawnToCarry == null || pawnToCarry.Dead)
            {
                return null;
            }
            if (pawnToCarry.GetLord() != pawn.GetLord())
            {
                return null;
            }
            if (skipIfTargetCanReach && !pawnToCarry.Downed && !pawnToCarry.ShouldAvoidFences)
            {
                return null;
            }
            if (pawnToCarry.mindState.duty == null)
            {
                return null;
            }

            LocalTargetInfo destination = GetDestination(pawnToCarry);
            if (!destination.IsValid || pawnToCarry.Position == destination.Cell)
            {
                return null;
            }
            if (!pawn.CanReach(pawnToCarry, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(pawn, maxDanger)))
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.DeliverToCell, pawnToCarry, destination).WithCount(1);
            job.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, locomotionUrgency);
            job.expiryInterval = jobMaxDuration;

            return job;
        }
    }
}
