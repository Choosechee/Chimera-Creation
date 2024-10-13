using AnomalyAllies.DefModExtensions;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace AnomalyAllies.Patches
{
    // it just keeps causing a stack overflow error and I don't know why
    //[HarmonyPatch]
    //[HarmonyPatchCategory(PatchAfterStaticConstructors.category)]
    //public static class DumbStupidWildnessTraversalPatch
    //{
    //    private static Module verseModule = typeof(RaceProperties).Module;
    //    private static Module thisModule = typeof(DumbStupidWildnessTraversalPatch).Module;
    //    private static FieldInfo wildnessField = typeof(RaceProperties).Field("wildness");
    //    private static MethodInfo racePropsGetter = typeof(Pawn).PropertyGetter("RaceProps");
    //    //private static Type pawnType = typeof(Pawn);
    //    private static MethodInfo newWildnessMethod = typeof(DumbStupidWildnessTraversalPatch).GetMethod("NewWildness", BindingFlags.Public | BindingFlags.Static);

    //    public static long timeSpentPatching = 0;
        
    //    static IEnumerable<MethodBase> TargetMethods()
    //    {
    //        Stopwatch stopwatch = Stopwatch.StartNew();

    //        List<Assembly> allAssembliesExceptThis = new List<Assembly>(AccessTools.AllAssemblies());
    //        if (!allAssembliesExceptThis.Remove(Assembly.GetExecutingAssembly()))
    //            throw new Exception("This assembly was not correctly removed");

    //        List<Type> supportedTypes = new List<Type>();
    //        AnomalyAlliesMod.Logger.Message("Assembly foreach");
    //        foreach (Assembly assembly in allAssembliesExceptThis)
    //        {
    //            supportedTypes.AddRange(assembly.GetTypes().Where(NotUnsupportedType));
    //        }

    //        List<MethodBase> supportedMethods = new List<MethodBase>();
    //        AnomalyAlliesMod.Logger.Message("Type foreach");
    //        foreach (Type type in supportedTypes)
    //        {
    //            supportedMethods.AddRange(type.GetConstructors(AccessTools.all).Where(NotUnsupportedMethod));
    //            supportedMethods.AddRange(type.GetMethods(AccessTools.all).Where(NotUnsupportedMethod));
    //        }

    //        AnomalyAlliesMod.Logger.Message("Method foreach");
    //        foreach (MethodBase method in supportedMethods)
    //        {
    //            IEnumerable<KeyValuePair<OpCode, object>> instructions = PatchProcessor.ReadMethodBody(method);

    //            if (instructions.Any(IsFieldForWildness) && instructions.Any(IsRacePropsGetter))
    //                yield return method;
    //        }

    //        stopwatch.Stop();
    //        AnomalyAlliesMod.Logger.Message($"WildnessPatch took {stopwatch.ElapsedMilliseconds} milliseconds to get all the methods that should be patched");
    //    }

    //    static bool NotUnsupportedType(Type type)
    //    {
    //        return !type.IsGenericType && (type.Namespace is null || !type.Namespace.StartsWith("System"));
    //    }

    //    static bool NotUnsupportedMethod(MethodBase method)
    //    {
    //        return method.HasMethodBody() && !method.IsGenericMethod;
    //    }

    //    static bool IsFieldForWildness(KeyValuePair<OpCode, object> instruction)
    //    {
    //        return instruction.Value is FieldInfo field && field == wildnessField;
    //    }

    //    static bool IsRacePropsGetter(KeyValuePair<OpCode, object> instruction)
    //    {
    //        return instruction.Value is MethodInfo method && method == racePropsGetter;
    //    }

    //    /*static bool IsNewWildnessMethod(byte instruction)
    //    {
    //        try
    //        {
    //            return thisModule.ResolveMethod(instruction) == newWildnessMethod;
    //        }
    //        catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
    //        {
    //            return false;
    //        }
    //    }*/

    //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //    {
    //        Stopwatch stopwatch = Stopwatch.StartNew();

    //        HashSet<OpCode> codesThatCouldGetAPawn = new HashSet<OpCode>()
    //        {
    //            OpCodes.Ldarg,
    //            OpCodes.Ldarg_0,
    //            OpCodes.Ldarg_1,
    //            OpCodes.Ldarg_2,
    //            OpCodes.Ldarg_3,
    //            OpCodes.Ldarg_S,
    //            OpCodes.Ldfld,
    //            OpCodes.Ldloc,
    //            OpCodes.Ldloc_0,
    //            OpCodes.Ldloc_1,
    //            OpCodes.Ldloc_2,
    //            OpCodes.Ldloc_3,
    //            OpCodes.Ldloc_S,
    //        };
            
    //        CodeMatcher codeMatcher = new CodeMatcher(instructions);
    //        codeMatcher.Start();

    //        codeMatcher.MatchStartForward(CodeMatch.LoadsField(wildnessField));
    //        while (codeMatcher.IsValid)
    //        {
    //            int wildnessPos = codeMatcher.Pos;
    //            CodeInstruction loadWildnessField = codeMatcher.Instruction.Clone();

    //            codeMatcher.MatchStartBackwards(CodeMatch.Calls(racePropsGetter));

    //            codeMatcher.MatchStartBackwards(CodeMatch.WithOpcodes(codesThatCouldGetAPawn));
    //            if (codeMatcher.IsInvalid)
    //            {
    //                AnomalyAlliesMod.Logger.Error("Could not find an instruction that gets a pawn. Choosechee (me) might have forgotten a way a pawn could be loaded in. Please contact him about this.");

    //                codeMatcher.Advance(wildnessPos - codeMatcher.Pos);
    //                codeMatcher.MatchStartForward(CodeMatch.LoadsField(wildnessField));
    //                continue;
    //            }
    //            CodeInstruction getAPawn = codeMatcher.Instruction.Clone();
                
    //            codeMatcher.Advance(wildnessPos - codeMatcher.Pos);
    //            codeMatcher.Advance(1);
    //            codeMatcher.Insert(getAPawn, new CodeInstruction(OpCodes.Call, newWildnessMethod));

    //            codeMatcher.MatchStartForward(CodeMatch.LoadsField(wildnessField));
    //        }

    //        stopwatch.Stop();
    //        timeSpentPatching += stopwatch.ElapsedMilliseconds;
    //        return codeMatcher.InstructionEnumeration();
    //    }

    //    public static float NewWildness(Pawn pawn, float wildness)
    //    {
    //        float wildnessChange = 0;
    //        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
    //        {
    //            HediffDef hediffDef = hediff.def;
    //            if (hediffDef.HasModExtension<ChangeWildness>())
    //            {
    //                wildnessChange += hediffDef.GetModExtension<ChangeWildness>().changeBy;
    //                wildnessChange = Math.Max(wildnessChange, 0f);
    //            }
    //        }

    //        return wildness + wildnessChange;
    //    }
    //}

    //[HarmonyPatch(typeof(StatsReportUtility))]
    //static class ShowAdjustedWildness
    //{
    //    static MethodBase TargetMethod()
    //    {
    //        return typeof(StatsReportUtility).Method("StatsToDraw", new Type[] { typeof(Thing) });
    //    }

    //    static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> __result, Thing thing)
    //    {
    //        if (thing is Pawn pawn)
    //        {
    //            List<StatDrawEntry> statDrawEntries = new List<StatDrawEntry>(__result);
    //            int wildnessIndex = statDrawEntries.FindIndex((StatDrawEntry sdw) => sdw.DisplayPriorityWithinCategory == StatDisplayOrder.Race_Wildness);
    //            if (wildnessIndex > -1)
    //            {
    //                float wildness = 0f;
    //                if (pawn.RaceProps.wildness > 0)
    //                    wildness = pawn.RaceProps.wildness;
    //                else if (pawn.RaceProps.Humanlike)
    //                    wildness = 0.75f;

    //                wildness = DumbStupidWildnessTraversalPatch.NewWildness(pawn, wildness);
    //                statDrawEntries[wildnessIndex] = new StatDrawEntry(StatCategoryDefOf.BasicsPawn, "Wildness".Translate(), wildness.ToStringPercent(), TrainableUtility.GetWildnessExplanation(pawn.def), StatDisplayOrder.Race_Wildness);
    //            }

    //            return statDrawEntries.AsEnumerable();
    //        }
    //        else
    //            return __result;
    //    }
    //}
}
