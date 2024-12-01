using AnomalyAllies.DefOfs;
using AnomalyAllies.Misc;
using AnomalyAllies.Patches;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace AnomalyAllies.Comps
{
    public class CompTransform : ThingComp, ISignalReceiver
    {
        public List<CompProperties_Transform.Transformation> Transformations => ((CompProperties_Transform)props).transformations;
        public bool AffectedByHoraxConnection => ((CompProperties_Transform)props).affectedByHoraxConnection;
        public Pawn Pawn => (Pawn)parent;

        protected Dictionary<int, Pawn> transformedPawns = new Dictionary<int, Pawn>();
        protected Dictionary<PawnKindDef, int> pawnKindTransformedPawnsIndex = new Dictionary<PawnKindDef, int>();
        public Dictionary<int, Pawn> TransformedPawns
        {
            get
            {
                Dictionary<int, Pawn> transformedPawnsCopy = new Dictionary<int, Pawn>(transformedPawns);
                return transformedPawnsCopy;
            }
        }

        protected bool active;
        public bool Active => active;
        protected bool wasConnectedToHorax;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            wasConnectedToHorax = Pawn.IsConnectedToHorax();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            active = true;
            if (AffectedByHoraxConnection)
                Find.SignalManager.RegisterReceiver(this);

            if (respawningAfterLoad)
                return;

            for (int i = 0; i < Transformations.Count; i++)
            {
                CompProperties_Transform.Transformation transformation = Transformations[i];
                if (transformation.initializeOnCreation && !transformedPawns.ContainsKey(i))
                {
                    CreateTransformedPawn(transformation);
                }
            }
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            
            active = false;
            if (AffectedByHoraxConnection)
                Find.SignalManager.DeregisterReceiver(this);
        }

        protected const string invalidTransformation = "Transformation {0} is not a transformation supported by this CompTransform";

        public Pawn CreateTransformedPawn(CompProperties_Transform.Transformation transformation)
        {
            int transformationIndex = Transformations.IndexOf(transformation);
            Pawn newPawn = null;
            if (transformationIndex < 0)
                throw new ArgumentException(string.Format(invalidTransformation, transformation), "transformation");
            else if (transformedPawns.TryGetValue(transformationIndex, out newPawn))
            {
                AnomalyAlliesMod.Logger.Warning($"CreateTransformedPawn for a transformation {transformation} that already has a corresponding pawn");
                return newPawn;
            }
            
            PawnKindDef transformedPawnKind = transformation.pawnKind;
            foreach (Pawn transformedPawn in transformedPawns.Values)
            {
                if (transformedPawn.TryGetComp(out CompTransform compTransform))
                {
                    foreach (Pawn otherTransformedPawn in compTransform.transformedPawns.Values)
                    {
                        if (transformedPawnKind == otherTransformedPawn.kindDef)
                        {
                            newPawn = otherTransformedPawn;
                            goto FullBreak;
                        }
                    }
                }
            }
            FullBreak:

            if (newPawn is null)
            {
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

                newPawn = PawnGenerator.GeneratePawn(transformedPawnRequest);
                if (Pawn.kindDef.alternateGraphics is not null && AlternateGraphicsEqual(Pawn.kindDef.alternateGraphics, newPawn.kindDef.alternateGraphics))
                {
                    int graphicIndex = Pawn.GetGraphicIndex();
                    newPawn.ForcedGraphic() = graphicIndex;
                }
                else
                    AnomalyAlliesMod.Logger.Error("The only transformable creature right now are the chimeras. Their alternateGraphics should be equal, so this should never appear.");

                if (Pawn.Name is not null)
                    newPawn.Name = Pawn.Name.GetCopy();
                else
                    newPawn.Name = null;
            }

            foreach (Pawn transformedPawn in transformedPawns.Values)
            {
                if (transformedPawn.TryGetComp(out CompTransform compTransform))
                {
                    for (int i = 0; i < compTransform.Transformations.Count; i++)
                    {
                        CompProperties_Transform.Transformation otherTransformation = compTransform.Transformations[i];
                        if (transformedPawnKind == otherTransformation.pawnKind)
                        {
                            if (!compTransform.pawnKindTransformedPawnsIndex.TryGetValue(transformedPawnKind, out int j))
                            {
                                compTransform.transformedPawns[i] = newPawn;
                                compTransform.pawnKindTransformedPawnsIndex[transformedPawnKind] = i;
                            }
                            else if (compTransform.transformedPawns[j] != newPawn)
                                AnomalyAlliesMod.Logger.Error($"A duplicate pawn of kind {transformedPawnKind} was created in linked CompTransform");
                        }
                    }
                }
            }
            
            {
                if (newPawn.TryGetComp(out CompTransform compTransform))
                {
                    for (int i = 0; i < compTransform.Transformations.Count; i++)
                    {
                        CompProperties_Transform.Transformation newPawnTransformation = compTransform.Transformations[i];
                        if (newPawnTransformation.pawnKind == Pawn.kindDef)
                        {
                            compTransform.transformedPawns[i] = Pawn;
                            compTransform.pawnKindTransformedPawnsIndex[Pawn.kindDef] = i;
                        }
                        else if (pawnKindTransformedPawnsIndex.TryGetValue(newPawnTransformation.pawnKind, out int j))
                        {
                            compTransform.transformedPawns[i] = transformedPawns[j];
                            compTransform.pawnKindTransformedPawnsIndex[newPawnTransformation.pawnKind] = i;
                        }
                    }
                }
            }

            transformedPawns[transformationIndex] = newPawn;
            pawnKindTransformedPawnsIndex[transformedPawnKind] = transformationIndex;
            return newPawn;
        }

        public Pawn TransformPawn(CompProperties_Transform.Transformation transformation)
        {
            if (!(Pawn.MapHeld is not null || Pawn.IsCaravanMember()))
            {
                AnomalyAlliesMod.Logger.Error($"CompTransform: TransformPawn was called when Pawn was in a {Pawn?.ParentHolder?.GetType()?.Name}. Returning null");
                return null;
            }

            if (!pawnKindTransformedPawnsIndex.TryGetValue(transformation.pawnKind, out int i))
            {
                i = Transformations.IndexOf(transformation);
                if (i < 0)
                    throw new ArgumentException(string.Format(invalidTransformation, transformation), "transformation");

                CreateTransformedPawn(transformation);
            }
            Pawn transformedPawn = transformedPawns[i];

            transformedPawn.Name = Pawn.Name.GetCopy();
            CopyAge(Pawn, transformedPawn);
            CopyHediffs(Pawn, transformedPawn, transformation.hediffDefsToDiscard);
            CopyNeeds(Pawn, transformedPawn);
            if (Pawn.abilities?.abilities is not null && transformedPawn.abilities?.abilities is not null)
                CopyAbilities(Pawn, transformedPawn);
            if (transformedPawn.Faction != Pawn.Faction)
                transformedPawn.SetFaction(Pawn.Faction);
            CopyTraining(Pawn, transformedPawn);
            CopySettings(Pawn, transformedPawn);
            CopyRecords(Pawn, transformedPawn);
            MoveRelationships(Pawn, transformedPawn);

            Selector selector = Find.Selector;
            bool pawnSelected = selector.IsSelected(Pawn); // I have to do this here because despawning unselects them

            if (Pawn.MapHeld is not null)
            {
                Pawn.DropAndForbidEverything();
                Rot4 rot4 = Pawn.Rotation;

                IThingHolder holder = Pawn.ParentHolder;
                if (holder is Map)
                {
                    GenSpawn.Spawn(transformedPawn, Pawn.Position, Pawn.Map);
                    Pawn.DeSpawn();
                }
                else
                {
                    ThingOwner owner = holder.GetDirectlyHeldThings();
                    owner.Remove(Pawn);
                    owner.TryAdd(transformedPawn);
                }

                if (transformedPawn.Spawned)
                    transformedPawn.Rotation = rot4;
            }
            else
            {
                Caravan caravan = Pawn.GetCaravan();
                caravan.AddPawn(transformedPawn, true);
                caravan.RemovePawn(Pawn);
            }

            if (pawnSelected)
            {
                selector.Select(transformedPawn);
                selector.Deselect(Pawn);
            }

            return transformedPawn;
        }

        public Pawn TransformPawnHostile()
        {
            CompProperties_Transform.Transformation.Type[] typesForTransformation =
            {
                CompProperties_Transform.Transformation.Type.Hostile,
                (Pawn.IsConnectedToHorax()) ? CompProperties_Transform.Transformation.Type.MonolithActive : CompProperties_Transform.Transformation.Type.MonolithDisrupted
            };

            CompProperties_Transform.Transformation hostileTransformation = Transformations.Find(CompProperties_Transform.TransformationWithTypesPredicate(typesForTransformation));
            if (hostileTransformation is null)
                hostileTransformation = Transformations.Find(CompProperties_Transform.TransformationWithTypesPredicate(typesForTransformation[0]));
            if (hostileTransformation is /*still*/ null)
                return null;

            return TransformPawn(hostileTransformation);
        }

        public Pawn TransformPawnFriendly()
        {
            CompProperties_Transform.Transformation.Type[] typesForTransformation =
            {
                CompProperties_Transform.Transformation.Type.Friendly,
                (Pawn.IsConnectedToHorax()) ? CompProperties_Transform.Transformation.Type.MonolithActive : CompProperties_Transform.Transformation.Type.MonolithDisrupted
            };

            CompProperties_Transform.Transformation friendlyTransformation = Transformations.Find(CompProperties_Transform.TransformationWithTypesPredicate(typesForTransformation));
            if (friendlyTransformation is null)
                friendlyTransformation = Transformations.Find(CompProperties_Transform.TransformationWithTypesPredicate(typesForTransformation[0]));
            if (friendlyTransformation is /*still*/ null)
                return null;

            return TransformPawn(friendlyTransformation);
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
                if (needCopy is not null)
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

        protected static readonly Action<Pawn, Pawn> CopyAbilities = (Action<Pawn, Pawn>)typeof(GameComponent_PawnDuplicator).Method("CopyAbilities").CreateDelegate(typeof(Action<Pawn, Pawn>));

        private static readonly FieldInfo allowedAreasField = typeof(Pawn_PlayerSettings).GetField("allowedAreas", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static void CopySettings(Pawn firstPawn, Pawn secondPawn)
        {
            var firstPawnAllowedAreas = (Dictionary<Map, Area>)allowedAreasField.GetValue(firstPawn.playerSettings);
            var secondPawnAllowedAreas = (Dictionary<Map, Area>)allowedAreasField.GetValue(secondPawn.playerSettings);

            secondPawnAllowedAreas.Clear();
            foreach (KeyValuePair<Map, Area> allowedAreaForMap in firstPawnAllowedAreas)
                secondPawnAllowedAreas[allowedAreaForMap.Key] = allowedAreaForMap.Value;

            secondPawn.playerSettings.joinTick = firstPawn.playerSettings.joinTick;
            secondPawn.playerSettings.medCare = firstPawn.playerSettings.medCare;
            secondPawn.playerSettings.hostilityResponse = firstPawn.playerSettings.hostilityResponse;
            secondPawn.playerSettings.selfTend = firstPawn.playerSettings.selfTend;
            secondPawn.playerSettings.displayOrder = firstPawn.playerSettings.displayOrder;

            secondPawn.playerSettings.Master = firstPawn.playerSettings.Master;
            secondPawn.playerSettings.followDrafted = firstPawn.playerSettings.followDrafted;
            secondPawn.playerSettings.followFieldwork = firstPawn.playerSettings.followFieldwork;
            secondPawn.playerSettings.animalsReleased = firstPawn.playerSettings.animalsReleased;
            
        }

        private static FieldInfo recordsField = typeof(Pawn_RecordsTracker).GetField("records", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static void CopyRecords(Pawn firstPawn, Pawn secondPawn)
        {
            if (recordsField is null)
            {
                AnomalyAlliesMod.Logger.Error("CompTransform: recordsField is null");
                return;
            }

            var firstPawnRecords = (DefMap<RecordDef, float>)recordsField.GetValue(firstPawn.records);
            var secondPawnRecords = (DefMap<RecordDef, float>)recordsField.GetValue(secondPawn.records);

            foreach (KeyValuePair<RecordDef, float> keyValuePair in firstPawnRecords)
                secondPawnRecords[keyValuePair.Key] = keyValuePair.Value;
        }

        private static FieldInfo wantedTrainablesField = typeof(Pawn_TrainingTracker).GetField("wantedTrainables", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo stepsField = typeof(Pawn_TrainingTracker).GetField("steps", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo learnedField = typeof(Pawn_TrainingTracker).GetField("learned", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo countDecayFromField = typeof(Pawn_TrainingTracker).GetField("countDecayFrom", BindingFlags.NonPublic | BindingFlags.Instance);
        protected static void CopyTraining(Pawn firstPawn, Pawn secondPawn)
        {
            var firstPawnWantedTrainables = (DefMap<TrainableDef, bool>)wantedTrainablesField.GetValue(firstPawn.training);
            var firstPawnSteps = (DefMap<TrainableDef, int>)stepsField.GetValue(firstPawn.training);
            var firstPawnLearned = (DefMap<TrainableDef, bool>)learnedField.GetValue(firstPawn.training);
            var firstPawnCountDecayFrom = (int)countDecayFromField.GetValue(firstPawn.training);

            var secondPawnWantedTrainables = (DefMap<TrainableDef, bool>)wantedTrainablesField.GetValue(secondPawn.training);
            var secondPawnSteps = (DefMap<TrainableDef, int>)stepsField.GetValue(secondPawn.training);
            var secondPawnLearned = (DefMap<TrainableDef, bool>)learnedField.GetValue(secondPawn.training);

            foreach (KeyValuePair<TrainableDef, bool> keyValuePair in firstPawnWantedTrainables)
                secondPawnWantedTrainables[keyValuePair.Key] = 
                    (secondPawn.RaceProps.trainability.intelligenceOrder >= keyValuePair.Key.requiredTrainability.intelligenceOrder)
                    ? keyValuePair.Value : false;

            foreach (KeyValuePair<TrainableDef, int> keyValuePair in firstPawnSteps)
                secondPawnSteps[keyValuePair.Key] = keyValuePair.Value;

            foreach (KeyValuePair<TrainableDef, bool> keyValuePair in firstPawnLearned)
                secondPawnLearned[keyValuePair.Key] = keyValuePair.Value;

            countDecayFromField.SetValue(secondPawn.training, firstPawnCountDecayFrom);
        }

        protected static void MoveRelationships(Pawn firstPawn, Pawn secondPawn)
        {
            List<DirectPawnRelation> firstPawnDirectRelationsCopy = new List<DirectPawnRelation>(firstPawn.relations.DirectRelations);
            List<VirtualPawnRelation> firstPawnVirtualRelationsCopy = new List<VirtualPawnRelation>(firstPawn.relations.VirtualRelations);

            firstPawn.relations.ClearAllRelations();
            secondPawn.relations.ClearAllRelations();

            foreach (DirectPawnRelation directRelation in firstPawnDirectRelationsCopy)
                secondPawn.relations.AddDirectRelation(directRelation.def, directRelation.otherPawn);

            secondPawn.relations.VirtualRelations.AddRange(firstPawnVirtualRelationsCopy);
        }

        public const string monolithFragmentImplantedSignal = "AnAl_MonolithFragmentImplanted";
        public static readonly HashSet<string> signalsToReceive = new HashSet<string>()
        {
            "MonolithLevelChanged", monolithFragmentImplantedSignal
        };
        public override void Notify_SignalReceived(Signal signal)
        {
            base.Notify_SignalReceived(signal);

            if (signalsToReceive.Contains(signal.tag) && Pawn.IsConnectedToHorax() != wasConnectedToHorax)
            {
                var transFormationType = (Pawn.IsConnectedToHorax()) ? CompProperties_Transform.Transformation.Type.MonolithActive : CompProperties_Transform.Transformation.Type.MonolithDisrupted;
                var transformationPredicate = CompProperties_Transform.TransformationWithTypesPredicate(transFormationType);
                
                var monolithDisruptedTransformation = Transformations.Find(transformationPredicate);
                if (monolithDisruptedTransformation is not null)
                    TransformPawn(monolithDisruptedTransformation);
            }
            wasConnectedToHorax = Pawn.IsConnectedToHorax();
        }

        public override void Notify_Downed()
        {
            base.Notify_Downed();

            if (AnomalyAlliesMod.Settings.wildChimerasAreTameable && Pawn.kindDef == PawnKindDefOf.Chimera)
                TransformPawnFriendly();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
                yield return item;

            if (!DebugSettings.ShowDevGizmos)
                yield break;

            foreach (CompProperties_Transform.Transformation transformation in Transformations)
            {
                Command_Action transformAction = new Command_Action();
                transformAction.defaultLabel = $"Transform pawn into {transformation.pawnKind}";
                transformAction.action = delegate { TransformPawn(transformation); };
                yield return transformAction;
            }
        }

        private List<int> transformedPawnsKeys = new List<int>();
        private List<Pawn> transformedPawnsValues = new List<Pawn>();

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref active, "AnAl_isActivePawnForCompTransform");

            LookMode valueLookMode = (active) ? LookMode.Deep : LookMode.Reference;
            Scribe_Collections.Look(ref transformedPawns, "AnAl_transformedPawns", LookMode.Value, valueLookMode, ref transformedPawnsKeys, ref transformedPawnsValues, saveDestroyedValues: true);

            Scribe_Collections.Look(ref pawnKindTransformedPawnsIndex, "AnAl_pawnKindTransformedPawnsIndex", LookMode.Def, LookMode.Value);
        }
    }
    
    public class CompProperties_Transform : CompProperties
    {
        public class Transformation
        {
            [Flags]
            public enum Type : byte
            {
                Misc = 1,
                Hostile = 2,
                Friendly = 4,
                MonolithActive = 8,
                MonolithDisrupted = 16
            }
            
            public PawnKindDef pawnKind;
            public Type type = Type.Misc;
            public bool initializeOnCreation = true;
            public List<HediffDef> hediffDefsToDiscard = new List<HediffDef>();

            public Transformation()
            {
            }

            public Transformation(PawnKindDef pawnKind)
            {
                this.pawnKind = pawnKind;
            }

            public Transformation(PawnKindDef pawnKind, Type type)
            {
                this.pawnKind = pawnKind;
                this.type = type;
            }

            public override string ToString()
            {
                return $"(PawnKindDef: {pawnKind}, Type: {type})";
            }
        }
        
        public List<Transformation> transformations = new List<Transformation>();
        public bool affectedByHoraxConnection;

        public CompProperties_Transform()
        {
            compClass = typeof(CompTransform);
        }

        public CompProperties_Transform(Type compClass)
        {
            this.compClass = compClass;
        }

        public static Predicate<Transformation> TransformationWithTypesPredicate(params Transformation.Type[] types)
        {
            return (t) =>
            {
                foreach (Transformation.Type type in types)
                {
                    if (!t.type.HasFlag(type))
                        return false;
                }
                return true;
            };
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (transformations.Empty())
            {
                yield return "There are no specified transformations";
            }
            else
            {
                for (int i = 0; i < transformations.Count; i++)
                {
                    if (transformations[i].pawnKind is null)
                        yield return $"Transformation {i} does not specify a PawnKindDef";
                }
            }
        }

        protected static ConcurrentDictionary<ThingDef, Mutex> mutexesForThingDefs = new ConcurrentDictionary<ThingDef, Mutex>(GenThreading.ProcessorCount, 31);
        // This can't be PostLoadSpecial because it gives an error when run from that about a collection being changed while iterated?
        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

            mutexesForThingDefs.TryAdd(parentDef, new Mutex());
            Mutex mutex = mutexesForThingDefs[parentDef];
            mutex.WaitOne();

            for (int i = 0, timesDecremented = 0; i < parentDef.comps.IndexOf(this); i++)
            {
                CompProperties compProperties = parentDef.comps[i];
                if (compProperties is CompProperties_Transform)
                {
                    AnomalyAlliesMod.Logger.Message($"Removing duplicate CompProperties_Transform at position {i + timesDecremented} in {parentDef}.comps");
                    parentDef.comps.Remove(compProperties);

                    i--;
                    timesDecremented++;
                }
            }

            mutex.ReleaseMutex();
        }
    }
}
