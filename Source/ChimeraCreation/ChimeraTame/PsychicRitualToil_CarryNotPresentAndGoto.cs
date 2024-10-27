using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AnomalyAllies.ChimeraTame
{
    // like PsychicRitualToil_CarryAndGoto, but it carries a pawn not in position instead of just the first pawn of a role, and you can choose the DutyDef
    public class PsychicRitualToil_CarryNotPresentAndGoto : PsychicRitualToil_Goto
    {
        public PsychicRitualRoleDef carrierRole;
        public PsychicRitualRoleDef payLoadRole;

        protected List<IntVec3> payLoadPositions;
        protected DutyDef carryDutyDef;
        protected JobGiver_DeliverPawnToCell deliverPawnJobGiver;

        protected Pawn payLoadCarried = null;
        protected PawnDuty payLoadOldDuty = null;

        protected PsychicRitualToil_CarryNotPresentAndGoto()
        {
        }

        public PsychicRitualToil_CarryNotPresentAndGoto(PsychicRitualRoleDef carrierRole, PsychicRitualRoleDef payLoadRole, DutyDef carryDutyDef, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions) : base(rolePositions.Slice(carrierRole))
        {
            this.carrierRole = carrierRole;
            this.payLoadRole = payLoadRole;
            payLoadPositions = new List<IntVec3>(rolePositions[payLoadRole]);

            ThinkNode deliverPawnJobGiver = carryDutyDef.thinkNode.subNodes.Find((tn) => tn is JobGiver_DeliverPawnToCell);
            if (deliverPawnJobGiver is not null)
            {
                this.carryDutyDef = carryDutyDef;
                this.deliverPawnJobGiver = deliverPawnJobGiver as JobGiver_DeliverPawnToCell;
            }
            else
                throw new ArgumentException($"carryDutyDef {carryDutyDef} does not contain a ThinkNode to carry a pawn.");
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref carrierRole, "carrierRole");
            Scribe_Defs.Look(ref payLoadRole, "payLoadRole");
            Scribe_Collections.Look(ref payLoadPositions, "payLoadPositions", LookMode.Value);
            Scribe_Defs.Look(ref carryDutyDef, "carryDutyDef");
            Scribe_Values.Look(ref deliverPawnJobGiver, "deliverPawnJobGiver");
        }

        public override void UpdateAllDuties(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            base.UpdateAllDuties(psychicRitual, parent);

            List<Pawn> potentialPayLoads = new List<Pawn>(psychicRitual.assignments.AssignedPawns(payLoadRole));

            // find a target not already in position
            int payLoadIndex = potentialPayLoads.FindIndex((pl) => pl.Position != payLoadPositions[potentialPayLoads.IndexOf(pl)]);
            if (payLoadIndex < 0)
                return;
            Pawn payLoad = potentialPayLoads[payLoadIndex];

            /*payLoadCarried = payLoad;
            payLoadOldDuty = payLoad.mindState.duty;*/

            Pawn carrier = psychicRitual.assignments.FirstAssignedPawn(carrierRole);
            if (carrier is null)
                return;

            if ((deliverPawnJobGiver is JobGiver_DeliverAnimalToPsychicRitualCell && payLoad.ShouldAvoidFences)
                || (deliverPawnJobGiver is not JobGiver_DeliverAnimalToPsychicRitualCell && payLoad.IsPrisoner))
            {
                AnomalyAlliesMod.Logger.Message(payLoad.mindState.duty);
                SetPawnDuty(payLoad, psychicRitual, parent, DutyDefOf.Idle);
                payLoad.mindState.duty.focus = psychicRitual.assignments.Target.Cell;
                AnomalyAlliesMod.Logger.Message(payLoad.mindState.duty);
            }
            SetPawnDuty(carrier, psychicRitual, parent, carryDutyDef, carrier.mindState.duty.focus, payLoad, payLoadPositions[payLoadIndex], FinalGatherPhase ? "final" : "initial");
        }

        /*public override void Notify_PawnJobDone(PsychicRitual psychicRitual, PsychicRitualGraph parent, Pawn pawn, Job job, JobCondition condition)
        {
            base.Notify_PawnJobDone(psychicRitual, parent, pawn, job, condition);

            if (payLoadCarried is not null)
                payLoadCarried.mindState.duty = payLoadOldDuty;
        }*/
    }
}
