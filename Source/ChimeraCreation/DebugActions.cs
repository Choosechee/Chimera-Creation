using AnomalyAllies.ChimeraTame;
using AnomalyAllies.DefOfs;
using LudeonTK;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AnomalyAllies
{
    public static class DebugActions
    {
        [DebugAction("Anomaly", "Spawn tame chimera", requiresAnomaly = true, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void SpawnTameChimera()
        {
            PawnKindDef chimeraDef = Find.Anomaly.LevelDef != MonolithLevelDefOf.Disrupted ? AlliedEntityDefOf.AnAl_ChimeraTame : AlliedEntityDefOf.AnAl_ChimeraTame_MonolithDisrupted;
            Pawn chimera = PawnGenerator.GeneratePawn(chimeraDef, Faction.OfPlayer);
            // ChimeraCreationMod.myLogger.Message(chimera.def.defName);

            GenSpawn.Spawn(chimera, UI.MouseCell(), Find.CurrentMap);
            typeof(DebugToolsSpawning).ForceInvokeStaticMethod("PostPawnSpawn", chimera);
        }

        [TweakValue("AnomalyAllies")]
        static float bodySizeForFleshbeastKnapsackDebug = 3f;

        [DebugAction("Anomaly", "Spawn fleshbeast knapsack for body size", requiresAnomaly = true, actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void SpawnFleshbeastKnapsack()
        {
            List<PawnKindDef> fleshbeasts = PsychicRitualToil_CreateChimera.FleshbeastsForBodySize(bodySizeForFleshbeastKnapsackDebug);

            foreach (PawnKindDef fleshbeastDef in fleshbeasts)
            {
                Pawn fleshbeast = PawnGenerator.GeneratePawn(fleshbeastDef, Faction.OfEntities);
                GenSpawn.Spawn(fleshbeast, UI.MouseCell(), Find.CurrentMap);
                typeof(DebugToolsSpawning).ForceInvokeStaticMethod("PostPawnSpawn", fleshbeast);
            }
        }
    }
}
