using RimWorld;
using Verse;

namespace AnomalyAllies.DefOfs
{
    [DefOf]
    public static class AnAl_RecipeDefOf
    {
        public static RecipeDef AnAl_InstallAdrenalHeartChimera;

        static AnAl_RecipeDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(AnAl_RecipeDefOf));
        }
    }
}
