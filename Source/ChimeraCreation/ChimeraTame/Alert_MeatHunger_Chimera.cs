using Verse;

namespace AnomalyAllies.ChimeraTame
{
    public class Alert_MeatHunger_Chimera : BetrayalHungerBases.Alert_BetrayalHunger
    {
        public Alert_MeatHunger_Chimera() : base()
        {
            defaultLabel = "AnAl_AlertMeatHungerChimera".Translate();
        }
        
        protected override bool AtRiskOfBetrayalHunger(Pawn pawn)
        {
            return pawn.health.hediffSet.HasHediff<Hediff_MeatHunger_Chimera>(mustBeVisible: false);
        }

        protected override string GetExplanationString(string listOfBetrayalHungerPawns)
        {
            string explanatiionStringPart = 
                (!AnomalyAlliesMod.Settings.chimeraIsNormalCarnivore)
                ? "AnAl_AlertMeatHungerChimera_Desc" : "AnAl_AlertMeatHungerChimera_Desc_ChimeraIsNormalCarnivore";
            explanatiionStringPart = explanatiionStringPart.Translate(listOfBetrayalHungerPawns.Named("AFFECTEDPAWNS"));
            return explanatiionStringPart + "AnAl_AlertMeatHungerChimera_Desc_Appended".Translate();
        }
    }
}
