using RimWorld;
using Verse.AI;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AnAl_DutyDefOf
    {
        public static DutyDef AnAl_DeliverAnimalToPsychicRitualCell;

        static AnAl_DutyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnAl_DutyDefOf));
        }
    }
}
