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
            SaveBeginningState();
            AnomalyAlliesMod.Logger.Message("Beginning setup");
            ResolveSettings();
            ThinkTreeSetup();
            CopyBearRecipes();
            RemoveInheritedForChimeraMonolithDisrupted();
            AnomalyAlliesMod.Logger.Message("Setup was successful");
        }

        static Setup()
        {
            Run();
        }
        
        static void ResolveSettings()
        {
            AnomalyAlliesMod.Logger.Message("Applying saved settings");
            if (AnomalyAlliesMod.Settings.chimeraIsNormalCarnivore)
            {
                foreach (PawnKindDef chimeraTameDef in AlliedEntityGroups.chimeraTameDefs)
                    chimeraTameDef.RaceProps.foodType =
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
            List<ThinkNode> thinkTreeSubNodes = AnAl_ThinkTreeDefOf.AnAl_ChimeraTame.thinkRoot.subNodes;
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

        // Change any subNodes from AnAl_ChimeraTameConstant that are
        // ThinkNode_ConditionalCanDoConstantThinkTreeJobNow to
        // ThinkNode_ConditionalCanDoConstantThinkTreeJobNowEntity
        // This fixes chimeras running away while they are enemies
        // when "Chimera betrayal is permanent" is disabled.
        static void ThinkTreeConstantSubNodeReplacing()
        {
            List<ThinkNode> thinkTreeSubNodes = AnAl_ThinkTreeDefOf.AnAl_ChimeraTameConstant.thinkRoot.subNodes;
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
            foreach (PawnKindDef chimeraTameDef in AlliedEntityGroups.chimeraTameDefs)
            {
                if (chimeraTameDef.race.recipes is null)
                {
                    AnomalyAlliesMod.Logger.Message($"{chimeraTameDef.race.defName}.recipes is null. Assigning a List instance to it");
                    chimeraTameDef.race.recipes = new List<RecipeDef>();
                }

                AnomalyAlliesMod.Logger.Message($"Copying Bear_Grizzly recipes to {chimeraTameDef.race.defName}");
                chimeraTameDef.race.recipes.AddRange(VanillaDefOf.Bear_Grizzly.recipes);

                // set allRecipesCached to null so it will be recalculated
                AnomalyAlliesMod.Logger.Message($"Recipe copying successful. Decaching {chimeraTameDef.race.defName}.allRecipesCached so it will be recalculated");
                chimeraTameDef.race.GetType().GetField("allRecipesCached", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(chimeraTameDef.race, null);
            }
        }

        static void RemoveInheritedForChimeraMonolithDisrupted()
        {
            AnomalyAlliesMod.Logger.Message("Removing the AnAl_MeatHungerGiverChimera inherited from AnAl_ChimeraTame from AnAl_ChimeraTame_MonolithDisrupted");

            List<HediffGiverSetDef> hediffGiverSetDefs = AlliedEntityDefOf.AnAl_ChimeraTame_MonolithDisrupted.RaceProps.hediffGiverSets;
            HediffGiverSetDef hediffGiverSetDefToRemove = hediffGiverSetDefs.Find((hgsf) => hgsf.defName == "AnAl_MeatHungerGiverChimera");
            hediffGiverSetDefs.Remove(hediffGiverSetDefToRemove);

            AnomalyAlliesMod.Logger.Message("Removing the CompProperties_RevengeOnSlaughter inherited from AnAl_ChimeraTame from AnAl_ChimeraTame_MonolithDisrupted");

            List<CompProperties> compPropertiesList = AlliedEntityDefOf.AnAl_ChimeraTame_MonolithDisrupted.race.comps;
            CompProperties compPropertiesToRemove = compPropertiesList.Find((comp) => comp is CompProperties_RevengeOnSlaughter);
            compPropertiesList.Remove(compPropertiesToRemove);
        }
    }
}
