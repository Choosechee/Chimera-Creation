using AnomalyAllies.Comps;
using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AnomalyAllies.Patches
{
    // thank you to AmCh for teaching me how to do this
    [HarmonyPatch]
    static class RevengeOnSlaughterPatch
    {
        private static MethodInfo makeToils = AccessTools.Method(typeof(JobDriver_Slaughter), "MakeNewToils");
        private static MethodInfo makeToilsMoveNext = AccessTools.EnumeratorMoveNext(makeToils);
        private static MethodInfo doExecutionByCut = AccessTools.Method(typeof(ExecutionUtility), nameof(ExecutionUtility.DoExecutionByCut));
        private static MethodInfo makeToilsDelegate = GetInternalMethods(makeToilsMoveNext).FirstOrDefault(IsExecutionMethod);

        static MethodBase TargetMethod()
        {
            return makeToilsDelegate;
        }

        static bool Prefix(JobDriver_Slaughter __instance)
        {
            Pawn victim = __instance.ForceGetProperty<Pawn>("Victim");
            Pawn executioner = __instance.pawn;

            if (victim is null || executioner is null)
                return true;

            CompRevengeOnSlaughter victimComp;
            if (victim.TryGetComp<CompRevengeOnSlaughter>(out victimComp))
            {
                bool tookRevenge = victimComp.TryRevenge(executioner);
                return !tookRevenge;
            }
            else
                return true;
        }
        
        // Get children methods which are loaded by the given method via Ldftn
        private static IEnumerable<MethodInfo> GetInternalMethods(MethodBase method)
        {
            return PatchProcessor.ReadMethodBody(method)
                .Where(x => x.Key == OpCodes.Ldftn)
                .Select(x => x.Value)
                .OfType<MethodInfo>();
        }

        private static bool IsExecutionMethod(MethodInfo method)
        {
            IEnumerable<KeyValuePair<OpCode, object>> methodBody = PatchProcessor.ReadMethodBody(method);
            return methodBody.Any(x => x.Value != null && x.Value.Equals(doExecutionByCut));
        }
    }
}
