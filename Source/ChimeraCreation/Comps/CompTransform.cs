using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace AnomalyAllies.Comps
{
    public class CompTransform : ThingComp
    {
        protected Pawn transformedPawn;
        public Pawn TransformedPawn => transformedPawn;

        public enum ActivePawn
        {
            ThisPawn,
            OtherPawn
        };
        public ActivePawn activePawn = ActivePawn.ThisPawn;

        public CompProperties_Transform Props => (CompProperties_Transform)props;
        public Pawn Pawn => (Pawn)parent;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            activePawn = ActivePawn.ThisPawn;

            if (TransformedPawn != null)
            {
                // AnomalyAlliesMod.Logger.Message("CompTransform: transformedPawn already exists. No initialization required");
                if (transformedPawn.HasComp<CompTransform>())
                {
                    CompTransform compTransform = transformedPawn.GetComp<CompTransform>();
                    if (compTransform.Props.pawnKindToTransformInto == Pawn.kindDef)
                        compTransform.activePawn = ActivePawn.OtherPawn;
                }
                return;
            }
            else if (respawningAfterLoad || (!Props.initializeTransformedPawnOnCreation))
            {
                // AnomalyAlliesMod.Logger.Message("CompTransform: Skipped initializing transformedPawn");
                return;
            }

            AnomalyAlliesMod.Logger.Message("CompTransform: Initializing transformedPawn");
            transformedPawn = CreateTransformedPawn();
        }

        /*public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (TransformedPawn.HasComp<CompTransform>())
            {
                CompTransform compTransform = TransformedPawn.GetComp<CompTransform>();
                if (compTransform.TransformedPawn == Pawn)
                    compTransform.transformedPawnSpawned = false;
            }
        }*/

        public Pawn CreateTransformedPawn()
        {
            PawnKindDef transformedPawnKind = Props.pawnKindToTransformInto;
            float biologicalAge = Pawn.ageTracker.AgeBiologicalYearsFloat;
            float chronologicalAge = Pawn.ageTracker.AgeChronologicalYearsFloat;

            PawnGenerationRequest transformedPawnRequest = new PawnGenerationRequest(
                transformedPawnKind, Pawn.Faction,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false,
                fixedBiologicalAge: biologicalAge, fixedChronologicalAge: chronologicalAge,
                allowFood: false, allowAddictions: false,
                fixedGender: Pawn.gender, forceNoGear: true
            );
            transformedPawnRequest.IsCreepJoiner = false;
            transformedPawnRequest.DontGivePreArrivalPathway = true;

            Pawn newPawn = PawnGenerator.GeneratePawn(transformedPawnRequest);
            if (Pawn.kindDef.alternateGraphics is object && AlternateGraphicsEqual(Pawn.kindDef.alternateGraphics, newPawn.kindDef.alternateGraphics))
            {
                int graphicIndex = Pawn.GetGraphicIndex();
                AnomalyAlliesMod.FieldProvider.SetForcedGraphic(newPawn, graphicIndex);
            }
            else
                AnomalyAlliesMod.Logger.Error("The only transformable creature right now are the chimeras. Their alternateGraphics should be equal, so this should never appear.");

            if (Pawn.Name != null)
                newPawn.Name = new NameSingle(Pawn.Name.ToStringFull);
            else
                newPawn.Name = null;
            
            /*
            newPawn.ageTracker = Pawn.ageTracker;
            newPawn.health = Pawn.health;
            newPawn.abilities = Pawn.abilities;
            newPawn.records = Pawn.records;
            */

            if (newPawn.HasComp<CompTransform>())
            {
                CompTransform compTransform = newPawn.GetComp<CompTransform>();
                if (compTransform.Props.pawnKindToTransformInto == Pawn.kindDef)
                {
                    compTransform.transformedPawn = Pawn;
                    compTransform.activePawn = ActivePawn.OtherPawn;
                }
            }
            
            return newPawn;
        }

        public Pawn TransformPawn()
        {
            if (!(Pawn.MapHeld != null || Pawn.IsCaravanMember()))
            {
                AnomalyAlliesMod.Logger.Error($"CompTransform: TransformPawn was called when Pawn was in a {Pawn?.ParentHolder?.GetType()?.Name}. Returning null");
                return null;
            }

            if (TransformedPawn is null)
                transformedPawn = CreateTransformedPawn();

            if (Pawn.Name != null)
                TransformedPawn.Name = new NameSingle(Pawn.Name.ToStringFull);
            else
                TransformedPawn.Name = null;
            CopyAge(Pawn, TransformedPawn);
            CopyHediffs(Pawn, TransformedPawn, Props.hediffDefsToDiscard);
            CopyNeeds(Pawn, TransformedPawn);
            // CopyAbilities(Pawn, TransformedPawn);
            CopyRecords(Pawn, TransformedPawn);
            if (TransformedPawn.Faction != Pawn.Faction)
                TransformedPawn.SetFaction(Pawn.Faction);

            Selector selector = Find.Selector;
            bool pawnSelected = selector.IsSelected(Pawn); // I have to do this here because despawning unselects them

            if (Pawn.MapHeld != null)
            {
                Pawn.DropAndForbidEverything();
                Rot4 rot4 = Pawn.Rotation;

                IThingHolder holder = Pawn.ParentHolder;
                if (holder is Map)
                {
                    GenSpawn.Spawn(TransformedPawn, Pawn.Position, Pawn.Map);
                    Pawn.DeSpawn();
                }
                else
                {
                    ThingOwner owner = holder.GetDirectlyHeldThings();
                    owner.Remove(Pawn);
                    owner.TryAdd(TransformedPawn);
                }

                if (TransformedPawn.Spawned)
                    TransformedPawn.Rotation = rot4;
            }
            else
            {
                Caravan caravan = Pawn.GetCaravan();
                caravan.AddPawn(TransformedPawn, true);
                caravan.RemovePawn(Pawn);
            }

            if (pawnSelected)
            {
                selector.Select(TransformedPawn);
                selector.Deselect(Pawn);
            }

            activePawn = ActivePawn.OtherPawn;
            if (transformedPawn.HasComp<CompTransform>())
            {
                CompTransform compTransform = transformedPawn.GetComp<CompTransform>();
                if (compTransform.Props.pawnKindToTransformInto == Pawn.kindDef)
                    compTransform.activePawn = ActivePawn.ThisPawn;
            }

            return TransformedPawn;
        }

        protected static bool AlternateGraphicsEqual(List<AlternateGraphic> alternateGraphics1, List<AlternateGraphic> alternateGraphics2)
        {
            if (alternateGraphics1.Count != alternateGraphics2.Count) { return false; }
            foreach (AlternateGraphic alternateGraphic1 in alternateGraphics1)
            {
                bool foundMatch = false;
                foreach (AlternateGraphic alternateGraphic2 in alternateGraphics2)
                {
                    if (alternateGraphic1.ForceGetField<string>("texPath") == alternateGraphic2.ForceGetField<string>("texPath"))
                    {
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch) { return false; }
            }
            return true;
        }

        protected static void CopyAge(Pawn firstPawn, Pawn secondPawn)
        {
            long biologicalAge = firstPawn.ageTracker.AgeBiologicalTicks;
            long chronologicalAge = firstPawn.ageTracker.AgeChronologicalTicks;

            secondPawn.ageTracker.AgeBiologicalTicks = biologicalAge;
            secondPawn.ageTracker.AgeChronologicalTicks = chronologicalAge;
        }

        protected static void CopyHediffs(Pawn firstPawn, Pawn secondPawn, List<HediffDef> hediffDefsToDiscard)
        {
            secondPawn.health.hediffSet.Clear();

            List<Hediff> firstPawnHediffs = firstPawn.health.hediffSet.hediffs;
            foreach (Hediff hediff in firstPawnHediffs)
            {
                if (hediffDefsToDiscard.Contains(hediff.def))
                {
                    continue;
                }
                
                if ((hediff.Part == null || secondPawn.RaceProps.body.AllParts.Contains(hediff.Part)))
                {
                    Hediff hediffCopy = HediffMaker.MakeHediff(hediff.def, secondPawn, hediff.Part);
                    hediffCopy.CopyFrom(hediff);
                    secondPawn.health.hediffSet.AddDirect(hediffCopy);
                }
            }
        }

        protected static void CopyNeeds(Pawn firstPawn, Pawn secondPawn)
        {
            List<Need> firstPawnNeeds = firstPawn.needs.AllNeeds;
            List<Need> secondPawnNeeds = secondPawn.needs.AllNeeds;
            HashSet<NeedDef> secondPawnNeedDefsAffected = new HashSet<NeedDef>();

            foreach (Need need in firstPawnNeeds)
            {
                Need needCopy = secondPawn.needs.TryGetNeed(need.def);
                if (needCopy != null)
                {
                    needCopy.CurLevel = need.CurLevel;
                    secondPawnNeedDefsAffected.Add(needCopy.def);
                }
            }

            foreach (Need need in secondPawnNeeds)
            {
                if (!secondPawnNeedDefsAffected.Contains(need.def))
                    need.CurLevel = need.MaxLevel;
            }
        }

        private static MethodInfo copyAbilities = typeof(GameComponent_PawnDuplicator).GetMethod("CopyAbilities", BindingFlags.NonPublic | BindingFlags.Static);
        protected static void CopyAbilities(Pawn firstPawn, Pawn secondPawn)
        {
            if (copyAbilities is null)
            {
                AnomalyAlliesMod.Logger.Error("CompTransform: copyAbilities is null");
                return;
            }
            
            copyAbilities.Invoke(null, new object[] { firstPawn, secondPawn });
        }

        private static FieldInfo recordsField = typeof(Pawn_RecordsTracker).GetField("records", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static void CopyRecords(Pawn firstPawn, Pawn secondPawn)
        {
            if (recordsField is null)
            {
                AnomalyAlliesMod.Logger.Error("CompTransform: recordsField is null");
                return;
            }

            DefMap<RecordDef, float> firstPawnRecords = (DefMap<RecordDef, float>)recordsField.GetValue(firstPawn.records);

            DefMap<RecordDef, float> firstPawnRecordsCopy = new DefMap<RecordDef, float>();
            foreach (KeyValuePair<RecordDef, float> keyValuePair in firstPawnRecords)
            {
                firstPawnRecordsCopy[keyValuePair.Key] = keyValuePair.Value;
            }

            recordsField.SetValue(secondPawn.records, firstPawnRecordsCopy);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
                yield return item;

            if (!DebugSettings.ShowDevGizmos)
                yield break;

            Command_Action transformAction = new Command_Action();
            transformAction.defaultLabel = $"Transform pawn into {Props.pawnKindToTransformInto.defName}";
            transformAction.action = delegate { TransformPawn(); };
            yield return transformAction;
        }

        public override void Notify_Downed()
        {
            base.Notify_Downed();

            if (AnomalyAlliesMod.Settings.wildChimerasAreTameable && Pawn.kindDef == PawnKindDefOf.Chimera)
                TransformPawn();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look<ActivePawn>(ref activePawn, "AnAl_scribedByReference");

            if (activePawn == ActivePawn.ThisPawn)
                Scribe_Deep.Look<Pawn>(ref transformedPawn, "AnAl_transformedPawn");
            else
                Scribe_References.Look<Pawn>(ref transformedPawn, "AnAl_transformedPawn", saveDestroyedThings: true);
        }
    }
    
    public class CompProperties_Transform : CompProperties
    {
        public PawnKindDef pawnKindToTransformInto;
        public bool initializeTransformedPawnOnCreation = true;
        public List<HediffDef> hediffDefsToDiscard = new List<HediffDef>();

        public CompProperties_Transform()
        {
            compClass = typeof(CompTransform);
        }

        public CompProperties_Transform(Type compClass)
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
            if (pawnKindToTransformInto == null)
                yield return $"{parentDef.defName} has an unspecified/invalid pawnKindToTransformInto.";
        }
    }
}
