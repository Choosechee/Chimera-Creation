using AnomalyAllies.DefOfs;
using AnomalyAllies.Misc;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace AnomalyAllies.Comps
{
    public class CompRevengeOnSlaughter : ThingComp
    {
        public CompProperties_RevengeOnSlaughter Props => (CompProperties_RevengeOnSlaughter)props;
        public Pawn Pawn => (Pawn)parent;

        public virtual bool RandRevenge(Pawn executioner)
        {
            if (executioner.relations.DirectRelationExists(PawnRelationDefOf.Bond, Pawn))
                return Rand.Chance(Props.ChanceToRevengeOnSlaughterBonded);
            else
                return Rand.Chance(Props.ChanceToRevengeOnSlaughter);
        }

        public virtual void Revenge(Pawn executioner)
        {
            Faction victimFaction = Pawn.Faction;
            Pawn.SetFaction(Find.FactionManager.FirstFactionOfDef(Pawn.kindDef.defaultFactionType));

            if (!Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter))
            {
                /*
                I can't start the mental state before setting the faction
                because setting the faction clears the mental state.
                But I don't want the faction to be changed if starting the
                mental state fails.
                */
                Pawn.SetFaction(victimFaction);

                throw new Exception("TryStartMentalState returned false.");
            }
            
            TaggedString label = "AnAl_RevengeFromAttemptedSlaughter_Label".Translate(Pawn.LabelCap.Named("VICTIM"));
            TaggedString text = "AnAl_RevengeFromAttemptedSlaughter_Text".Translate(Pawn.Named("VICTIM"), executioner.Named("SLAUGHTERER"));

            DirectPawnRelation bond = Pawn.relations.GetDirectRelation(PawnRelationDefOf.Bond, executioner);
            if (bond != null)
            {
                Pawn.relations.RemoveDirectRelation(bond);
                if (Pawn.IsEntity)
                    BondBreakHelper.TryGiveEntityBetrayalThought(executioner, Pawn);
                else
                    executioner.needs.mood.thoughts.memories.TryGainMemory(AnAl_ThoughtDefOf.AnAl_BondedAnimalEntityBetrayed, Pawn);
                text += "AnAl_RevengeFromAttemptedSlaughter_Bond".Translate(Pawn.Named("VICTIM"), executioner.Named("SLAUGHTERER"));

                TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Slaughter_Bonded_Temporary, Pawn, executioner);
            }
            else
                TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Slaughter_Temporary, Pawn, executioner);

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, Pawn);
            // Pawn.mindState.ForceInvokeMethod("StartManhunterBecauseOfPawnAction", new object[] {executioner, "AnAl_RevengealFromAttemptedSlaughter", false});
        }

        public virtual bool TryRevenge(Pawn executioner)
        {
            bool tookRevenge = RandRevenge(executioner) && AnomalyAlliesMod.Settings.entitiesCanBetray
                && Pawn.health.capacities.CanBeAwake;
            if (tookRevenge)
            {
                try
                {
                    Revenge(executioner);
                }
                catch (Exception ex)
                {
                    Log.Error($"Revenge for {Pawn.LabelCap} was attempted, but failed due to exception: {ex}");
                    return false;
                }
            }
            return tookRevenge;
        }
    }
    
    public class CompProperties_RevengeOnSlaughter : CompProperties
    {
        protected float chanceToRevengeOnSlaughter = 1f;
        protected float? chanceToRevengeOnSlaughterBonded = null;

        public float ChanceToRevengeOnSlaughter => chanceToRevengeOnSlaughter;
        public float ChanceToRevengeOnSlaughterBonded => chanceToRevengeOnSlaughterBonded ?? chanceToRevengeOnSlaughter;

        public CompProperties_RevengeOnSlaughter()
        {
            compClass = typeof(CompRevengeOnSlaughter);
        }

        public CompProperties_RevengeOnSlaughter(Type compClass)
        {
            this.compClass = compClass;
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            var baseErrors = base.ConfigErrors(parentDef);
            if (baseErrors != null)
            {
                foreach (string error in baseErrors)
                    yield return error;
            }
            if (parentDef.race is null)
                yield return $"{parentDef.defName} is not a race def for a pawn.";
        }
    }
}
