using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AlliedEntityDefOf
    {
        public static PawnKindDef AnAl_ChimeraTame;
        public static PawnKindDef AnAl_ChimeraTame_MonolithDisrupted;

        static AlliedEntityDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AlliedEntityDefOf));
        }
    }
}
