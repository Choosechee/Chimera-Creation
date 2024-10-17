using AnomalyAllies.ChimeraTame;
using AnomalyAllies.DefOfs;
using HarmonyLib;
using InterfacesForModularity;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AnomalyAllies
{
    public class AnomalyAlliesMod : Mod
    {
        public readonly string modName;
        public readonly string modPrefix = "AnAl";

        private AnomalyAlliesSettings settings;
        private ModLogger logger;
        private ICustomFieldsProvider fieldProvider;
        private Harmony harmony;

        private static AnomalyAlliesMod modInstance;
        public static AnomalyAlliesSettings Settings => modInstance.settings;
        internal static ModLogger Logger => modInstance.logger;
        public static ICustomFieldsProvider FieldProvider => modInstance.fieldProvider;
        internal static Harmony Harmony => modInstance.harmony;
        
        public AnomalyAlliesMod(ModContentPack content) : base(content)
        {
            modName = content.Name;

            settings = GetSettings<AnomalyAlliesSettings>();
            logger = new ModLogger(modName);

            Assembly fieldProviderAssembly;
            List<Assembly> loadedAssemblies = Content.assemblies.loadedAssemblies;
            if (ModLister.GetActiveModWithIdentifier("zetrith.prepatcher", ignorePostfix: true) is not null)
            {
                logger.Message("Found Prepatcher. Accessing PrepatcherFieldProvider.dll");
                fieldProviderAssembly = loadedAssemblies.Find((Assembly a) => a.GetName().Name == "PrepatcherFieldProvider");
            }
            else
            {
                logger.Message("Could not find Prepatcher. Accessing NoPrepatcherFieldProvider.dll");
                fieldProviderAssembly = loadedAssemblies.Find((Assembly a) => a.GetName().Name == "NoPrepatcherFieldProvider");
            }
            Type fieldProviderType = fieldProviderAssembly.ExportedTypes.FirstOrDefault();
            if (fieldProviderType.Name.Contains("Dictionary"))
                fieldProviderType = fieldProviderAssembly.ExportedTypes.ElementAt(1);
            fieldProvider = (ICustomFieldsProvider)Activator.CreateInstance(fieldProviderType, "AnAl");

            modInstance = this;

            logger.Message("Creating a Harmony instance and applying patches");
            harmony = new Harmony(content.PackageId);
            harmony.PatchAllUncategorized();

            /*var patchedMethods = harmony.GetPatchedMethods();
            LogPatchedMethods(patchedMethods);*/
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard settingsList = new Listing_Standard();
            settingsList.Begin(inRect);

            settingsList.CheckboxLabeled("AnAl_EntitiesCanBetray_Label".Translate(), ref settings.entitiesCanBetray, "AnAl_EntitiesCanBetray_Tooltip".Translate());
            if (settings.entitiesCanBetray)
                settingsList.CheckboxLabeled("AnAl_BetrayalIsPermanent_Label".Translate(), ref settings.betrayalIsPermanent, "AnAl_BetrayalIsPermanent_Tooltip".Translate());
            
            settingsList.GapLine();
            settingsList.CheckboxLabeled("AnAl_ChimeraIsNormalCarnivore_Label".Translate(), ref settings.chimeraIsNormalCarnivore, "AnAl_ChimeraIsNormalCarnivore_Tooltip".Translate());

            settingsList.Label("AnAl_ChimeraMeatRequirementOffset_Label".Translate(), tooltip: "AnAl_ChimeraMeatRequirementOffset_Tooltip".Translate(AnAl_PsychicRitualDefOf.AnAl_CreateChimera.MeatYieldNeededForChimeraWithOffset.Named("MEATREQUIRED")));
            string editBuffer = settings.chimeraMeatRequirementOffset.ToString();
            settingsList.TextFieldNumeric(ref settings.chimeraMeatRequirementOffset, ref editBuffer, min: float.NegativeInfinity, max: float.PositiveInfinity);

            settingsList.CheckboxLabeled("AnAl_WildChimerasAreTameable_Label".Translate(), ref settings.wildChimerasAreTameable, "AnAl_WildChimerasAreTameable_Tooltip".Translate());
            settingsList.Label("AnAl_NotRecommendedWarning".Translate().Colorize(Color.yellow));

            float buttonWidth = inRect.width / 4f;
            float buttonHeight = 40f;
            float rectBottomHeight = 40f + Window.CloseButSize.y;
            Rect buttonRect = new Rect(inRect.x + (inRect.width - buttonWidth) / 2, inRect.yMax - (rectBottomHeight), buttonWidth, buttonHeight);
            if (Widgets.ButtonText(buttonRect, "AnAl_ResetSettings".Translate()))
            {
                FieldInfo[] settingsFields = settings.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
                AnomalyAlliesSettings defaultSettings = new AnomalyAlliesSettings();

                foreach (FieldInfo field in settingsFields)
                {
                    object defaultValue = field.GetValue(defaultSettings);
                    field.SetValue(settings, defaultValue);
                }
            }

            settingsList.End();

            base.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();

            if (settings.chimeraIsNormalCarnivore)
            {
                AlliedEntityDefOf.AnAl_ChimeraTame.RaceProps.foodType =
                    (FoodTypeFlags.CarnivoreAnimal | FoodTypeFlags.OvivoreAnimal);
                AnAl_HediffDefOf.AnAl_MeatHungerChimera.description = "AnAl_MeatHungerChimera_Description_ChimeraIsNormalCarnivore".Translate();
            }
            else
            {
                AlliedEntityDefOf.AnAl_ChimeraTame.RaceProps.foodType = Setup.originalChimeraTameDiet;
                AnAl_HediffDefOf.AnAl_MeatHungerChimera.description = Setup.originalMeatHungerChimeraDescription;
            }

            AnAl_HediffDefOf.AnAl_MeatHungerChimera.GetType().GetField("descriptionCached", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(AnAl_HediffDefOf.AnAl_MeatHungerChimera, null);
            AnAl_PsychicRitualDefOf.AnAl_CreateChimera.InvalidateTimeAndOfferingLabelCache();
        }

        public override string SettingsCategory()
        {
            return modName;
        }

        public class AnomalyAlliesSettings : ModSettings
        {
            public bool entitiesCanBetray = true;
            public bool betrayalIsPermanent = true;

            public bool chimeraIsNormalCarnivore = false;
            public int chimeraMeatRequirementOffset = 0;
            public bool wildChimerasAreTameable = false; // heavily unrecommended

            public override void ExposeData()
            {
                base.ExposeData();

                Scribe_Values.Look(ref entitiesCanBetray, "entitiesCanBetray");
                Scribe_Values.Look(ref betrayalIsPermanent, "betrayalIsPermanent");

                Scribe_Values.Look(ref chimeraIsNormalCarnivore, "chimeraIsNormalCarnivore");
                Scribe_Values.Look(ref chimeraMeatRequirementOffset, "chimeraMeatRequirementOffset");
                Scribe_Values.Look(ref wildChimerasAreTameable, "wildChimerasAreTameable");
            }
        }

        public class ModLogger
        {
            protected string modName;

            public ModLogger(string modName)
            {
                this.modName = modName;
            }
            
            public void Message(string text)
            {
                Log.Message($"[{modName}]: {text}");
            }

            public void Message(object obj)
            {
                Log.Message($"[{modName}]: {obj}");
            }

            public void Warning(string text)
            {
                Log.Warning($"[{modName}]: {text}");
            }

            public void Warning(object obj)
            {
                Log.Warning($"[{modName}]: {obj}");
            }

            public void WarningOnce(string text, int key)
            {
                Log.WarningOnce($"[{modName}]: {text}", key);
            }

            public void Error(string text)
            {
                Log.Error($"[{modName}]: {text}");
            }

            public void Error(object obj)
            {
                Log.Error($"[{modName}]: {obj}");
            }

            public void ErrorOnce(string text, int key)
            {
                Log.ErrorOnce($"[{modName}]: {text}", key);
            }
        }

        void LogPatchedMethods(IEnumerable<MethodBase> patchedMethods)
        {
            foreach (var method in patchedMethods)
            {
                Logger.Message($"Patches on {method}:");
                HarmonyLib.Patches patches = Harmony.GetPatchInfo(method);

                LogPatches(patches.Prefixes);
                LogPatches(patches.Postfixes);
                LogPatches(patches.Transpilers);
                LogPatches(patches.Finalizers);
            }
        }

        void LogPatches(ReadOnlyCollection<Patch> patches)
        {
            foreach (Patch patch in patches)
            {
                if (patch.owner == Content.PackageId)
                    Logger.Message(patch.PatchMethod);
            }
        }

        internal static void LogPatchedMethodsStatic(IEnumerable<MethodBase> patchedMethods)
        {
            modInstance.LogPatchedMethods(patchedMethods);
        }
    }
}
