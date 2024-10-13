using AnomalyAllies.DefOfs;
using AnomalyAllies.Misc;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace AnomalyAllies.Comps
{
    public class CompEntityRevengeOnSlaughter : CompRevengeOnSlaughter
    {
        public override void Revenge(Pawn executioner)
        {
            if (!AnomalyAlliesMod.Settings.betrayalIsPermanent)
            {
                base.Revenge(executioner);
                return;
            }
            
            Pawn victim = Pawn;
            Faction victimFaction = victim.Faction;
            List<Pawn> bondedPawns = new List<Pawn>();
            victim.relations.GetDirectRelations(PawnRelationDefOf.Bond, ref bondedPawns);

            CompTransform compTransform = null;
            if (victim.HasComp<CompTransform>())
            {
                compTransform = victim.TryGetComp<CompTransform>();
                victim = compTransform.TransformPawn();
            }
            victim.SetFaction(Faction.OfEntities);

            LordJob_ChimeraAssault lordJob = new LordJob_ChimeraAssault();
            Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, lordJob, victim.Map, new List<Pawn>() { victim });
            lord.ReceiveMemo(LordJob_ChimeraAssault.StalkToAttackMemo);
            victim.jobs.EndCurrentJob(Verse.AI.JobCondition.None); // so they attack immediately instead of running away for a bit
            Find.LetterStack.RemoveLastLetter(removeFromArchive: true);

            TaggedString label = "AnAl_RevengeFromAttemptedSlaughter_Label".Translate(Pawn.LabelCap.Named("VICTIM"));
            TaggedString text = "AnAl_EntityRevengeFromAttemptedSlaughter_Text".Translate(Pawn.Named("VICTIM"), executioner.Named("SLAUGHTERER"));

            if (bondedPawns.Count > 0)
            {
                foreach (Pawn bondedPawn in bondedPawns)
                {
                    Pawn.relations.RemoveDirectRelation(PawnRelationDefOf.Bond, bondedPawn);
                    BondBreakHelper.TryGiveEntityBetrayalThought(bondedPawn, victim);
                }
                if (bondedPawns.Count == 1)
                    text += "AnAl_EntityRevengeFromAttemptedSlaughter_Bond".Translate(Pawn.Named("VICTIM"), bondedPawns[0].Named("BONDEDPAWN"));
                else
                {
                    string bondedPawnsString = BondBreakHelper.CreateBondedPawnsString(bondedPawns);
                    text += "AnAl_EntityRevengeFromAttemptedSlaughter_Bond_Multiple".Translate(Pawn.Named("VICTIM"), bondedPawnsString.Named("BONDEDPAWNS"));
                }
            }

            if (bondedPawns.Contains(executioner))
                TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Slaughter_Bonded_Permanent, Pawn, executioner);
            else
                TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Slaughter_Permanent, Pawn, executioner);

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, victim);
        }
    }
}
