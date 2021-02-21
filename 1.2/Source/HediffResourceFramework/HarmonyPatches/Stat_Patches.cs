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
    [HarmonyPatch(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
    public static class GetStatValue_Patch
    {
        private static void Postfix(Thing thing, StatDef stat, bool applyPostProcess, ref float __result)
        {
            if (CompFacilityInUse_StatBoosters.thingBoosters.TryGetValue(thing, out var comp) && comp.InUse(out var claimants))
            {
                IEnumerable<Pawn> users = null;
                Dictionary<Pawn, HediffResource> checkedPawnsResources = new Dictionary<Pawn, HediffResource>();
                var oldResult = __result;
                foreach (var statBooster in comp.Props.statBoosters)
                {
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
                                        Log.Message($"1 Due to an user {user} with {statBooster.hediff} - {hediffResource}, {thing} is gaining a bonus to {stat}!");
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
                                        Log.Message($"2 Due to an user {user} with {statBooster.hediff} - {hediffResource}, {thing} is gaining a bonus to {stat}!");
                                        __result *= statModifier.value;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (__result != oldResult && (users?.Any() ?? false))
                {
                    foreach (var user in users)
                    {
                        Log.Message($"{thing} is giving stat boosts {stat} - {__result} to {user}");
                    }
                }
            }
        }
    }
}
