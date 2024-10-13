using AnomalyAllies.DefOfs;
using AnomalyAllies.Patches;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI.Group;

namespace AnomalyAllies.ChimeraTame
{
    public class PsychicRitualToil_CreateChimera : PsychicRitualToil
    {
        private PsychicRitualRoleDef invokerRole;
        private PsychicRitualRoleDef targetRole;

        protected PsychicRitualToil_CreateChimera()
        {
        }

        public PsychicRitualToil_CreateChimera(PsychicRitualRoleDef invokerRole, PsychicRitualRoleDef targetRole)
        {
            this.invokerRole = invokerRole;
            this.targetRole = targetRole;
        }

        private static PawnKindDef FleshbeastForAnimals(IEnumerable<Pawn> animals)
        {
            float bodySizeSum = animals.Sum(p => p.BodySize);

            if (bodySizeSum < 0.75f)
                return PawnKindDefOf.Fingerspike;
            else if (bodySizeSum < 3.5f)
            {
                if (Rand.Bool)
                    return PawnKindDefOf.Toughspike;
                else
                    return PawnKindDefOf.Trispike;
            }
            else
                return PawnKindDefOf.Bulbfreak;
        }

        public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            base.Start(psychicRitual, parent);

            Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
            var targets = new List<Pawn>(psychicRitual.assignments.AssignedPawns(targetRole));

            PsychicRitualDef_CreateChimera def = (PsychicRitualDef_CreateChimera)psychicRitual.def;
            float meatYieldRequired = def.MeatYieldNeededForChimeraWithOffset;
            float failureChance = def.fleshbeastChanceFromQualityCurve.Evaluate(psychicRitual.PowerPercent);

            if (invoker != null && targets.Count() > 0)
                ApplyOutcome(psychicRitual, invoker, targets, meatYieldRequired, failureChance, def);
        }

        private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, List<Pawn> targets, float meatYieldRequired, float failureChance, PsychicRitualDef_CreateChimera def)
        {
            float totalMeatYield = PsychicRitualDef_CreateChimera.TotalMeatYieldOfTargets(targets);
            Thing innvocation = psychicRitual.assignments.Target.Thing ?? invoker;
            var spawningCell = psychicRitual.assignments.Target.Cell;

            var chimeraTypeAnimals = def.chimeraTypeAnimals;
            List<int> validForcedChimeraTypes = new List<int>();

            foreach (Pawn target in targets)
                target.DeSpawn();

            PawnGenerationRequest? pawnGenerationRequest = null;
            LetterDef outcomeLetterDef;
            TaggedString outcomeText;
            DamageDef deathMessage;
            if (!Rand.Chance(failureChance))
            {
                if (totalMeatYield >= meatYieldRequired)
                {
                    totalMeatYield -= meatYieldRequired;
                    pawnGenerationRequest = new PawnGenerationRequest(AlliedEntityDefOf.AnAl_ChimeraTame, Faction.OfPlayer, fixedBiologicalAge: 0f, fixedChronologicalAge: 0f);
                    outcomeLetterDef = LetterDefOf.PositiveEvent;
                    deathMessage = DeathMessageOf.AnAl_MorphedIntoChimera;

                    foreach (Pawn target in targets)
                    {
                        for (int i = 0; i < chimeraTypeAnimals.Count; i++)
                        {
                            List<string> animalNameList = chimeraTypeAnimals[i];
                            foreach (String animal in animalNameList)
                            {
                                if (target.kindDef.defName.ToLower().Contains(animal))
                                {
                                    validForcedChimeraTypes.Add(i - 1);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    outcomeLetterDef = LetterDefOf.NeutralEvent;
                    deathMessage = DeathMessageOf.AnAl_MorphedIntoMeat;
                }
            }
            else
            {
                PawnKindDef fleshbeast = FleshbeastForAnimals(targets);
                pawnGenerationRequest = new PawnGenerationRequest(fleshbeast, Faction.OfEntities, fixedBiologicalAge: 0f, fixedChronologicalAge: 0f);
                deathMessage = DeathMessageOf.AnAl_MorphedIntoFleshbeast;
                if (fleshbeast == PawnKindDefOf.Bulbfreak)
                    outcomeLetterDef = LetterDefOf.ThreatBig;
                else
                    outcomeLetterDef = LetterDefOf.ThreatSmall;

                float meatAmountOfFleshbeast = AnimalProductionUtility.AdultMeatAmount(fleshbeast.race);
                totalMeatYield = Math.Max(totalMeatYield - meatAmountOfFleshbeast, 0f);
            }

            Pawn creation = null;
            FleshbeastUtility.MeatExplosionSize meatExplosionSize = FleshbeastUtility.MeatExplosionSize.Small;
            IntRange filthRange = new IntRange(1, 2);
            if (pawnGenerationRequest.HasValue)
            {
                creation = PawnGenerator.GeneratePawn(pawnGenerationRequest.Value);
                creation.health.hediffSet.hediffs.RemoveAll(h => h.def.HasComp(typeof(HediffCompProperties_GetsPermanent)));
                if (validForcedChimeraTypes.Count > 0)
                    AnomalyAlliesMod.FieldProvider.SetForcedGraphic(creation, validForcedChimeraTypes.RandomElement());

                GenSpawn.Spawn(creation, spawningCell, invoker.Map);

                int stunTicks = 300;
                bool creationHostile = false;
                if (creation.Faction.HostileTo(Faction.OfPlayer))
                {
                    stunTicks = 180;
                    creationHostile = true;
                }
                creation.stances.stunner.StunFor(stunTicks, innvocation, addBattleLog: creationHostile);

                meatExplosionSize = FleshbeastUtility.ExplosionSizeFor(creation);
                filthRange = new IntRange(targets.Count * 3, targets.Count * 4);
            }
            FleshbeastUtility.MeatSplatter(filthRange.RandomInRange, spawningCell, invoker.Map, meatExplosionSize);

            int totalMeatYieldInt = (int)totalMeatYield;
            while (totalMeatYieldInt > 0)
            {
                Thing twistedMeat = ThingMaker.MakeThing(ThingDefOf.Meat_Twisted);
                twistedMeat.stackCount = Math.Min(totalMeatYieldInt, ThingDefOf.Meat_Twisted.stackLimit);
                totalMeatYieldInt -= twistedMeat.stackCount;
                GenSpawn.Spawn(twistedMeat, spawningCell, invoker.Map);
            }

            switch (outcomeLetterDef.defName)
            {
                case "PositiveEvent":
                    outcomeText = "AnAl_CreateChimera_Success".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), creation.Named("CHIMERA"), totalMeatYield.Named("MEATREFUNDED"));
                    Find.TickManager.Pause();
                    break;
                case "NeutralEvent":
                    outcomeText = "AnAl_CreateChimera_Nothing".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), totalMeatYield.Named("MEATREFUNDED"));
                    break;
                case "ThreatSmall": case "ThreatBig":
                    outcomeText = "AnAl_CreateChimera_Failure".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), creation.Named("FLESHBEAST"), totalMeatYield.Named("MEATREFUNDED"));
                    break;
                default:
                    outcomeText = "SOMETHING HAS GONE TERRIBLY WRONG. PLEASE CONTACT CHOOSECHEE";
                    break;
            }

            // Final cleanup
            for (int i = targets.Count - 1; i >= 0; i--)
                targets[i].Kill(new DamageInfo(deathMessage, 9999f, instigator: innvocation, intendedTarget: targets[i], instigatorGuilty: false, spawnFilth: false, checkForJobOverride: false));

            Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), outcomeText, outcomeLetterDef, creation);
        }

        public override void UpdateAllDuties(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            foreach (Pawn pawn in psychicRitual.assignments.AllAssignedPawns)
            {
                SetPawnDuty(pawn, psychicRitual, parent, DutyDefOf.Idle);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref invokerRole, "invokerRole");
            Scribe_Defs.Look(ref targetRole, "targetRole");
        }
    }
}
