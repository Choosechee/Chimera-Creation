using System;
using System.Collections.Generic;
using Verse;

namespace AnomalyAllies.Comps
{
    public class CompMaxExisting : ThingComp
    {
        public int MaxExisting => ((CompProperties_MaxExisting)props).maxExisting;
        public static Dictionary<ThingDef, int> InstancesExisting => GameComponent_ThingsExistingWithMax.CurrentInstance.trackedInstancesExisting;

        protected bool hasExisted;
        protected bool shouldDestroy;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            // LogMethodRun(nameof(Initialize));

            try
            {
                if (InstancesExisting[parent.def] >= MaxExisting)
                {
                    shouldDestroy = true;
                    return;
                }

                InstancesExisting[parent.def]++;
                hasExisted = true;
            }
            catch (NullReferenceException)
            {
                AnomalyAlliesMod.Logger.Error("Something is null. What object is null?");

                bool gameComponentIsNull = (GameComponent_ThingsExistingWithMax.CurrentInstance is null);
                AnomalyAlliesMod.Logger.Warning($"GameComponent_ThingsExistingWithMax.CurrentInstance is null: {gameComponentIsNull.ToStringYesNo()}");
                if (!gameComponentIsNull)
                    AnomalyAlliesMod.Logger.Warning($"GameComponent_ThingsExistingWithMax.CurrentInstance.trackedInstancesExisting is null: {(GameComponent_ThingsExistingWithMax.CurrentInstance.trackedInstancesExisting is null).ToStringYesNo()}");

                bool parentIsNull = (parent is null);
                AnomalyAlliesMod.Logger.Warning($"parent is null: {parentIsNull.ToStringYesNo()}");
                if (!parentIsNull)
                    AnomalyAlliesMod.Logger.Warning($"parent.def is null: {(parent.def is null).ToStringYesNo()}");
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            // LogMethodRun(nameof(PostSpawnSetup));

            if (shouldDestroy)
            {
                parent.Destroy();
                return;
            }
            
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            // LogMethodRun(nameof(PostDestroy));

            if (hasExisted)
                InstancesExisting[parent.def]--;
        }

        internal void LogMethodRun(string methodName)
        {
            AnomalyAlliesMod.Logger.Message($"{GetType().Name}.{methodName} is running for {parent}");
        }
    }

    public class CompProperties_MaxExisting : CompProperties
    {
        public int maxExisting = 1;

        public CompProperties_MaxExisting()
        {
            compClass = typeof(CompMaxExisting);
        }

        public override void PostLoadSpecial(ThingDef parent)
        {
            base.PostLoadSpecial(parent);

            GameComponent_ThingsExistingWithMax.trackedThingDefs.Add(parent);
        }
    }

    public class GameComponent_ThingsExistingWithMax : GameComponent
    {
        public static readonly HashSet<ThingDef> trackedThingDefs = new HashSet<ThingDef>();
        
        public static GameComponent_ThingsExistingWithMax CurrentInstance { get; private set; }

        public readonly Dictionary<ThingDef, int> trackedInstancesExisting;

        public GameComponent_ThingsExistingWithMax(Game game) : this()
        {
        }

        public GameComponent_ThingsExistingWithMax()
        {
            trackedInstancesExisting = new Dictionary<ThingDef, int>(trackedThingDefs.Count);
            foreach (ThingDef thingDef in trackedThingDefs)
                trackedInstancesExisting[thingDef] = 0;
            
            CurrentInstance = this;
        }
    }
}
