using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ART
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    internal static class MakeRecipeProducts_Patch
    {
        private static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver)
        {
            foreach (var result in __result)
            {
                ProcessThing(worker, billGiver, result);
                yield return result;
            }
        }

        private static void ProcessThing(Pawn worker, IBillGiver billGiver, Thing result)
        {
            if (billGiver is Thing workBench && CompThingInUse.things.TryGetValue(workBench, out var comp))
            {
                var statValues = new Dictionary<StatDef, StatBonus>();
                foreach (var useProps in comp.Props.useProperties)
                {
                    if (comp.UseIsEnabled(useProps))
                    {
                        if (useProps.resourceOnComplete != -1f)
                        {
                            if (worker.health.hediffSet.GetFirstHediffOfDef(useProps.hediff) is HediffResource hediffResource && hediffResource.CanUse(useProps, out _))
                            {
                                hediffResource.ChangeResourceAmount(useProps.resourceOnComplete);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (useProps.outputStatOffsets != null)
                        {
                            foreach (var statModifier in useProps.outputStatOffsets)
                            {
                                if (statValues.ContainsKey(statModifier.stat))
                                {
                                    statValues[statModifier.stat].statOffset += statModifier.value;
                                }
                                else
                                {
                                    statValues[statModifier.stat] = new StatBonus(statModifier.stat)
                                    {
                                        statOffset = statModifier.value
                                    };
                                }
                            }
                        }
                        if (useProps.outputStatFactors != null)
                        {
                            foreach (var statModifier in useProps.outputStatFactors)
                            {
                                if (statValues.ContainsKey(statModifier.stat))
                                {
                                    statValues[statModifier.stat].statFactor += statModifier.value;
                                }
                                else
                                {
                                    statValues[statModifier.stat] = new StatBonus(statModifier.stat)
                                    {
                                        statFactor = statModifier.value
                                    };
                                }
                            }
                        }
                    }
                }
                if (statValues.Any())
                {
                    var hediffResourceManager = ARTManager.Instance;
                    var statBonuses = new StatBonuses
                    {
                        statBonuses = new Dictionary<StatDef, StatBonus>()
                    };
                    foreach (var statValue in statValues)
                    {
                        var statBonus = new StatBonus
                        {
                            stat = statValue.Key,
                            statOffset = statValue.Value.statOffset,
                            statFactor = statValue.Value.statFactor
                        };
                        statBonuses.statBonuses[statValue.Key] = statBonus;
                    }

                    while (result.stackCount > result.def.stackLimit) // with ART we can get overstacked things when resource in use is active.
                                                                      // when the game does that, it creates new things in place of overstacked things and then we can't tract them.
                                                                      // this is a workaround to solve it.
                    {
                        var thing = result.SplitOff(result.def.stackLimit);
                        GenPlace.TryPlaceThing(thing, worker.Position, worker.Map, ThingPlaceMode.Near);
                        hediffResourceManager.thingsWithBonuses[thing] = statBonuses;
                    }
                    result.stackCount = result.def.stackLimit;
                    hediffResourceManager.thingsWithBonuses[result] = statBonuses;
                }
            }
        }
    }

    [HarmonyPatch(typeof(QualityUtility))]
    [HarmonyPatch("GenerateQualityCreatedByPawn")]
    [HarmonyPatch(new Type[]
    {
            typeof(Pawn),
            typeof(SkillDef)
    }, new ArgumentType[]
    {
            0,
            0
    })]
    public static class GenerateQualityCreatedByPawn_Patch
    {
        private static void Postfix(ref QualityCategory __result, Pawn pawn, SkillDef relevantSkill)
        {
            if (pawn.CurJobDef == JobDefOf.DoBill)
            {
                if (CompThingInUse.things.TryGetValue(pawn.CurJob.targetA.Thing, out var comp))
                {
                    foreach (var useProps in comp.Props.useProperties)
                    {
                        if (comp.UseIsEnabled(useProps) && useProps.increaseQuality != -1 && __result < useProps.increaseQualityCeiling)
                        {
                            if (pawn.health.hediffSet.GetFirstHediffOfDef(useProps.hediff) is HediffResource hediffResource && hediffResource.CanUse(useProps, out _))
                            {
                                int result = (int)__result + useProps.increaseQuality;
                                if (result > (int)QualityCategory.Legendary)
                                {
                                    result = (int)QualityCategory.Legendary;
                                }
                                if (result > (int)useProps.increaseQualityCeiling)
                                {
                                    result = (int)useProps.increaseQualityCeiling;
                                }
                                __result = (QualityCategory)result;
                            }
                        }
                    }
                }

                if (pawn.health?.hediffSet?.hediffs != null)
                {
                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff is HediffResource hediffResource && hediffResource.CurStage is HediffStageResource hediffStageResource && hediffStageResource.qualityAdjustProperties != null)
                        {
                            int qualityBonus = (int)Math.Truncate(hediffStageResource.qualityAdjustProperties.qualityOffset);
                            float decimalPart = hediffStageResource.qualityAdjustProperties.qualityOffset - qualityBonus;
                            if (Rand.Chance(decimalPart))
                            {
                                qualityBonus += 1;
                            }
                            int result = (int)__result + qualityBonus;
                            if (result > (int)QualityCategory.Legendary)
                            {
                                result = (int)QualityCategory.Legendary;
                            }
                            ARTLog.Message("Old result: " + __result);
                            __result = (QualityCategory)result;
                            ARTLog.Message("New result: " + __result);
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    public static class GetStatValue_Patch
    {
        private static void Postfix(Thing thing, StatDef stat, bool applyPostProcess, ref float __result)
        {
            if (ARTManager.Instance.thingsWithBonuses.TryGetValue(thing, out var statBonuses))
            {
                if (statBonuses.statBonuses.TryGetValue(stat, out var statBonus))
                {
                    __result += statBonus.statOffset;
                    __result *= statBonus.statFactor;
                }
            }
            if (CompThingInUse.things.TryGetValue(thing, out var comp) && comp.InUse(out var claimants))
            {
                IEnumerable<Pawn> users = null;
                var checkedPawnsResources = new Dictionary<Pawn, Dictionary<UseProps, HediffResource>>();
                foreach (var useProps in comp.Props.useProperties)
                {
                    if (!comp.UseIsEnabled(useProps))
                    {
                        continue;
                    }
                    if (useProps.statOffsets != null)
                    {
                        foreach (var statModifier in useProps.statOffsets)
                        {
                            if (statModifier.stat == stat)
                            {
                                if (users is null)
                                {
                                    users = comp.GetActualUsers(claimants);
                                }
                                foreach (var user in users)
                                {
                                    if (!checkedPawnsResources.TryGetValue(user, out var hediffResourceDict))
                                    {
                                        checkedPawnsResources[user] = hediffResourceDict = new Dictionary<UseProps, HediffResource>();
                                    }
                                    if (!hediffResourceDict.TryGetValue(useProps, out var hediffResource))
                                    {
                                        hediffResourceDict[useProps] = hediffResource = user.health.hediffSet.GetFirstHediffOfDef(useProps.hediff) as HediffResource;
                                    }
                                    if (hediffResource != null && hediffResource.CanUse(useProps, out _))
                                    {
                                        __result += statModifier.value;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (useProps.statFactors != null)
                    {
                        foreach (var statModifier in useProps.statFactors)
                        {
                            if (statModifier.stat == stat)
                            {
                                if (users is null)
                                {
                                    users = comp.GetActualUsers(claimants);
                                }
                                foreach (var user in users)
                                {
                                    if (!checkedPawnsResources.TryGetValue(user, out var hediffResourceDict))
                                    {
                                        checkedPawnsResources[user] = hediffResourceDict = new Dictionary<UseProps, HediffResource>();
                                    }
                                    if (!hediffResourceDict.TryGetValue(useProps, out var hediffResource))
                                    {
                                        hediffResourceDict[useProps] = hediffResource = user.health.hediffSet.GetFirstHediffOfDef(useProps.hediff) as HediffResource;
                                    }

                                    if (hediffResource != null && hediffResource.CanUse(useProps, out _))
                                    {
                                        __result *= statModifier.value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}