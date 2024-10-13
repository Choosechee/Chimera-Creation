using AnomalyAllies.ChimeraTame;
using RimWorld;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AnAl_PsychicRitualDefOf
    {
        public static PsychicRitualDef_CreateChimera AnAl_CreateChimera;
        public static PsychicRitualRoleDef_CreateChimeraTarget AnAl_CreateChimeraTarget;

        static AnAl_PsychicRitualDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnAl_PsychicRitualDefOf));
        }
    }
}
