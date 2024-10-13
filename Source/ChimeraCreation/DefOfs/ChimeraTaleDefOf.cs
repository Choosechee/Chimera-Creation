using RimWorld;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class ChimeraTaleDefOf
    {
        public static TaleDef AnAl_ChimeraBetrayal_Hunger;
        public static TaleDef AnAl_ChimeraBetrayal_Hunger_Multiple;
        public static TaleDef AnAl_ChimeraBetrayal_Hunger_Bonded;
        public static TaleDef AnAl_ChimeraBetrayal_Hunger_Bonded_Multiple;

        public static TaleDef AnAl_ChimeraBetrayal_Slaughter_Temporary;
        public static TaleDef AnAl_ChimeraBetrayal_Slaughter_Permanent;
        public static TaleDef AnAl_ChimeraBetrayal_Slaughter_Bonded_Temporary;
        public static TaleDef AnAl_ChimeraBetrayal_Slaughter_Bonded_Permanent;

        static ChimeraTaleDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(ChimeraTaleDefOf));
        }
    }
}
