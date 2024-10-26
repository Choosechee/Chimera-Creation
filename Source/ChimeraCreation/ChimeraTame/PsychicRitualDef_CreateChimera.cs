using AnomalyAllies.DefOfs;
using AnomalyAllies.Misc;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace AnomalyAllies.ChimeraTame
{
    public class PsychicRitualDef_CreateChimera : PsychicRitualDef_InvocationCircle
    {
        public string outcomeDescriptionPlural;

        public string outcomeDescriptionMultiplePart1;
        public string outcomeDescriptionMultiplePart1Plural;

        public string outcomeDescriptionMultiplePart2;
        public string outcomeDescriptionMultiplePart2Plural;

        public SimpleCurve fleshbeastChanceFromQualityCurve;

        protected float? meatYieldNeededForChimera;
        public float MeatYieldNeededForChimeraWithOffset => meatYieldNeededForChimera.GetValueOrDefault(0f) + AnomalyAlliesMod.Settings.chimeraMeatRequirementOffset;

        public List<List<string>> chimeraTypeAnimals;

        public static float TotalMeatYieldOfTargets(IEnumerable<Pawn> targets)
        {
            float totalMeatYield = 0;
            foreach (Pawn target in targets)
                totalMeatYield += target.GetStatValue(StatDefOf.MeatAmount, cacheStaleAfterTicks: 1);
            
            return totalMeatYield;
        }
        protected float TotalMeatYieldOfTargets(PsychicRitualRoleAssignments assignments)
        {
            return TotalMeatYieldOfTargets(assignments.AssignedPawns(TargetRole));
        }

        public override List<PsychicRitualToil> CreateToils(PsychicRitual psychicRitual, PsychicRitualGraph graph)
        {
            List<PsychicRitualToil> list = base.CreateToils(psychicRitual, graph);
            list.Add(new PsychicRitualToil_CreateChimera(InvokerRole, TargetRole));
            return list;
        }

        public override PsychicRitualCandidatePool FindCandidatePool()
        {
            MapPawns mapPawns = Find.CurrentMap.mapPawns;
            
            List<Pawn> candidates = new List<Pawn>(mapPawns.FreeColonistsSpawned);
            candidates.AddRange(mapPawns.SpawnedColonyAnimals);

            return new PsychicRitualCandidatePool(candidates, new List<Pawn>());
        }

        public override IEnumerable<string> GetPawnTooltipExtras(Pawn pawn)
        {
            if (pawn.IsNonMutantAnimal)
                yield return $"{StatDefOf.MeatAmount.LabelCap}: {(int)pawn.GetStatValue(StatDefOf.MeatAmount, cacheStaleAfterTicks: 1)}";
        }

        public override TaggedString OutcomeDescription(FloatRange qualityRange, string qualityNumber, PsychicRitualRoleAssignments assignments)
        {
            if (!AnomalyAlliesMod.Settings.multipleChimeraCreation)
            {
                string outcomeDescriptionSelected = (assignments.RoleAssignedCount(TargetRole) != 1) ? outcomeDescriptionPlural : outcomeDescription;

                float meatRefunded = TotalMeatYieldOfTargets(assignments);
                if (meatRefunded >= MeatYieldNeededForChimeraWithOffset)
                    meatRefunded -= MeatYieldNeededForChimeraWithOffset;

                return outcomeDescriptionSelected.Formatted(fleshbeastChanceFromQualityCurve.Evaluate(qualityRange.min).ToStringPercent(), meatRefunded);
            }
            else
            {
                float totalMeatYield = TotalMeatYieldOfTargets(assignments);
                int numberOfChimeras = (int)(totalMeatYield / MeatYieldNeededForChimeraWithOffset);
                float meatRefunded = totalMeatYield - (numberOfChimeras * MeatYieldNeededForChimeraWithOffset);

                string outcomeDescriptionSelectedPart1 = (assignments.RoleAssignedCount(TargetRole) != 1) ? outcomeDescriptionMultiplePart1Plural : outcomeDescriptionMultiplePart1;
                string outcomeDescriptionSelectedPart2 = (numberOfChimeras > 1) ? outcomeDescriptionMultiplePart2Plural : outcomeDescriptionMultiplePart2;
                string outcomeDescriptionSelected = outcomeDescriptionSelectedPart1 + outcomeDescriptionSelectedPart2;

                return outcomeDescriptionSelected.Formatted(fleshbeastChanceFromQualityCurve.Evaluate(qualityRange.min).ToStringPercent(), numberOfChimeras, meatRefunded);
            }
        }

        public override IEnumerable<TaggedString> OutcomeWarnings(PsychicRitualRoleAssignments assignments)
        {
            var baseWarnings = base.OutcomeWarnings(assignments);
            foreach (TaggedString warning in baseWarnings)
            {
                yield return warning;
            }

            List<Pawn> targets = new List<Pawn>(assignments.AssignedPawns(targetRole));

            int numberOfTargets = targets.Count;
            if (numberOfTargets > 0)
            {
                float totalMeatYield = TotalMeatYieldOfTargets(assignments);
                if (totalMeatYield < MeatYieldNeededForChimeraWithOffset)
                {
                    if (numberOfTargets == 1)
                        yield return "AnAl_PsychicRitualCreateChimera_NotEnoughMeatWarning_Single".Translate((MeatYieldNeededForChimeraWithOffset - totalMeatYield).Named("MEATSTILLREQUIRED"));
                    else
                        yield return "AnAl_PsychicRitualCreateChimera_NotEnoughMeatWarning_Multiple".Translate((MeatYieldNeededForChimeraWithOffset - totalMeatYield).Named("MEATSTILLREQUIRED"));
                }

                foreach (Pawn target in targets)
                {
                    List<Pawn> bondedPawns = new List<Pawn>();
                    target.relations.GetDirectRelations(PawnRelationDefOf.Bond, ref bondedPawns, p => !p.health.Dead);
                    if (bondedPawns.Count > 0)
                    {
                        if (bondedPawns.Count == 1)
                            yield return "AnAl_PsychicRitualCreateChimera_BondedTargetWarning_Single".Translate(target.Named("TARGET"), bondedPawns[0].Named("BONDEDPAWN"));
                        else
                        {
                            string bondedPawnsString = BondBreakHelper.CreateBondedPawnsString(bondedPawns);
                            yield return "AnAl_PsychicRitualCreateChimera_BondedTargetWarning_Multiple".Translate(target.Named("TARGET"), bondedPawnsString.Named("BONDEDPAWNS"));
                        }
                    }
                }
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (StatDrawEntry item in base.SpecialDisplayStats(req))
            {
                yield return item;
            }

            yield return new StatDrawEntry(StatCategoryDefOf.PsychicRituals, "AnAl_StatsReport_MeatRequired".Translate(), Mathf.RoundToInt(MeatYieldNeededForChimeraWithOffset).ToString(), "AnAl_StatsReport_MeatRequired_Desc".Translate(), 750);
        }

        public TaggedString MeatRequiredLabel => $"{"AnAl_StatsReport_MeatRequired".Translate()}: {Mathf.RoundToInt(MeatYieldNeededForChimeraWithOffset).ToString()}";

        public override TaggedString TimeAndOfferingLabel()
        {
            if (timeAndOfferingLabelCached is not null)
                return timeAndOfferingLabelCached;
            
            StringBuilder timeAndOfferingLabelBuilder = new StringBuilder(base.TimeAndOfferingLabel());

            timeAndOfferingLabelBuilder.AppendLine();
            timeAndOfferingLabelBuilder.Append(MeatRequiredLabel);

            timeAndOfferingLabelCached = timeAndOfferingLabelBuilder.ToString();
            return timeAndOfferingLabelCached;
        }

        // used when settings are changed
        public void InvalidateTimeAndOfferingLabelCache()
        {
            timeAndOfferingLabelCached = null;
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();

            if (meatYieldNeededForChimera is null)
                meatYieldNeededForChimera = AnimalProductionUtility.AdultMeatAmount(AlliedEntityDefOf.AnAl_ChimeraTame.race);

            if (chimeraTypeAnimals is null)
                chimeraTypeAnimals = new List<List<string>>(AlliedEntityDefOf.AnAl_ChimeraTame.alternateGraphics.Count + 1);
        }
    }
}
