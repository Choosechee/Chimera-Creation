using RimWorld;
using System;
using System.Collections.Generic;
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

        public static bool IsInitialized => AnAl_ChimeraTame is not null;
    }

    public static class AlliedEntityGroups
    {
        public static readonly List<PawnKindDef> chimeraTameDefs;

        static AlliedEntityGroups()
        {
            if (!AlliedEntityDefOf.IsInitialized)
                throw new Exception($"{nameof(AlliedEntityGroups)} was used before {nameof(AlliedEntityDefOf)} was initialized.");

            chimeraTameDefs = new List<PawnKindDef>(2)
            {
                AlliedEntityDefOf.AnAl_ChimeraTame,
                AlliedEntityDefOf.AnAl_ChimeraTame_MonolithDisrupted
            };
        }
    }
}
