using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AnAl_HediffDefOf
    {
        public static HediffDef AnAl_MeatHungerChimera;
        public static HediffDef AnAl_AdrenalHeartChimera;

        static AnAl_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnAl_HediffDefOf));
        }
    }
}
