using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AnomalyAllies.ChimeraTame
{
    public class PsychicRitualToil_AnimalCompatibleGoto : PsychicRitualToil_Goto
    {
        protected delegate void DumbPrivateMethod(PsychicRitualToil_Goto instance, Pawn pawn);
        protected DumbPrivateMethod ForceFinished = (DumbPrivateMethod)typeof(PsychicRitualToil_Goto).Method("ForceFinished").CreateDelegate(typeof(DumbPrivateMethod));

        public PsychicRitualToil_AnimalCompatibleGoto(IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions) : base(rolePositions)
        {
        }

        public override void UpdateAllDuties(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            bool finalGatherPhase = FinalGatherPhase;

            foreach (KeyValuePair<PsychicRitualRoleDef, List<IntVec3>> rolePosition in rolePositions)
            {
                GenCollection.Deconstruct(rolePosition, out var key, out var value);
                PsychicRitualRoleDef role = key;
                List<IntVec3> positions = value;
                int index = 0;

                foreach (Pawn pawn in psychicRitual.assignments.AssignedPawns(role))
                {
                    IntVec3 position = positions[index++];
                    DutyDef dutyDef = ((!finalGatherPhase && !initialGoto.WaitingOnPawn(pawn)) ? DutyDefOf.WanderClose : (pawn.IsPrisonerOfColony || pawn.ShouldAvoidFences ? DutyDefOf.Idle : DutyDefOf.Goto));
                    string tag = (finalGatherPhase ? "final" : "initial");

                    SetPawnDuty(pawn, psychicRitual, parent, dutyDef, position, null, null, tag);
                }
            }
        }

        public override bool Tick(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            base.Tick(psychicRitual, parent);

            foreach (Pawn pawn in ControlledPawns(psychicRitual))
            {
                if (pawn.ShouldAvoidFences)
                {
                    IntVec3 pawnPosition = pawn.Position;
                    IntVec3? wantedPosition = pawn.mindState?.duty?.focus.Cell;
                    if (pawnPosition == wantedPosition)
                        ForceFinished(this, pawn);
                }
            }

            return finalGoto.AllPawnsDone;
        }
    }
}
