using AnomalyAllies.Misc;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AnomalyAllies.ChimeraTame
{
    public class Thought_Memory_ChimeraRemembersBond : Thought_Memory
    {
        public List<Pawn> otherPawnPastSelves = new List<Pawn>();

        protected string cachedLabelCap;
        protected int cachedLabelCapForStageIndex;
        protected List<Pawn> cachedLabelCapForOtherPawnPastSelves;
        public override string LabelCap
        {
            get
            {
                if (cachedLabelCap is null || cachedLabelCapForStageIndex != CurStageIndex || !(cachedLabelCapForOtherPawnPastSelves.SequenceEqual(otherPawnPastSelves)))
                {
                    if (otherPawnPastSelves.Count > 0)
                    {
                        cachedLabelCap = CurStage.label.Formatted(BondBreakHelper.CreateBondedPawnsString(otherPawnPastSelves)).CapitalizeFirst();
                        if (def.Worker is not null)
                            cachedLabelCap = def.Worker.PostProcessLabel(pawn, cachedLabelCap);
                    }
                    else
                        cachedLabelCap = base.LabelCap;

                    cachedLabelCapForStageIndex = CurStageIndex;
                    cachedLabelCapForOtherPawnPastSelves = otherPawnPastSelves.ListFullCopy();
                }
                
                return cachedLabelCap;
            }
        }
    }
}
