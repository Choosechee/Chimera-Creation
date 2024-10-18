using Verse;

namespace InterfacesForModularity
{
    public interface ICustomFieldsProvider
    {
        ref bool EntityAnimal(RaceProperties raceProperties);

        ref int? ForcedGraphic(Pawn pawn);

        void ExposeData(Pawn pawn);
    }
}
