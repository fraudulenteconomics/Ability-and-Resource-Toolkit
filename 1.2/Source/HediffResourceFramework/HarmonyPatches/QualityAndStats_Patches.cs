using HarmonyLib;
using MVCF.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HediffResourceFramework
{
    [HarmonyPatch(typeof(GenRecipe), "MakeRecipeProducts")]
    internal static class MakeRecipeProducts_Patch
    {
        private static void Postfix(ref IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver)
        {
            if (billGiver is Thing workBench && CompFacilityInUse_StatBoosters.thingBoosters.TryGetValue(workBench, out var comp))
            {
                var list = __result.ToList();
                Dictionary<StatDef, StatBonus> statValues = new Dictionary<StatDef, StatBonus>();
                foreach (var statBooster in comp.Props.statBoosters)
                {
                    if (comp.StatBoosterIsEnabled(statBooster))
                    {
                        if (statBooster.outputStatOffsets != null)
                        {
                            foreach (var statModifier in statBooster.outputStatOffsets)
                            {
                                if (statValues.ContainsKey(statModifier.stat))
                                {
                                    statValues[statModifier.stat].statOffset = statModifier.value;
                                }
                                else
                                {
                                    statValues[statModifier.stat] = new StatBonus(statModifier.stat);
                                    statValues[statModifier.stat].statOffset = statModifier.value;
                                }
                            }
                        }
                        if (statBooster.outputStatFactors != null)
                        {
                            foreach (var statModifier in statBooster.outputStatFactors)
                            {
                                if (statValues.ContainsKey(statModifier.stat))
                                {
                                    statValues[statModifier.stat].statFactor = statModifier.value;
                                }
                                else
                                {
                                    statValues[statModifier.stat] = new StatBonus(statModifier.stat);
                                    statValues[statModifier.stat].statFactor = statModifier.value;
                                }
                            }
                        }
                    }
                }
                if (statValues.Any())
                {
                    var hediffResourceManager = HediffResourceUtils.HediffResourceManager;
                    var statBonuses = new StatBonuses();
                    statBonuses.statBonuses = new Dictionary<StatDef, StatBonus>();
                    foreach (var statValue in statValues)
                    {
                        var statBonus = new StatBonus();
                        statBonus.stat = statValue.Key;
                        statBonus.statOffset = statValue.Value.statOffset;
                        statBonus.statFactor = statValue.Value.statFactor;
                        statBonuses.statBonuses[statValue.Key] = statBonus;
                    }

                    for (var i = 0; i < list.Count; i++)
                    {
                        while (list[i].stackCount > list[i].def.stackLimit) // with HRF we can get overstacked things when resource in use is active. 
                                                                            // when the game does that, it creates new things in place of overstacked things and then we can't tract them. 
                                                                            // this is a workaround to solve it.
                        {
                            var thing = list[i].SplitOff(list[i].def.stackLimit);
                            thing.stackCount = list[i].def.stackLimit;
                            GenPlace.TryPlaceThing(thing, worker.Position, worker.Map, ThingPlaceMode.Near);
                            hediffResourceManager.thingsWithBonuses[thing] = statBonuses;
                        }
                        list[i].stackCount = list[i].def.stackLimit;
                        hediffResourceManager.thingsWithBonuses[list[i]] = statBonuses;
                    }
                }
                __result = list;
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
            if (pawn.CurJobDef == JobDefOf.DoBill && CompFacilityInUse_StatBoosters.thingBoosters.TryGetValue(pawn.CurJob.targetA.Thing, out var comp))
            {
                foreach (var statBooster in comp.Props.statBoosters)
                {
                    if (comp.StatBoosterIsEnabled(statBooster) && statBooster.increaseQuality != -1 && __result < statBooster.increaseQualityCeiling)
                    {
                        var hediffResource = pawn.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                        if (hediffResource != null && hediffResource.ResourceAmount >= statBooster.resourcePerSecond)
                        {
                            var result = (int)__result + (int)statBooster.increaseQuality;
                            if (result > (int)QualityCategory.Legendary)
                            {
                                result = (int)QualityCategory.Legendary;
                            }
                            if (result > (int)statBooster.increaseQualityCeiling)
                            {
                                result = (int)statBooster.increaseQualityCeiling;
                            }
                            __result = (QualityCategory)result;
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
            if (HediffResourceUtils.HediffResourceManager.thingsWithBonuses.TryGetValue(thing, out var statBonuses))
            {
                if (statBonuses.statBonuses.TryGetValue(stat, out StatBonus statBonus))
                {
                    __result += statBonus.statOffset;
                    __result *= statBonus.statFactor;
                }
            }
            if (CompFacilityInUse_StatBoosters.thingBoosters.TryGetValue(thing, out var comp) && comp.InUse(out var claimants))
            {
                IEnumerable<Pawn> users = null;
                Dictionary<Pawn, HediffResource> checkedPawnsResources = new Dictionary<Pawn, HediffResource>();
                var oldResult = __result;
                foreach (var statBooster in comp.Props.statBoosters)
                {
                    if (!comp.StatBoosterIsEnabled(statBooster))
                    {
                        continue;
                    }
                    if (statBooster.statOffsets != null)
                    {
                        foreach (var statModifier in statBooster.statOffsets)
                        {
                            if (statModifier.stat == stat)
                            {
                                if (users is null)
                                {
                                    users = comp.GetActualUsers(claimants);
                                }
                                foreach (var user in users)
                                {
                                    if (!checkedPawnsResources.TryGetValue(user, out var hediffResource))
                                    {
                                        hediffResource = user.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                                        checkedPawnsResources[user] = hediffResource;
                                    }
                                    if (hediffResource != null && statBooster.hediff == hediffResource.def && hediffResource.ResourceAmount >= -statBooster.resourcePerSecond)
                                    {
                                        //Log.Message($"1 Due to an user {user} with {statBooster.hediff} - {hediffResource}, {thing} is gaining a bonus to {stat}!");
                                        __result += statModifier.value;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (statBooster.statFactors != null)
                    {
                        foreach (var statModifier in statBooster.statFactors)
                        {
                            if (statModifier.stat == stat)
                            {
                                if (users is null)
                                {
                                    users = comp.GetActualUsers(claimants);
                                }
                                foreach (var user in users)
                                {
                                    if (!checkedPawnsResources.TryGetValue(user, out var hediffResource))
                                    {
                                        hediffResource = user.health.hediffSet.GetFirstHediffOfDef(statBooster.hediff) as HediffResource;
                                        checkedPawnsResources[user] = hediffResource;
                                    }
                                    if (hediffResource != null && statBooster.hediff == hediffResource.def && hediffResource.ResourceAmount >= -statBooster.resourcePerSecond)
                                    {
                                        //Log.Message($"2 Due to an user {user} with {statBooster.hediff} - {hediffResource}, {thing} is gaining a bonus to {stat}!");
                                        __result *= statModifier.value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                //if (__result != oldResult && (users?.Any() ?? false))
                //{
                //    foreach (var user in users)
                //    {
                //        Log.Message($"{thing} is giving stat boosts {stat} - {__result} to {user}");
                //    }
                //}
            }
        }
    }
}
