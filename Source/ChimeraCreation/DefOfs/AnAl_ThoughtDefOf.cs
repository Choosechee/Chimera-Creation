using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AnAl_ThoughtDefOf
    {
        public static ThoughtDef AnAl_BondedAnimalEntityBetrayed;
        public static ThoughtDef AnAl_BondedAnimalEntityBetrayedInhumanized;
        public static ThoughtDef AnAl_BondedAnimalEntityBetrayedVoidFascinated;
        public static ThoughtDef AnAl_BondedAnimalEntityBetrayedVoidFascinatedPsychopath;

        static AnAl_ThoughtDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnAl_ThoughtDefOf));
        }
    }
}
