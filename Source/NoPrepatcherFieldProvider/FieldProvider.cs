using InterfacesForModularity;
using System.Collections.Generic;
using Verse;

namespace NoPrepatcherFieldProvider
{
    public class FieldProvider : ICustomFieldsProvider
    {
        protected HashSet<RaceProperties> forcedAnimalRaces;
        protected Dictionary<Pawn, int> forcedGraphicsForPawns;

        public readonly string prefix;

        public FieldProvider(string prefix)
        {
            forcedAnimalRaces = new HashSet<RaceProperties>();
            forcedGraphicsForPawns = new Dictionary<Pawn, int>();

            this.prefix = prefix;
        }
        
        public bool IsForcedAnimal(RaceProperties raceProperties)
        {
            return forcedAnimalRaces.Contains(raceProperties);
        }

        public void SetForcedAnimal(RaceProperties raceProperties, bool value)
        {
            if (value)
                forcedAnimalRaces.Add(raceProperties);
            else
                forcedAnimalRaces.Remove(raceProperties);
        }

        public int? GetForcedGraphic(Pawn pawn)
        {
            if (forcedGraphicsForPawns.TryGetValue(pawn, out int value))
                return value;
            else
                return null;
        }

        public void SetForcedGraphic(Pawn pawn, int? value)
        {
            if (value.HasValue)
                forcedGraphicsForPawns[pawn] = value.Value;
            else
                forcedGraphicsForPawns.Remove(pawn);
        }

        public void ExposeData(Pawn pawn)
        {
            int? forcedGraphic = null;
            if (Scribe.mode == LoadSaveMode.Saving)
                forcedGraphic = GetForcedGraphic(pawn);

            Scribe_Values.Look<int?>(ref forcedGraphic, $"{prefix}_forcedGraphic");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
                SetForcedGraphic(pawn, forcedGraphic);
        }
    }
}
