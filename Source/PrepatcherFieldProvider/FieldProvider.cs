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

        public ref bool EntityAnimal(RaceProperties raceProperties)
        {
            return ref raceProperties.ForcedAnimalField();
        }

        public ref int? ForcedGraphic(Pawn pawn)
        {
            return ref pawn.ForcedGraphicField();
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
