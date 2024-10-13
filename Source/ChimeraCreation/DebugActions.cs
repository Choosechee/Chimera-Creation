using LudeonTK;
using RimWorld;
using System.Reflection;
using Verse;

namespace AnomalyAllies
{
    public static class DebugActions
    {
        [DebugAction("Anomaly", "Spawn tame chimera", requiresAnomaly = true, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void SpawnTameChimera()
        {
            PawnKindDef chimeraDef = DefDatabase<PawnKindDef>.GetNamed("AnAl_ChimeraTame");
            Pawn chimera = PawnGenerator.GeneratePawn(chimeraDef, Faction.OfPlayer);
            // ChimeraCreationMod.myLogger.Message(chimera.def.defName);

            GenSpawn.Spawn(chimera, UI.MouseCell(), Find.CurrentMap);

            object[] forPostPawnSpawn = new object[1];
            forPostPawnSpawn[0] = chimera;
            typeof(DebugToolsSpawning).GetMethod("PostPawnSpawn", (BindingFlags)60).Invoke(null, forPostPawnSpawn);
        }
    }
}
