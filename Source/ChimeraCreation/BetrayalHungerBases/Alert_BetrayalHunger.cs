using RimWorld;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace AnomalyAllies.BetrayalHungerBases
{
    public abstract class Alert_BetrayalHunger : Alert_Critical
    {
        protected List<Pawn> betrayalHungerPawns = new List<Pawn>();
        protected List<Pawn> BetrayalHungerPawns
        {
            get
            {
                betrayalHungerPawns.Clear();

                List<Pawn> pawnsInPlayerFaction = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction;
                foreach (Pawn pawn in pawnsInPlayerFaction)
                {
                    if (AtRiskOfBetrayalHunger(pawn))
                        betrayalHungerPawns.Add(pawn);
                }

                return betrayalHungerPawns;
            }
        }

        protected StringBuilder stringBuilderForConvenience = new StringBuilder();

        public Alert_BetrayalHunger()
        {
            defaultPriority = AlertPriority.High;
            requireAnomaly = true;
        }

        protected abstract bool AtRiskOfBetrayalHunger(Pawn pawn);
        protected abstract string GetExplanationString(string listOfBetrayalHungerPawns);

        public override TaggedString GetExplanation()
        {
            stringBuilderForConvenience.Clear();
            foreach (Pawn pawn in betrayalHungerPawns)
                stringBuilderForConvenience.AppendLine("  - " + pawn.NameShortColored.Resolve());

            return GetExplanationString(stringBuilderForConvenience.ToString().TrimEndNewlines());
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(BetrayalHungerPawns);
    }
    }
}
