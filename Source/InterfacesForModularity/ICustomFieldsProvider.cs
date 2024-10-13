using Verse;

namespace InterfacesForModularity
{
    public interface ICustomFieldsProvider
    {
        bool IsForcedAnimal(RaceProperties raceProperties);
        void SetForcedAnimal(RaceProperties raceProperties, bool value);

        int? GetForcedGraphic(Pawn pawn);
        void SetForcedGraphic(Pawn pawn, int? value);

        void ExposeData(Pawn pawn);
    }
}
