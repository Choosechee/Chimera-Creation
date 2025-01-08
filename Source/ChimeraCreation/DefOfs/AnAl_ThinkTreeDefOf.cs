using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AnAl_ThinkTreeDefOf
    {
        public static ThinkTreeDef AnAl_ChimeraTame;
        public static ThinkTreeDef AnAl_ChimeraTameConstant;

        static AnAl_ThinkTreeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnAl_ThinkTreeDefOf));
        }
    }
}
