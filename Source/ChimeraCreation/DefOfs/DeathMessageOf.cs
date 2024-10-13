using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class DeathMessageOf
    {
        public static DamageDef AnAl_MorphedIntoChimera;
        public static DamageDef AnAl_MorphedIntoFleshbeast;
        public static DamageDef AnAl_MorphedIntoMeat;

        static DeathMessageOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DeathMessageOf));
        }
    }
}
