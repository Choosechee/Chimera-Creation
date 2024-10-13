using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AlliedEntityDefOf
    {
        public static PawnKindDef AnAl_ChimeraTame;

        static AlliedEntityDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AlliedEntityDefOf));
        }
    }
}
