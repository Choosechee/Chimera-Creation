using InterfacesForModularity;
using Prepatcher;
using Verse;

namespace PrepatcherFieldProvider
{
    public class FieldProvider : ICustomFieldsProvider
    {
        public readonly string prefix;

        public FieldProvider(string prefix)
        {
            this.prefix = prefix;
        }

        public bool IsForcedAnimal(RaceProperties raceProperties)
        {
            return raceProperties.ForcedAnimalField();
        }

        public void SetForcedAnimal(RaceProperties raceProperties, bool value)
        {
            raceProperties.ForcedAnimalField() = value;
        }

        public int? GetForcedGraphic(Pawn pawn)
        {
            return pawn.ForcedGraphicField();
        }

        public void SetForcedGraphic(Pawn pawn, int? value)
        {
            pawn.ForcedGraphicField() = value;
        }

        public void ExposeData(Pawn pawn)
        {
            Scribe_Values.Look<int?>(ref pawn.ForcedGraphicField(), $"{prefix}_forcedGraphic");
        }
    }

    internal static class ForPrepatcherFieldProvider
    {
        [PrepatcherField]
        [Prepatcher.DefaultValue(false)]
        internal static extern ref bool ForcedAnimalField(this RaceProperties raceProperties);

        [PrepatcherField]
        [Prepatcher.DefaultValue(null)]
        internal static extern ref int? ForcedGraphicField(this Pawn pawn);
    }
}
