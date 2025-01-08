using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnomalyAllies.Comps;
using AnomalyAllies.DefOfs;
using AnomalyAllies.Misc;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AnomalyAllies.Patches
{
    internal static class DisruptedMonolithPatches
    {
        [HarmonyPatch(typeof(GameComponent_Anomaly), "Notify_LevelChanged")]
        static class DisruptedMonolithCanBeTargeted
        {
            static void Postfix(GameComponent_Anomaly __instance)
            {
                GameComponent_DefChanges defChanges = GameComponent_DefChanges.CurrentInstance;
                ThingDef monolithDef = ThingDefOf.VoidMonolith;
                string[] fieldPath = { "building", "isTargetable" };

                if (__instance.LevelDef == MonolithLevelDefOf.Disrupted)
                    defChanges.AddNewDefChange(monolithDef, fieldPath, true);
                else
                    defChanges.RemoveDefChange(monolithDef, fieldPath);
            }
        }

        [HarmonyPatch(typeof(Building_VoidMonolith))]
        static class DisruptedMonolithDropsFragmentOnHit
        {
            static MethodInfo TargetMethod()
            {
                return typeof(Building_VoidMonolith).Method(nameof(Building_VoidMonolith.PreApplyDamage));
            }
            
            static void Postfix(Building_VoidMonolith __instance, DamageInfo dinfo)
            {
                if (Find.Anomaly.LevelDef != MonolithLevelDefOf.Disrupted)
                    return;

                float adjustedDamage = dinfo.Amount;
                if (dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp && !dinfo.Def.isExplosive)
                    adjustedDamage /= 2;
                else if (dinfo.Def.armorCategory == VanillaDefOf.Heat)
                    adjustedDamage /= 4;

                Map map = __instance.Map;
                if (adjustedDamage >= 10f && Rand.Bool && CellFinder.TryFindRandomCellNear(__instance.Position, map, 2, (iv) => iv.Standable(map), out IntVec3 cell))
                {
                    GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.MonolithFragment), cell, map);
                }
            }
        }
        
        [HarmonyPatch(typeof(CompChimera), nameof(CompChimera.PostPostApplyDamage))]
        static class ChimeraDisruptionEffect
        {
            static void Prefix(CompChimera __instance, out bool __state)
            {
                __state = __instance.Pawn.health.hediffSet.HasHediff(HediffDefOf.RageSpeed);
            }

            static void Postfix(CompChimera __instance, bool __state)
            {
                bool newState = __instance.Pawn.health.hediffSet.TryGetHediff(HediffDefOf.RageSpeed, out Hediff rageSpeed);
                bool rageSpeedAdded = !__state && newState;
                if (rageSpeedAdded && !__instance.Pawn.IsConnectedToHorax())
                    rageSpeed.Severity *= 0.5f;
            }
        }
    }
}
