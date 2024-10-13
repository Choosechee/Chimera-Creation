using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class VanillaDefOf
    {
        public static ThingDef Bear_Grizzly;

        // public static ThinkTreeDef Animal;
        // public static ThinkTreeDef AnimalConstant;

        public static RecipeDef InstallBionicHeart;

        static VanillaDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(VanillaDefOf));
        }
    }
}
