using AnomalyAllies.Comps;
using AnomalyAllies.DefOfs;
using AnomalyAllies.ThinkNodes;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace AnomalyAllies.ChimeraTame
{
    [StaticConstructorOnStartup]
    public static class Setup
    {
        internal static FoodTypeFlags originalChimeraTameDiet;
        internal static string originalMeatHungerChimeraDescription;

        static void SaveBeginningState()
        {
            originalChimeraTameDiet = AlliedEntityDefOf.AnAl_ChimeraTame.RaceProps.foodType;
            originalMeatHungerChimeraDescription = AnAl_HediffDefOf.AnAl_MeatHungerChimera.description;
        }

        public static void Run()
        {
            AnomalyAlliesMod.Logger.Message("Beginning setup");
            ResolveSettings();
            ThinkTreeSetup();
            CopyBearRecipes();
            RemoveInheritedCompTransform();
            AnomalyAlliesMod.Logger.Message("Setup was successful");
        }

        static Setup()
        {
            SaveBeginningState();
            Run();
        }

        static void ResolveSettings()
        {
            AnomalyAlliesMod.Logger.Message("Applying saved settings");
            if (AnomalyAlliesMod.Settings.chimeraIsNormalCarnivore)
            {
                AlliedEntityDefOf.AnAl_ChimeraTame.RaceProps.foodType =
                    (FoodTypeFlags.CarnivoreAnimal | FoodTypeFlags.OvivoreAnimal);
                AnAl_HediffDefOf.AnAl_MeatHungerChimera.description = "AnAl_MeatHungerChimera_Description_ChimeraIsNormalCarnivore".Translate();
                AnAl_HediffDefOf.AnAl_MeatHungerChimera.GetType().GetField("descriptionCached", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(AnAl_HediffDefOf.AnAl_MeatHungerChimera, null);
            }
        }

        // setup AnAl_ChimeraTame think trees
        static void ThinkTreeSetup()
        {
            AnomalyAlliesMod.Logger.Message("Setting up AnAl_ChimeraTame main think tree");
            ThinkTreeMainRearranging();
            AnomalyAlliesMod.Logger.Message("Setting up AnAl_ChimeraTame constant think tree");
            ThinkTreeConstantSubNodeReplacing();
        }

        // move the new subNodes from AnAl_ChimeraTame to after the LordDuty node
        static void ThinkTreeMainRearranging()
        {
            List<ThinkNode> thinkTreeSubNodes = AlliedEntityDefOf.AnAl_ChimeraTame.RaceProps.thinkTreeMain.thinkRoot.subNodes;
            int indexToInsertNewNodes = thinkTreeSubNodes.FindIndex(
                (ThinkNode tn) => tn is ThinkNode_Subtree subtree
                && subtree.ForceGetField<ThinkTreeDef>("treeDef").defName == "LordDuty")
                + 1;
            int idleErrorIndex = thinkTreeSubNodes.FindIndex((ThinkNode tn) => tn is JobGiver_IdleError);

            if (indexToInsertNewNodes < 0)
            {
                AnomalyAlliesMod.Logger.Error("indexToInsertNewNodes wasn't found");
                return;
            }
            if (idleErrorIndex < 0)
            {
                AnomalyAlliesMod.Logger.Error("idleErrorIndex wasn't found");
                return;
            }

            List<ThinkNode> newNodes = new List<ThinkNode>();
            for (int i = thinkTreeSubNodes.Count - 1; i > idleErrorIndex; i--)
            {
                ThinkNode thinkNode = thinkTreeSubNodes[i];
                thinkTreeSubNodes.RemoveAt(i);
                newNodes.Insert(0, thinkNode);
            }

            thinkTreeSubNodes.InsertRange(indexToInsertNewNodes, newNodes);
        }

        // change any subNodes from AnAl_ChimeraTameConstant that are ThinkNode_ConditionalCanDoConstantThinkTreeJobNow to ThinkNode_ConditionalCanDoConstantThinkTreeJobNowEntity
        static void ThinkTreeConstantSubNodeReplacing()
        {
            List<ThinkNode> thinkTreeSubNodes = AlliedEntityDefOf.AnAl_ChimeraTame.RaceProps.thinkTreeConstant.thinkRoot.subNodes;
            for (int i = 0; i < thinkTreeSubNodes.Count; i++)
            {
                ThinkNode subNode = thinkTreeSubNodes[i];
                if (subNode is ThinkNode_ConditionalCanDoConstantThinkTreeJobNow subNodeMatch)
                {
                    ThinkNode_ConditionalCanDoConstantThinkTreeJobNowEntity replacementNode = new ThinkNode_ConditionalCanDoConstantThinkTreeJobNowEntity();
                    replacementNode.DeepCopyToThisFrom(subNodeMatch);
                    thinkTreeSubNodes[i] = replacementNode;
                }
            }
        }

        static void CopyBearRecipes()
        {
            if (AlliedEntityDefOf.AnAl_ChimeraTame.race.recipes is null)
            {
                AnomalyAlliesMod.Logger.Message("AnAl_ChimeraTame.recipes is null. Assigning a List instance to it");
                AlliedEntityDefOf.AnAl_ChimeraTame.race.recipes = new List<RecipeDef>();
            }

            AnomalyAlliesMod.Logger.Message("Copying Bear_Grizzly recipes to AnAl_ChimeraTame");
            AlliedEntityDefOf.AnAl_ChimeraTame.race.recipes.AddRange(VanillaDefOf.Bear_Grizzly.recipes);

            // set allRecipesCached to null so it will be recalculated
            AnomalyAlliesMod.Logger.Message("Recipe copying successful. Decaching AnAl_ChimeraTame.allRecipesCached so it will be recalculated");
            AlliedEntityDefOf.AnAl_ChimeraTame.race.GetType().GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(AlliedEntityDefOf.AnAl_ChimeraTame.race, null);
        }

        static void RemoveInheritedCompTransform()
        {
            AnomalyAlliesMod.Logger.Message("Removing the CompProperties_Transform inherited from Chimera from AnAl_ChimeraTame");
            List<CompProperties> comps = AlliedEntityDefOf.AnAl_ChimeraTame.race.comps;
            CompProperties compToRemove = comps.Find((CompProperties comp) =>
                comp is CompProperties_Transform compTransform
                && compTransform.pawnKindToTransformInto == AlliedEntityDefOf.AnAl_ChimeraTame
            );

            if (compToRemove is not null)
                comps.Remove(compToRemove);
        }
    }
}
