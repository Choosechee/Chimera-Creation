using AnomalyAllies.Comps;
using AnomalyAllies.DefOfs;
using AnomalyAllies.Misc;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace AnomalyAllies.ChimeraTame
{
    public class Hediff_MeatHunger_Chimera : BetrayalHungerBases.Hediff_BetrayalHunger
    {
        protected override string BetrayalLabel
        {
            get { return "AnAl_ChimeraBetrayal_Label".Translate(); }
        }

        private string betrayalText = string.Empty;
        protected override string BetrayalText
        {
            get { return betrayalText; }
        }

        private List<Pawn> AffectedPawnsInLocation // pawns with this hediff in the same location, including self
        {
            get
            {
                List<Pawn> pawnsinSameLocation;
                bool locationIsCaravan = false;
                if (pawn.MapHeld is not null)
                    pawnsinSameLocation = pawn.MapHeld.mapPawns.PawnsInFaction(Faction.OfPlayer);
                else
                {
                    pawnsinSameLocation = pawn.GetCaravan().pawns.InnerListForReading;
                    locationIsCaravan = true;
                }

                List<Pawn> pawnsWithThisHediff = new List<Pawn>();
                foreach (Pawn pawn in pawnsinSameLocation)
                {
                    if (pawn.health.hediffSet.HasHediff<Hediff_MeatHunger_Chimera>(mustBeVisible: true)
                        && pawn.health.capacities.CanBeAwake
                        && (pawn.ParentHolder is Map || locationIsCaravan || pawn.ParentHolder is Pawn_CarryTracker))
                        pawnsWithThisHediff.Add(pawn);
                }
                return pawnsWithThisHediff;
            }
        }

        private string GetPawnsAfterTransformationString(List<Pawn> pawnsAfterTransformation)
        {
            StringBuilder pawnsAfterTransformationBuilder = new StringBuilder();
            foreach (Pawn pawn in pawnsAfterTransformation)
            {
                pawnsAfterTransformationBuilder.AppendLine("  - " + pawn.NameShortColored.Resolve());
            }

            return pawnsAfterTransformationBuilder.ToString().TrimEndNewlines();
        }

        private string GetBondsBrokenString(Dictionary<Pawn, List<Pawn>> bondsBroken)
        {
            StringBuilder bondsBrokenBuilder = new StringBuilder();
            foreach (KeyValuePair<Pawn, List<Pawn>> bonds in bondsBroken)
            {
                Pawn bondedAnimal = bonds.Key;
                List<Pawn> bondedPawns = bonds.Value;

                foreach (Pawn bondedPawn in bondedPawns)
                    bondsBrokenBuilder.AppendLine("  - " + "AnAl_ChimeraBetrayal_Extended_BondBroken_Multiple_Line".Translate(bondedAnimal.Named("BONDEDANIMAL"), bondedPawn.Named("BONDEDPAWN")).Resolve());
            }

            return bondsBrokenBuilder.ToString().TrimEndNewlines();
        }

        protected override void Betrayal() // all pawns with this hediff on the map/caravan betray at once
        {
            List<Pawn> affectedPawns = AffectedPawnsInLocation;
            int thisPawnIndex = affectedPawns.IndexOf(pawn);
            if (thisPawnIndex < 0)
            {
                AnomalyAlliesMod.Logger.Error($"{this.GetType().Name}: Pawn starting betrayal is not in the affected pawns list! Something has gone wrong.");
                return;
            }

            List<Pawn> pawnsAfterTransformation = new List<Pawn>();
            Dictionary<Pawn, List<Pawn>> bondsBroken = new Dictionary<Pawn, List<Pawn>>();
            foreach (Pawn affectedPawn in affectedPawns)
            {
                if (affectedPawn.ParentHolder is Pawn_CarryTracker carryingPawn2)
                    carryingPawn2.TryDropCarriedThing(carryingPawn2.pawn.Position, ThingPlaceMode.Direct, out _);
                
                List<Pawn> affectedPawnBondedPawns = new List<Pawn>();
                affectedPawn.relations.GetDirectRelations(PawnRelationDefOf.Bond, ref affectedPawnBondedPawns);
                foreach (Pawn bondedPawn in affectedPawnBondedPawns)
                {
                    affectedPawn.relations.RemoveDirectRelation(PawnRelationDefOf.Bond, bondedPawn);
                    BondBreakHelper.TryGiveEntityBetrayalThought(bondedPawn, affectedPawn);
                }

                List<Pawn> aliveBondedPawns = affectedPawnBondedPawns.Where(p => !p.health.Dead).ToList();
                if (aliveBondedPawns.Count > 0)
                    bondsBroken[affectedPawn] = aliveBondedPawns;
                    
                pawnsAfterTransformation.Add(BetrayalHelper(affectedPawn));
            }
            Pawn originatorPawn = pawnsAfterTransformation[thisPawnIndex];

            string betrayalTextPart =
                (!AnomalyAlliesMod.Settings.chimeraIsNormalCarnivore)
                ? "AnAl_ChimeraBetrayal_Text" : "AnAl_ChimeraBetrayal_Text_ChimeraIsNormalCarnivore";
            betrayalTextPart = betrayalTextPart.Translate(originatorPawn.Named("PAWN")).Resolve();
            StringBuilder betrayalTextBuilder = new StringBuilder(betrayalTextPart);
            bool multipleBetrayed = false;

            if (pawnsAfterTransformation.Count > 1)
            {
                List<Pawn> pawnsAfterTransformationWithoutOriginator = pawnsAfterTransformation.GetRange(0, pawnsAfterTransformation.Count);
                pawnsAfterTransformationWithoutOriginator.Remove(originatorPawn);
                string pawnsAfterTransformationString = GetPawnsAfterTransformationString(pawnsAfterTransformationWithoutOriginator);
                betrayalTextBuilder.Append("AnAl_ChimeraBetrayal_Extended_Multiple".Translate(NamedArgumentUtility.Named(pawnsAfterTransformationString, "AFFECTEDPAWNS")).Resolve());
                multipleBetrayed = true;
            }

            int bondsBrokenCount = 0;
            foreach (List<Pawn> bonds in bondsBroken.Values)
            {
                bondsBrokenCount += bonds.Count;
            }
            if (multipleBetrayed && bondsBrokenCount > 0)
                betrayalTextBuilder.AppendLine();
            if (bondsBrokenCount > 1)
            {
                string bondsBrokenString = GetBondsBrokenString(bondsBroken);
                betrayalTextBuilder.Append("AnAl_ChimeraBetrayal_Extended_BondBroken_Multiple".Translate(NamedArgumentUtility.Named(bondsBrokenString, "BONDSBROKEN")).Resolve());
            }
            else if (bondsBrokenCount == 1)
            {
                var bondBroken = bondsBroken.First();
                Pawn bondedAnimal = bondBroken.Key;
                Pawn bondedPawn = bondBroken.Value[0];
                betrayalTextBuilder.Append("AnAl_ChimeraBetrayal_Extended_BondBroken_Single".Translate(bondedAnimal.Named("BONDEDANIMAL"), bondedPawn.Named("BONDEDPAWN")).Resolve());
            }
            betrayalText = betrayalTextBuilder.ToString();

            if (originatorPawn.IsCaravanMember())
            {
                Caravan caravan = originatorPawn.GetCaravan();
                foreach (Pawn betrayingPawn in pawnsAfterTransformation)
                {
                    caravan.RemovePawn(betrayingPawn);
                }
                Map map = CaravanIncidentUtility.SetupCaravanAttackMap(caravan, pawnsAfterTransformation, false);
            }

            if (bondsBroken.ContainsKey(pawn))
            {
                if (multipleBetrayed)
                    TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Hunger_Bonded_Multiple, pawn, bondsBroken[pawn].RandomElement());
                else
                    TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Hunger_Bonded, pawn, bondsBroken[pawn].RandomElement());
            }
            else
            {
                if (multipleBetrayed)
                    TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Hunger_Multiple, pawn);
                else
                    TaleRecorder.RecordTale(ChimeraTaleDefOf.AnAl_ChimeraBetrayal_Hunger, pawn);
            }

            Find.LetterStack.ReceiveLetter(BetrayalLabel, BetrayalText, LetterDefOf.ThreatBig, pawnsAfterTransformation);

            LordJob_ChimeraAssault lordJob = new LordJob_ChimeraAssault();
            Lord lord = LordMaker.MakeNewLord(Faction.OfEntities, lordJob, originatorPawn.Map, pawnsAfterTransformation);
            lord.ReceiveMemo(LordJob_ChimeraAssault.StalkToAttackMemo);
            foreach (Pawn pawn in pawnsAfterTransformation) // so they attack immediately instead of running away for a bit
            {
                pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.None);
            }
        }

        public static Pawn BetrayalHelper(Pawn pawn)
        {
            if (AnomalyAlliesMod.Settings.betrayalIsPermanent && pawn.TryGetComp(out CompTransform compTransform))
                pawn = compTransform.TransformPawn();
            else
            {
                Need_Food hunger = pawn.needs.food;
                if (hunger is not null)
                    hunger.CurLevelPercentage = 1f;

                Hediff malnutrition = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition);
                if (malnutrition is not null)
                    pawn.health.RemoveHediff(malnutrition);

                Hediff meatHunger = pawn.health.hediffSet.GetFirstHediffOfDef(AnAl_HediffDefOf.AnAl_MeatHungerChimera);
                pawn.health.RemoveHediff(meatHunger);
            }

            pawn.SetFaction(Faction.OfEntities);
            return pawn;
        }
    }
}
