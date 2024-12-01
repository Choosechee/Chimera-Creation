using AnomalyAllies.Comps;
using AnomalyAllies.DefOfs;
using AnomalyAllies.Patches;
using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // It is Unbounded Knapsack Problem time
        // bodySize is the "weight", and combatPower is the "value"
        // The "weight capacity" will be the cumulative body size of selected animals
        internal record FleshbeastItem
        {
            public readonly PawnKindDef fleshbeast;
            public readonly float bodySize;
            public readonly float combatPower;

            public FleshbeastItem(PawnKindDef fleshbeast) : this(fleshbeast, fleshbeast.combatPower)
            {
            }

            public FleshbeastItem(PawnKindDef fleshbeast, float customCombatPower)
            {
                this.fleshbeast = fleshbeast;
                bodySize = fleshbeast.RaceProps.baseBodySize;
                combatPower = customCombatPower;
            }
        }

        private static readonly float toughTriSpikeAverageCombatPower =
            (PawnKindDefOf.Toughspike.combatPower + PawnKindDefOf.Trispike.combatPower) / 2;

        private static readonly List<FleshbeastItem> fleshbeastItems = new List<FleshbeastItem>()
        {
            new FleshbeastItem(PawnKindDefOf.Fingerspike),
            /*
             * I averaged the combat power of the toughspike and trispike
             * and made them the same item because toughspikes have lower
             * combat power but the same body size as trispikes, so they 
             * would never get picked otherwise. Which one will be chosen
             * for each fleshbeast will be random.
            */
            new FleshbeastItem(PawnKindDefOf.Toughspike, toughTriSpikeAverageCombatPower),
            new FleshbeastItem(PawnKindDefOf.Bulbfreak)
        };

        private static List<PawnKindDef> FleshbeastsForAnimals(IEnumerable<Pawn> animals)
        {
            float bodySizeSum = animals.Sum(p => p.BodySize);
            return FleshbeastsForBodySize(bodySizeSum);
        }

        internal static List<PawnKindDef> FleshbeastsForBodySize(float bodySize)
        {
            List<FleshbeastItem> bestFleshbeasts = SolveFleshbeastKnapsack(bodySize).bestFleshbeasts;

            List<PawnKindDef> fleshbeastsForAnimals = new List<PawnKindDef>();
            foreach (FleshbeastItem fleshbeastItem in bestFleshbeasts)
            {
                if (fleshbeastItem.fleshbeast == PawnKindDefOf.Toughspike)
                    fleshbeastsForAnimals.Add(Rand.Bool ? fleshbeastItem.fleshbeast : PawnKindDefOf.Trispike);
                else
                    fleshbeastsForAnimals.Add(fleshbeastItem.fleshbeast);
            }

            //fleshbeastsForAnimals.Shuffle();
            return fleshbeastsForAnimals;
        }

        internal static (List<FleshbeastItem> bestFleshbeasts, float totalCombatPower) SolveFleshbeastKnapsack(float totalSize)
        {
            float totalCombatPower = FleshbeastKnapsackRecursive(totalSize);
            List<FleshbeastItem> bestFleshbeasts = new List<FleshbeastItem>();
            float sizeRemaining = totalSize;

            while (sizeRemaining > 0 && lookupTable.TryGetValue(sizeRemaining, out var computedValue) && computedValue.itemUsed is not null)
            {
                bestFleshbeasts.Add(computedValue.itemUsed);
                sizeRemaining -= computedValue.itemUsed.bodySize;
            }

            return (bestFleshbeasts, totalCombatPower);
        }

        private static readonly Dictionary<float, (float computedMax, FleshbeastItem itemUsed)> lookupTable = new();
        private static float FleshbeastKnapsackRecursive(float sizeRemaining)
        {
            if (sizeRemaining == 0f)
                return 0f;

            if (lookupTable.TryGetValue(sizeRemaining, out var computedValue))
                return computedValue.computedMax;

            float maxCombatPower = 0f;
            FleshbeastItem itemUsed = null;
            foreach (FleshbeastItem fleshbeastItem in fleshbeastItems)
            {
                if (fleshbeastItem.bodySize <= sizeRemaining)
                {
                    float newCombatPower = fleshbeastItem.combatPower + FleshbeastKnapsackRecursive(sizeRemaining - fleshbeastItem.bodySize);
                    if (newCombatPower > maxCombatPower)
                    {
                        maxCombatPower = newCombatPower;
                        itemUsed = fleshbeastItem;
                    }
                }
            }

            lookupTable[sizeRemaining] = (maxCombatPower, itemUsed);
            return maxCombatPower;
        }

        // End of knapsack problem stuff

        public override void Start(PsychicRitual psychicRitual, PsychicRitualGraph parent)
        {
            base.Start(psychicRitual, parent);

            Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
            var targets = new List<Pawn>(psychicRitual.assignments.AssignedPawns(targetRole));

            PsychicRitualDef_CreateChimera def = (PsychicRitualDef_CreateChimera)psychicRitual.def;
            float meatYieldRequired = def.MeatYieldNeededForChimeraWithOffset;
            float failureChance = def.fleshbeastChanceFromQualityCurve.Evaluate(psychicRitual.PowerPercent);

            if (invoker is not null && targets.Count() > 0)
                ApplyOutcome(psychicRitual, invoker, targets, meatYieldRequired, failureChance, def);
        }

        /*
        public override IEnumerable<Gizmo> GetBuildingGizmos(PsychicRitual psychicRitual, PsychicRitualGraph parent, Building building)
        {
            foreach (Gizmo gizmo in base.GetBuildingGizmos(psychicRitual, parent, building))
                yield return gizmo;

            if (DebugSettings.ShowDevGizmos)
            {
                Pawn invoker = psychicRitual.assignments.FirstAssignedPawn(invokerRole);
                var targets = new List<Pawn>(psychicRitual.assignments.AssignedPawns(targetRole));

                PsychicRitualDef_CreateChimera def = (PsychicRitualDef_CreateChimera)psychicRitual.def;
                float meatYieldRequired = def.MeatYieldNeededForChimeraWithOffset;

                Command_Action commandSuccess = new Command_Action();
                commandSuccess.defaultLabel = "DEV: Instantly succeed ritual";
                commandSuccess.action = () => ApplyOutcome(psychicRitual, invoker, targets, meatYieldRequired, 0f, def);

                Command_Action commandFailure = new Command_Action();
                commandFailure.defaultLabel = "DEV: Instantly fail ritual";
                commandFailure.action = () => ApplyOutcome(psychicRitual, invoker, targets, meatYieldRequired, 1f, def);

                yield return commandSuccess;
                yield return commandFailure;
            }
        }
        */

        private void ApplyOutcome(PsychicRitual psychicRitual, Pawn invoker, List<Pawn> targets, float meatYieldRequired, float failureChance, PsychicRitualDef_CreateChimera def)
        {
            float totalMeatYield = PsychicRitualDef_CreateChimera.TotalMeatYieldOfTargets(targets);
            Thing innvocation = psychicRitual.assignments.Target.Thing ?? invoker;
            var spawningCell = psychicRitual.assignments.Target.Cell;

            var chimeraTypeAnimals = def.chimeraTypeAnimals;
            List<int> validForcedChimeraTypes = new List<int>();

            int numberOfChimerasToCreate = 0;
            if (AnomalyAlliesMod.Settings.multipleChimeraCreation)
                numberOfChimerasToCreate = (int)(totalMeatYield / meatYieldRequired);
            else if (totalMeatYield >= meatYieldRequired)
                numberOfChimerasToCreate = 1;

            List<CompBondsFromPastLife.BondFromPastLife> targetBonds = new();
            foreach (Pawn target in targets)
            {
                List<Pawn> bondedPawns = new List<Pawn>();
                target.relations.GetDirectRelations(PawnRelationDefOf.Bond, ref bondedPawns);
                foreach (Pawn bondedPawn in bondedPawns)
                    targetBonds.Add(new(target, bondedPawn));

                target.DeSpawn();
            }

            List<PawnGenerationRequest> pawnGenerationRequests = new List<PawnGenerationRequest>();
            LetterDef outcomeLetterDef;
            TaggedString outcomeText;
            DamageDef deathMessage;
            if (!Rand.Chance(failureChance))
            {
                if (numberOfChimerasToCreate > 0)
                {
                    totalMeatYield -= meatYieldRequired * numberOfChimerasToCreate;
                    PawnKindDef chimeraTame = (Find.Anomaly.LevelDef != MonolithLevelDefOf.Disrupted) ? AlliedEntityDefOf.AnAl_ChimeraTame : AlliedEntityDefOf.AnAl_ChimeraTame_MonolithDisrupted;
                    for (int i = 0; i < numberOfChimerasToCreate; i++)
                        pawnGenerationRequests.Add(new PawnGenerationRequest(chimeraTame, Faction.OfPlayer, fixedBiologicalAge: 0f, fixedChronologicalAge: 0f));

                    outcomeLetterDef = LetterDefOf.PositiveEvent;
                    deathMessage = DeathMessageOf.AnAl_MorphedIntoChimera;

                    foreach (Pawn target in targets)
                    {
                        for (int i = 0; i < chimeraTypeAnimals.Count; i++)
                        {
                            List<string> animalNameList = chimeraTypeAnimals[i];
                            foreach (string animal in animalNameList)
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
                float meatAmountOfFleshbeasts = 0f;
                if (numberOfChimerasToCreate < 2)
                {
                    PawnKindDef fleshbeast = FleshbeastForAnimals(targets);
                    pawnGenerationRequests.Add(new PawnGenerationRequest(fleshbeast, Faction.OfEntities, fixedBiologicalAge: 0f, fixedChronologicalAge: 0f));
                    meatAmountOfFleshbeasts = AnimalProductionUtility.AdultMeatAmount(fleshbeast.race);
                }
                else
                {
                    List<PawnKindDef> fleshbeasts = FleshbeastsForAnimals(targets);
                    if (fleshbeasts.Empty())
                        fleshbeasts.Add(PawnKindDefOf.Fingerspike);

                    foreach (PawnKindDef fleshbeast in fleshbeasts)
                    {
                        pawnGenerationRequests.Add(new PawnGenerationRequest(fleshbeast, Faction.OfEntities, fixedBiologicalAge: 0f, fixedChronologicalAge: 0f));
                        meatAmountOfFleshbeasts += AnimalProductionUtility.AdultMeatAmount(fleshbeast.race);
                    }
                }

                deathMessage = DeathMessageOf.AnAl_MorphedIntoFleshbeast;
                if (pawnGenerationRequests.Count > 1 || pawnGenerationRequests[0].KindDef == PawnKindDefOf.Bulbfreak)
                    outcomeLetterDef = LetterDefOf.ThreatBig;
                else
                    outcomeLetterDef = LetterDefOf.ThreatSmall;

                totalMeatYield = Math.Max(totalMeatYield - meatAmountOfFleshbeasts, 0f);
            }

            List<Pawn> creations = new List<Pawn>();
            FleshbeastUtility.MeatExplosionSize meatExplosionSize = FleshbeastUtility.MeatExplosionSize.Small;
            IntRange filthRange = new IntRange(1, 2);
            if (pawnGenerationRequests.Any())
            {
                foreach (PawnGenerationRequest pawnGenerationRequest in pawnGenerationRequests)
                {
                    Pawn creation = PawnGenerator.GeneratePawn(pawnGenerationRequest);
                    creation.health.hediffSet.hediffs.RemoveAll(h => h.def.HasComp(typeof(HediffCompProperties_GetsPermanent)));

                    if (validForcedChimeraTypes.Count > 0)
                        creation.ForcedGraphic() = validForcedChimeraTypes.RandomElement();

                    if (creation.TryGetComp(out CompBondsFromPastLife comp))
                        comp.Bonds.AddRange(targetBonds);

                    GenSpawn.Spawn(creation, spawningCell, invoker.Map);

                    int stunTicks = 300;
                    bool creationHostile = false;
                    if (creation.Faction.HostileTo(Faction.OfPlayer))
                    {
                        stunTicks = 180;
                        creationHostile = true;
                    }
                    creation.stances.stunner.StunFor(stunTicks, innvocation, addBattleLog: creationHostile);

                    creations.Add(creation);
                }

                meatExplosionSize = FleshbeastUtility.ExplosionSizeFor(creations.MaxBy((c) => c.RaceProps.baseBodySize));
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
                    if (creations.Count == 1)
                        outcomeText = "AnAl_CreateChimera_Success".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), totalMeatYield.Named("MEATREFUNDED"));
                    else
                        outcomeText = "AnAl_CreateChimera_Multiple_Success".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), creations.Count.Named("CHIMERASCREATED"), totalMeatYield.Named("MEATREFUNDED"));
                    //Find.TickManager.Pause(); I forgot to remove this before the first release !!!
                    break;
                case "NeutralEvent":
                    outcomeText = "AnAl_CreateChimera_Nothing".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), totalMeatYield.Named("MEATREFUNDED"), innvocation);
                    break;
                case "ThreatSmall": case "ThreatBig":
                    if (creations.Count == 1)
                        outcomeText = "AnAl_CreateChimera_Failure".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), creations[0].Named("FLESHBEAST"), totalMeatYield.Named("MEATREFUNDED"));
                    else
                        outcomeText = "AnAl_CreateChimera_Multiple_Failure".Translate(invoker.Named("INVOKER"), psychicRitual.def.Named("RITUAL"), creations.Count.Named("FLESHBEASTSCREATED"), totalMeatYield.Named("MEATREFUNDED"));
                    break;
                default:
                    outcomeText = "SOMETHING HAS GONE TERRIBLY WRONG. PLEASE CONTACT CHOOSECHEE".Colorize(ColorLibrary.LogError);
                    break;
            }

            // Final cleanup
            for (int i = targets.Count - 1; i >= 0; i--)
                targets[i].Kill(new DamageInfo(deathMessage, 9999f, instigator: innvocation, intendedTarget: targets[i], instigatorGuilty: false, spawnFilth: false, checkForJobOverride: false));

            Find.LetterStack.ReceiveLetter("PsychicRitualCompleteLabel".Translate(psychicRitual.def.label), outcomeText, outcomeLetterDef, creations);
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
