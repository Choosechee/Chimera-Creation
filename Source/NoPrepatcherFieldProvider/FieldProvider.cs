using InterfacesForModularity;
using System.Collections.Generic;
using Verse;

namespace NoPrepatcherFieldProvider
{
    public class FieldProvider : ICustomFieldsProvider
    {
        protected RefDictionary<RaceProperties, bool> forcedAnimalRaces;
        protected RefDictionary<Pawn, int?> forcedGraphicsForPawns;

        public readonly string prefix;

        public FieldProvider(string prefix)
        {
            forcedAnimalRaces = new RefDictionary<RaceProperties, bool>();
            forcedGraphicsForPawns = new RefDictionary<Pawn, int?>();

            this.prefix = prefix;
        }

        public ref bool ForcedAnimal(RaceProperties raceProperties)
        {
            return ref forcedAnimalRaces[raceProperties];
        }

        public ref int? ForcedGraphic(Pawn pawn)
        {
            return ref forcedGraphicsForPawns[pawn];
        }

        public void ExposeData(Pawn pawn)
        {
            Scribe_Values.Look(ref ForcedGraphic(pawn), $"{prefix}_forcedGraphic");
        }
    }
}
