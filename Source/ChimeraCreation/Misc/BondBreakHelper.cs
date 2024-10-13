using AnomalyAllies.DefOfs;
using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace AnomalyAllies.Misc
{
    public static class BondBreakHelper
    {
        public static string CreateBondedPawnsString(List<Pawn> pawns)
        {
            StringBuilder bondedPawnsString = new StringBuilder();
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn bondedPawn = pawns[i];
                if (i + 1 < pawns.Count)
                    bondedPawnsString.Append("AnAl_BondedPawn_Separator".Translate(bondedPawn.Named("BONDEDPAWN")).Resolve());
                else
                    bondedPawnsString.Append("AnAl_BondedPawn_Separator_Final".Translate(bondedPawn.Named("BONDEDPAWN")).Resolve());
            }

            return bondedPawnsString.ToString();
        }

        public static void TryGiveEntityBetrayalThought(Pawn pawn, Pawn betrayer)
        {
            MemoryThoughtHandler pawnMemories = pawn.needs.mood.thoughts.memories;
            TraitSet pawnTraits = pawn.story.traits;
            HediffSet pawnHediffs = pawn.health.hediffSet;
            if (pawnTraits.HasTrait(TraitDefOf.VoidFascination))
            {
                if (!(pawnTraits.HasTrait(TraitDefOf.Psychopath) || pawnHediffs.HasHediff(HediffDefOf.Inhumanized)))
                    pawnMemories.TryGainMemory(AnAl_ThoughtDefOf.AnAl_BondedAnimalEntityBetrayedVoidFascinated, betrayer);
                else
                    pawnMemories.TryGainMemory(AnAl_ThoughtDefOf.AnAl_BondedAnimalEntityBetrayedVoidFascinatedPsychopath, betrayer);
            }
            else if (pawnHediffs.HasHediff(HediffDefOf.Inhumanized))
                pawnMemories.TryGainMemory(AnAl_ThoughtDefOf.AnAl_BondedAnimalEntityBetrayedInhumanized, betrayer);
            else
                pawnMemories.TryGainMemory(AnAl_ThoughtDefOf.AnAl_BondedAnimalEntityBetrayed, betrayer);
        }
    }
}
