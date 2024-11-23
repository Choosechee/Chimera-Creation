using HarmonyLib;
using RimWorld;
using Verse;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using InterfacesForModularity;
using AnomalyAllies.Comps;
using AnomalyAllies.DefOfs;
using AnomalyAllies.ChimeraTame;

namespace AnomalyAllies.Patches
{
    [HarmonyPatch(typeof(RelationsUtility), nameof(RelationsUtility.TryDevelopBondRelation))]
    //[HarmonyDebug]
    static class EntityAnimalBonds
    {
        private static MethodInfo inhumanizedMethod = typeof(AnomalyUtility).Method(nameof(AnomalyUtility.Inhumanized));
        private static MethodInfo racePropsGetter = typeof(Pawn).PropertyGetter(nameof(Pawn.RaceProps));
        private static MethodInfo fieldProviderGetter = typeof(AnomalyAlliesMod).PropertyGetter(nameof(AnomalyAlliesMod.FieldProvider));
        private static MethodInfo entityAnimalMethod = typeof(ICustomFieldsProvider).Method(nameof(ICustomFieldsProvider.EntityAnimal));

        static void Prefix(Pawn humanlike, Pawn animal, ref float baseChance, ref List<CompBondsFromPastLife.BondFromPastLife> __state)
        {
            if (animal.RaceProps.EntityAnimal())
            {
                if (humanlike.health.hediffSet.HasHediff(HediffDefOf.VoidTouched))
                {
                    baseChance = float.PositiveInfinity;
                }
                else if (humanlike.Inhumanized())
                    baseChance *= 3f;
            }

            if (animal.TryGetComp(out CompBondsFromPastLife comp))
            {
                __state = comp.Bonds.FindAll((b) => b.PawnBondedTo == humanlike);
                if (__state.Count > 0)
                    baseChance *= 5f;
            }
        }

        // makes inhumanized pawns able to bond with chimeras
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher.Start();

            codeMatcher.MatchStartForward(CodeMatch.Calls(inhumanizedMethod));
            codeMatcher.ThrowIfInvalid("Could not find call to AnomalyUtility.Inhumanized");

            codeMatcher.MatchStartForward(CodeMatch.Branches());
            codeMatcher.ThrowIfInvalid("Could not find end of inhumanized check");
            int inhumanizedCheckEndPos = codeMatcher.Pos;

            codeMatcher.Advance(-1);
            codeMatcher.MatchStartBackwards(CodeMatch.Branches());
            codeMatcher.ThrowIfInvalid("Could not find start of inhumanized check");
            int inhumanizedCheckLength = inhumanizedCheckEndPos - codeMatcher.Pos;

            codeMatcher.Advance(1);
            CodeMatcher inhumanizedCheckMatcher = new CodeMatcher(codeMatcher.Instructions(inhumanizedCheckLength), generator);
            codeMatcher.RemoveInstructions(inhumanizedCheckLength);

            codeMatcher.MatchStartBackwards(new CodeMatch(OpCodes.Ret));
            codeMatcher.ThrowIfInvalid("Could not find return before psychopath check");
            codeMatcher.Advance(1);
            Label psychopathCheckLabel = codeMatcher.Labels[0];

            inhumanizedCheckMatcher.Start();
            inhumanizedCheckMatcher.CreateLabel(out Label inhumanizedCheckLabel);

            inhumanizedCheckMatcher.End();
            Label nextCheckLabel = (Label)inhumanizedCheckMatcher.Operand;
            inhumanizedCheckMatcher.Operand = psychopathCheckLabel;

            int psychopathCheckStartPos = codeMatcher.Pos;
            codeMatcher.MatchStartForward(CodeMatch.Branches());
            codeMatcher.ThrowIfInvalid("Could not find branch after psychopath check");

            Label unneededLabel = (Label)codeMatcher.Operand;
            codeMatcher.Operand = nextCheckLabel;
            codeMatcher.Opcode = OpCodes.Brfalse_S;
            codeMatcher.MatchStartForward(new CodeMatch((ci) => ci.labels.Contains(unneededLabel)));

            if (codeMatcher.IsValid)
            {
                //AnomalyAlliesMod.Logger.Message("Valid");
                codeMatcher.Labels.Remove(unneededLabel);
                codeMatcher.Advance(psychopathCheckStartPos - codeMatcher.Pos);
            }
            else
            {
                //AnomalyAlliesMod.Logger.Message("Invalid");
                codeMatcher.Start();
                codeMatcher.Advance(psychopathCheckStartPos);
            }

            inhumanizedCheckMatcher.Instructions().AddRange(
            [
                new CodeInstruction(OpCodes.Call, fieldProviderGetter),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, racePropsGetter),
                new CodeInstruction(OpCodes.Callvirt, entityAnimalMethod),
                new CodeInstruction(OpCodes.Ldind_I1),
                new CodeInstruction(OpCodes.Brtrue_S, nextCheckLabel),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret)
            ]);
            codeMatcher.Insert(inhumanizedCheckMatcher.InstructionEnumeration());

            codeMatcher.MatchStartBackwards(new CodeMatch(operand: psychopathCheckLabel));
            codeMatcher.ThrowIfInvalid("Could not find branch to psychopath check");
            codeMatcher.Operand = inhumanizedCheckLabel;

            return codeMatcher.InstructionEnumeration();
        }

        static void Postfix(Pawn humanlike, Pawn animal, List<CompBondsFromPastLife.BondFromPastLife> __state, bool __result)
        {
            if (__state is not null && __state.Count > 0 && __result)
            {
                Thought_Memory_ChimeraRemembersBond memory = (Thought_Memory_ChimeraRemembersBond)ThoughtMaker.MakeThought(AnAl_ThoughtDefOf.AnAl_ChimeraRemembersBond, null);
                foreach (CompBondsFromPastLife.BondFromPastLife bond in __state)
                {
                    humanlike.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(VanillaDefOf.BondedAnimalDied, bond.PreviousSelf);
                    memory.otherPawnPastSelves.Add(bond.PreviousSelf);
                }

                humanlike.needs.mood.thoughts.memories.TryGainMemory(memory, animal);
            }
        }
    }
}
