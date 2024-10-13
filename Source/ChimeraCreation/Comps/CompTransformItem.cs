using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace AnomalyAllies.Comps
{
    public class CompTransformItem : ThingComp
    {
        public CompProperties_TransformItem Props => (CompProperties_TransformItem)props;

        public void TransformItem()
        {
            IntVec3 position = parent.Position;
            Map map = parent.Map;
            ThingOwner holder = parent.holdingOwner;
            int stackCount = parent.stackCount;
            int health = parent.HitPoints;
            int maxHealth = parent.MaxHitPoints;

            Selector selector = Find.Selector;
            bool selected = selector.IsSelected(parent);
            selector.Deselect(parent);

            Thing newThing = ThingMaker.MakeThing(Props.thingDefToTransformInto);
            newThing.stackCount = stackCount;
            newThing.HitPoints = (int)(((float)health / maxHealth) * newThing.MaxHitPoints);
            parent.Destroy();

            if (map is null)
            {
                holder.TryAdd(newThing, stackCount);
            }
            else
                GenSpawn.Spawn(newThing, position, map);

            if (selected)
            {
                selector.Select(newThing);
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
                yield return gizmo;

            if (!(Props.researchPrerequisite is null || Props.researchPrerequisite.IsFinished))
                yield break;
            else if (!(Props.researchPrerequisites is null || !Props.researchPrerequisites.Any((r) => !r.IsFinished)))
                yield break;

            Command_Action transformGizmo = new Command_Action();

            string defaultLabel = Props.label ?? $"Transform";
            defaultLabel = string.Format(defaultLabel, parent.def.label, Props.thingDefToTransformInto.label);
            transformGizmo.defaultLabel = defaultLabel;

            if (parent.stackCount > 1)
            {
                string descMultiple = Props.DescriptionPlural ?? "Transform these {0}s into {1}s.";
                descMultiple = string.Format(descMultiple, parent.def.label, Props.thingDefToTransformInto.label);
                transformGizmo.defaultDesc = descMultiple;
            }
            else
            {
                string descSingle = Props.description ?? "Transform this {0} into a {1}.";
                descSingle = string.Format(descSingle, parent.def.label, Props.thingDefToTransformInto.label);
                transformGizmo.defaultDesc = descSingle;
            }

            transformGizmo.icon = Props.thingDefToTransformInto.uiIcon;
            transformGizmo.iconAngle = Props.thingDefToTransformInto.uiIconAngle;
            transformGizmo.iconOffset = Props.thingDefToTransformInto.uiIconOffset;
            transformGizmo.SetColorOverride(Props.thingDefToTransformInto.graphicData.color);

            transformGizmo.action = TransformItem;

            yield return transformGizmo;
        }
    }

    public class CompProperties_TransformItem : CompProperties
    {
        public ThingDef thingDefToTransformInto;

        public string label;
        public string description;
        public string descriptionPlural;
        public string DescriptionPlural => descriptionPlural ?? description;
        // image path

        public ResearchProjectDef researchPrerequisite;
        public List<ResearchProjectDef> researchPrerequisites;

        public CompProperties_TransformItem()
        {
            compClass = typeof(CompTransformItem);
        }

        public CompProperties_TransformItem(Type compClass)
        {
            this.compClass = compClass;
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
                yield return error;

            if (thingDefToTransformInto is null)
                yield return $"{parentDef.defName} has an unspecified/invalid thingDefToTransformInto.";
        }
    }
}
