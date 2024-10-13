using AnomalyAllies.Misc;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AnomalyAllies.Patches
{
    public static class InhumanizedCanTrainEntitiesPatches
    {
        private static readonly MethodInfo inhumanizedMasterAndEntityAnimalMethod = typeof(InhumanizedCanTrainEntitiesPatches).Method("InhumanizedMasterAndEntityAnimal");

        public static bool InhumanizedOrVoidTouched(Pawn pawn)
        {
            HediffSet hediffSet = pawn.health.hediffSet;
            return hediffSet.HasHediff(HediffDefOf.Inhumanized) || hediffSet.HasHediff(HediffDefOf.VoidTouched);
        }

        public static bool InhumanizedMasterAndEntityAnimal(Pawn master, Pawn animal)
        {
            // AnomalyAlliesMod.Logger.Message($"Master is {master}, animal is {animal}");
            return animal.RaceProps.IsAnomalyEntity && InhumanizedOrVoidTouched(master);
        }

        [HarmonyPatch]
        public static class CanInteract
        {
            static MethodBase TargetMethod()
            {
                return typeof(WorkGiver_InteractAnimal).GetMethod("CanInteractWithAnimal", BindingFlags.Public | BindingFlags.Static);
            }
            
            static void Prefix(Pawn pawn, Pawn animal, ref bool ignoreSkillRequirements)
            {
                if (InhumanizedMasterAndEntityAnimal(pawn, animal))
                    ignoreSkillRequirements = true;
            }
        }

        [HarmonyPatch(typeof(TrainableUtility), nameof(TrainableUtility.CanBeMaster))]
        //[HarmonyDebug]
        public static class CanBeMaster
        {
            private static readonly MethodInfo directRelationExistsMethod = typeof(Pawn_RelationsTracker).Method("DirectRelationExists");

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                codeMatcher.Start();

                codeMatcher.MatchStartForward(CodeMatch.Calls(directRelationExistsMethod));
                codeMatcher.ThrowIfInvalid("Could not find call to Pawn_RelationsTracker.DirectRelationExists");

                codeMatcher.MatchStartForward(CodeMatch.Branches());
                codeMatcher.ThrowIfInvalid("Could not find branch after call to Pawn_RelationsTracker.DirectRelationExists");
                Label oldSkillCheckLabel = (Label)codeMatcher.Instruction.operand;

                codeMatcher.MatchStartForward(new CodeMatch((ci) => ci.labels.Contains(oldSkillCheckLabel)));
                codeMatcher.ThrowIfInvalid("Could not find instruction refered to by the branch after Pawn_RelationsTracker.DirectRelationExists");
                Label newSkillCheckLabel = generator.DefineLabel();
                codeMatcher.Instruction.labels.Remove(oldSkillCheckLabel);
                codeMatcher.Instruction.labels.Add(newSkillCheckLabel);

                List<CodeInstruction> newInstructions = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, inhumanizedMasterAndEntityAnimalMethod),
                    new CodeInstruction(OpCodes.Brfalse_S, newSkillCheckLabel),

                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Ret)
                };
                newInstructions[0].labels.Add(oldSkillCheckLabel);
                codeMatcher.Insert(newInstructions);

                return codeMatcher.InstructionEnumeration();
            }
        }

        [HarmonyPatch(typeof(TameUtility), nameof(TameUtility.ShowDesignationWarnings))]
        //[HarmonyDebug]
        public static class NoWarningIfEntityAnimalAndInumanizedPawnAvailable
        {
            private static readonly FieldInfo animalsSkillDefField = typeof(SkillDefOf).Field("Animals");
            private static readonly MethodInfo racePropsGetter = typeof(Pawn).PropertyGetter("RaceProps");
            private static readonly MethodInfo isAnomalyEntityGetter = typeof(RaceProperties).PropertyGetter("IsAnomalyEntity");
            private static readonly MethodInfo validPawnsHasInhumanizedPawnMethod = typeof(NoWarningIfEntityAnimalAndInumanizedPawnAvailable).Method("ValidPawnsHasInhumanizedPawn");

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                codeMatcher.Start();

                codeMatcher.MatchStartForward(CodeMatch.LoadsField(animalsSkillDefField));
                codeMatcher.ThrowIfInvalid("Could not find instruction that loads field SkillDefOf.Animals");

                codeMatcher.MatchStartBackwards(new CodeMatch(OpCodes.Call), CodeMatch.Branches());
                codeMatcher.ThrowIfInvalid("Could not find location right before skill check");

                codeMatcher.MatchStartBackwards(CodeMatch.LoadsLocal());
                codeMatcher.ThrowIfInvalid("Could not find valid pawns variable");
                CodeInstruction loadValidPawnsVar = codeMatcher.Instruction.Clone();

                codeMatcher.MatchStartForward(CodeMatch.Branches());
                codeMatcher.Advance(1);
                Label skillCheckLabel = generator.DefineLabel();
                codeMatcher.Instruction.labels.Add(skillCheckLabel);

                List<CodeInstruction> newInstructions = new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Callvirt, racePropsGetter),
                    new CodeInstruction(OpCodes.Callvirt, isAnomalyEntityGetter),
                    new CodeInstruction(OpCodes.Brfalse_S, skillCheckLabel),

                    loadValidPawnsVar,
                    new CodeInstruction(OpCodes.Call, validPawnsHasInhumanizedPawnMethod),
                    new CodeInstruction(OpCodes.Brfalse_S, skillCheckLabel),

                    new CodeInstruction(OpCodes.Ret)
                };
                codeMatcher.Insert(newInstructions);

                return codeMatcher.InstructionEnumeration();
            }

            public static bool ValidPawnsHasInhumanizedPawn(IEnumerable<Pawn> validPawns)
            {
                return validPawns.Any(InhumanizedOrVoidTouched);
            }
        }

        public static class AnimalStatsOffset
        {
            private static readonly MethodInfo getStatValueMethod = typeof(StatExtension).Method("GetStatValue");

            public static int minSkillLevel = SkillRecord.MinLevel;
            public static int maxSkillLevel = SkillRecord.MaxLevel;

            public static float NewStatFromSkillOffset(StatDef statDef, float stat, int skillOffset, Pawn pawn)
            {
                SkillNeed skillNeed = statDef.skillNeedFactors.FirstOrDefault();
                if (skillNeed is null)
                    throw new ArgumentException($"Stat {statDef} is not affected by skills");

                int currentSkill = pawn.skills.GetSkill(skillNeed.skill).GetLevel();
                int newSkill = Mathf.Clamp(currentSkill + skillOffset, minSkillLevel, maxSkillLevel);

                float expectedCurrentStat = skillNeed.ValueFor(pawn);
                float extraMultiplier = stat / expectedCurrentStat;

                float newStat;
                if (skillNeed is SkillNeed_BaseBonus baseBonus)
                    newStat = extraMultiplier * baseBonus.ForceInvokeMethod<float>("ValueAtLevel", newSkill);
                else if (skillNeed is SkillNeed_Direct direct)
                    newStat = extraMultiplier * direct.valuesPerLevel[newSkill];
                else
                {
                    SkillRecord skillRecord = pawn.skills.GetSkill(skillNeed.skill);
                    int oldSkill = skillRecord.levelInt;
                    skillRecord.levelInt = newSkill - skillRecord.Aptitude;
                    newStat = skillNeed.ValueFor(pawn);
                    skillRecord.Level = oldSkill;
                }

                return newStat;
            }

            [HarmonyPatch]
            static class TrainStat
            {
                private static readonly MethodInfo tryTrainMethod = typeof(Toils_Interpersonal).Method("TryTrain");
                private static readonly FieldInfo initActionField = typeof(Toil).Field("initAction");
                private const string targetMethodFail = "Could not find delegate method in TryTrain";


                private static readonly FieldInfo actorField = typeof(Toil).Field("actor");
                private static readonly MethodInfo newTrainStatMethod = typeof(TrainStat).Method("NewTrainStat");

                // get the delegate method created by TryTrain
                static MethodBase TargetMethod()
                {
                    var tryTrainBody = PatchProcessor.ReadMethodBody(tryTrainMethod).ToList();

                    int initActionLoadIndex = tryTrainBody.FindIndex((inst) => inst.Value as FieldInfo == initActionField);
                    if (initActionLoadIndex < 0)
                        throw new Exception(targetMethodFail);

                    for (int i = initActionLoadIndex; i >= 0; i--)
                    {
                        var instruction = tryTrainBody[i];
                        if (instruction.Value is MethodInfo target)
                            return target;
                    }
                    throw new Exception(targetMethodFail);
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    CodeMatcher codeMatcher = new CodeMatcher(instructions);
                    codeMatcher.Start();

                    codeMatcher.MatchEndForward(CodeMatch.LoadsField(actorField), CodeMatch.StoresLocal());
                    codeMatcher.ThrowIfInvalid("Could not find variable for trainer");
                    CodeInstruction loadTrainer = codeMatcher.Instruction.Opposite();

                    codeMatcher.MatchEndForward(new CodeMatch(OpCodes.Castclass, typeof(Pawn)), CodeMatch.StoresLocal());
                    codeMatcher.ThrowIfInvalid("Could not find variable for animal");
                    CodeInstruction loadAnimal = codeMatcher.Instruction.Opposite();

                    codeMatcher.MatchStartForward(CodeMatch.Calls(getStatValueMethod));
                    codeMatcher.ThrowIfInvalid("Could not find call to GetStatValue");
                    codeMatcher.Advance(1);

                    List<CodeInstruction> newInstructions = new List<CodeInstruction>()
                    {
                        loadTrainer,
                        loadAnimal,
                        new CodeInstruction(OpCodes.Call, newTrainStatMethod)
                    };
                    codeMatcher.Insert(newInstructions);

                    return codeMatcher.InstructionEnumeration();
                }

                public static float NewTrainStat(float stat, Pawn trainer, Pawn animal)
                {
                    if (!animal.RaceProps.IsAnomalyEntity)
                        return stat;

                    if (trainer.health.hediffSet.HasHediff(HediffDefOf.VoidTouched))
                        return NewStatFromSkillOffset(StatDefOf.TrainAnimalChance, stat, 20, trainer);
                    else if (trainer.health.hediffSet.HasHediff(HediffDefOf.Inhumanized))
                        return NewStatFromSkillOffset(StatDefOf.TrainAnimalChance, stat, 12, trainer);
                    else return stat;
                }
            }

            [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted))]
            static class TameStat
            {
                private static readonly MethodInfo newTameStatMethod = typeof(TameStat).Method("NewTameStat");
                
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    CodeMatcher codeMatcher = new CodeMatcher(instructions);
                    codeMatcher.Start();

                    codeMatcher.MatchStartForward(CodeMatch.Calls(getStatValueMethod));
                    codeMatcher.ThrowIfInvalid("Could not find call to GetStatValue");
                    codeMatcher.Advance(1);

                    List<CodeInstruction> newInstructions = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Call, newTameStatMethod)
                    };
                    codeMatcher.Insert(newInstructions);

                    return codeMatcher.InstructionEnumeration();
                }

                public static float NewTameStat(float stat, Pawn trainer, Pawn animal)
                {
                    if (!animal.RaceProps.IsAnomalyEntity)
                        return stat;

                    if (trainer.health.hediffSet.HasHediff(HediffDefOf.VoidTouched))
                        return NewStatFromSkillOffset(StatDefOf.TameAnimalChance, stat, 20, trainer);
                    else if (trainer.health.hediffSet.HasHediff(HediffDefOf.Inhumanized))
                        return NewStatFromSkillOffset(StatDefOf.TameAnimalChance, stat, 12, trainer);
                    else return stat;
                }
            }
        }
    }
}
