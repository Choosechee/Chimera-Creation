using RimWorld;
using System.Collections.Generic;
using Verse;

namespace AnomalyAllies.Comps
{
    public class CompBondsFromPastLife : ThingComp
    {
        public record BondFromPastLife : IExposable
        {
            private Pawn previousSelf;
            private Pawn pawnBondedTo;

            public Pawn PreviousSelf
            {
                get => previousSelf; init => previousSelf = value;
            }
            public Pawn PawnBondedTo
            {
                get => pawnBondedTo; init => pawnBondedTo = value;
            }

            public BondFromPastLife(Pawn previousSelf, Pawn pawnBondedTo)
            {
                this.previousSelf = previousSelf;
                this.pawnBondedTo = pawnBondedTo;
            }

            public void ExposeData()
            {
                Scribe_References.Look(ref previousSelf, "AnAl_previousSelf", true);
                Scribe_References.Look(ref pawnBondedTo, "AnAl_pawnBondedTo");
            }
        }
        
        private List<BondFromPastLife> bonds = new List<BondFromPastLife>();
        public List<BondFromPastLife> Bonds => bonds;

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Collections.Look(ref bonds, "AnAl_bondsFromPastLife", LookMode.Deep);
        }
    }
}
