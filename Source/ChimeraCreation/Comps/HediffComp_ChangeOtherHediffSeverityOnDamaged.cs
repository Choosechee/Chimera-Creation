using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace AnomalyAllies.Comps
{
    public class HediffComp_ChangeOtherHediffSeverityOnDamaged : HediffComp
    {
        public HediffCompProperties_ChangeOtherHediffSeverityOnDamaged Props => (HediffCompProperties_ChangeOtherHediffSeverityOnDamaged)props;
        
        protected HashSet<Hediff> hediffsChangedAlready;

        public override void CompPostMake()
        {
            base.CompPostMake();

            if (Props.onlyOnce)
                hediffsChangedAlready = new HashSet<Hediff>();
        }

        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);

            if (totalDamageDealt <= 0)
                return;

            List<Hediff> hediffsToChange = new List<Hediff>();
            parent.pawn.health.hediffSet.GetHediffs(ref hediffsToChange, (Hediff h) => h.def == Props.otherHediff);

            foreach (Hediff hediffToChange in hediffsToChange)
            {
                if (!Props.onlyOnce || !hediffsChangedAlready.Contains(hediffToChange))
                {
                    hediffToChange.Severity = (Props.set) ? Props.severityChange : hediffToChange.Severity + Props.severityChange;
                }
                hediffsChangedAlready?.Add(hediffToChange);
            }
            if (hediffsChangedAlready != null)
            {
                List<Hediff> currentHediffs = parent.pawn.health.hediffSet.hediffs;
                List<Hediff> oldHediffsToRemove = new List<Hediff>();

                foreach (Hediff hediffChanged in hediffsChangedAlready)
                {
                    if (!currentHediffs.Contains(hediffChanged))
                        oldHediffsToRemove.Add(hediffChanged);
                }
                foreach (Hediff oldHediff in oldHediffsToRemove)
                    hediffsChangedAlready.Remove(oldHediff);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                hediffsChangedAlready.RemoveWhere((Hediff h) => !parent.pawn.health.hediffSet.hediffs.Contains(h));
            }
            Scribe_Collections.Look(ref hediffsChangedAlready, "hediffsChangedAlready", LookMode.Reference);
        }
    }

    public class HediffCompProperties_ChangeOtherHediffSeverityOnDamaged : HediffCompProperties
    {
        public HediffDef otherHediff;
        public float severityChange;
        public bool set = false;
        public bool onlyOnce = false;

        public HediffCompProperties_ChangeOtherHediffSeverityOnDamaged()
        {
            compClass = typeof(HediffComp_ChangeOtherHediffSeverityOnDamaged);
        }

        public HediffCompProperties_ChangeOtherHediffSeverityOnDamaged(Type compClass)
        {
            this.compClass = compClass;
        }
    }
}
