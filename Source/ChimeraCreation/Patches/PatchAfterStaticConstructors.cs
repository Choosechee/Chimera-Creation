using HarmonyLib;
using Verse;

namespace AnomalyAllies.Patches
{
    // not needed yet
    /*[HarmonyPatch(typeof(StaticConstructorOnStartupUtility), nameof(StaticConstructorOnStartupUtility.CallAll))]
    public static class PatchAfterStaticConstructors
    {
        public static bool patched = false;
        public const string category = "after_static_constructors";

        static void Postfix()
        {
            if (!patched)
            {
                AnomalyAlliesMod.Harmony.PatchCategory(category);
                patched = true;
            }
        }
    }*/
}
