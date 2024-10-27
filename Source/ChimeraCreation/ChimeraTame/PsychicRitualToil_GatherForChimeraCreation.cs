using AnomalyAllies.DefOfs;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace AnomalyAllies.ChimeraTame
{
    // like PsychicRitualToil_GatherForInvocation, but it works if more than one target needs to be carried
    public class PsychicRitualToil_GatherForChimeraCreation : PsychicRitualToil_GatherForInvocation
    {
        public PsychicRitualToil_GatherForChimeraCreation(PsychicRitual psychicRitual, PsychicRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions) : base(def, FallbackToil(psychicRitual, def, rolePositions), InvokerToil(def, rolePositions))
        {
        }

        public static new PsychicRitualToil_Goto FallbackToil(PsychicRitual psychicRitual, PsychicRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
        {
            return new PsychicRitualToil_AnimalCompatibleGoto(rolePositions.Slice(rolePositions.Keys.Except(def.InvokerRole)));
        }

        public static new PsychicRitualGraph InvokerToil(PsychicRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
        {
            return new PsychicRitualGraph(InvokerGatherPhaseToils(def, rolePositions))
            {
                willAdvancePastLastToil = false
            };
        }
        
        public static new IEnumerable<PsychicRitualToil> InvokerGatherPhaseToils(PsychicRitualDef_InvocationCircle def, IReadOnlyDictionary<PsychicRitualRoleDef, List<IntVec3>> rolePositions)
        {
            if (def.RequiredOffering != null)
            {
                yield return new PsychicRitualToil_GatherOfferings(def.InvokerRole, def.RequiredOffering);
            }

            if (def.TargetRole != null)
            {
                for (int i = 0; i < rolePositions[def.TargetRole].Count; i++)
                    yield return new PsychicRitualToil_CarryNotPresentAndGoto(def.InvokerRole, def.TargetRole, AnAl_DutyDefOf.AnAl_DeliverAnimalToPsychicRitualCell, rolePositions);
                
                yield break;
            }

            yield return new PsychicRitualToil_Goto(rolePositions.Slice(def.InvokerRole));
        }
    }
}
