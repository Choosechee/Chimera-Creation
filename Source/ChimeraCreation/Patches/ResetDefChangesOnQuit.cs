using AnomalyAllies.Misc;
using HarmonyLib;
using Verse;

namespace AnomalyAllies.Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Dispose))]
    static class ResetDefChangesOnQuit
    {
        static void Prefix()
        {
            GameComponent_DefChanges.CurrentInstance.Dispose();
        }
    }
}
