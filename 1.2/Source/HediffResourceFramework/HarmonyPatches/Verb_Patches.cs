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

	[HarmonyPatch(typeof(Verb), "IsStillUsableBy")]
	public static class Patch_IsStillUsableBy
	{
		private static void Postfix(ref bool __result, Verb __instance, Pawn pawn)
		{
			if (__result)
			{
				__result = HediffResourceUtils.IsUsableBy(__instance, out string disableReason);
			}
		}
	}
    
	[HarmonyPatch(typeof(Verb), "Available")]
	public static class Patch_Available
	{
		private static void Postfix(ref bool __result, Verb __instance)
		{
			if (__result)
			{
				__result = HediffResourceUtils.IsUsableBy(__instance, out string disableReason);
            }
        }
	}

    [HarmonyPatch(typeof(Verb), "TryCastNextBurstShot")]
    public static class Patch_TryCastNextBurstShot
    {
        private static void Prefix(Verb __instance, out bool __state)
        {
            if (__instance.Available())
            {
                __state = true;
            }
            else
            {
                __state = false;
            }
        }

        private static void Postfix(Verb __instance, bool __state)
        {
            if (__state && __instance.CasterIsPawn && __instance.EquipmentSource != null)
            {
                var comp = __instance.EquipmentSource.GetComp<CompAdjustHediffs>();
                if (comp != null)
                {
                    if (comp.postUseDelayTicks is null)
                    {
                        comp.postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                    }

                    var verbProps = __instance.verbProps as VerbResourceProps;
                    if (verbProps != null && verbProps.resourceSettings != null)
                    {
                        var verbPostUseDelay = new List<int>();
                        var verbPostUseDelayMultipliers = new List<float>();

                        var hediffPostUse = new Dictionary<HediffResource, List<int>>();
                        var hediffPostUseDelayMultipliers = new Dictionary<HediffResource, List<float>>();

                        foreach (var option in verbProps.resourceSettings)
                        {
                            var compResourseSettings = comp.Props.resourceSettings.FirstOrDefault(x => x.hediff == option.hediff);
                            if (compResourseSettings != null)
                            {
                                if (option.postUseDelay != 0)
                                {
                                    verbPostUseDelay.Add(option.postUseDelay);
                                    if (compResourseSettings.postUseDelayMultiplier != 1)
                                    {
                                        verbPostUseDelayMultipliers.Add(compResourseSettings.postUseDelayMultiplier);
                                    }
                                }
                                Log.Message($"Adding postUseDelayMultiplier from comp {comp}: {compResourseSettings.postUseDelayMultiplier}");
                                Log.Message("Adding postUseDelay: " + option.postUseDelay);
                            }

                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            if (hediffResource != null && option.postUseDelay != 0)
                            {
                                if (hediffPostUse.ContainsKey(hediffResource))
                                {
                                    hediffPostUse[hediffResource].Add(option.postUseDelay);
                                }
                                else
                                {
                                    hediffPostUse[hediffResource] = new List<int> { option.postUseDelay };
                                }
                                if (compResourseSettings.postUseDelayMultiplier != 1)
                                {
                                    if (hediffPostUseDelayMultipliers.ContainsKey(hediffResource))
                                    {
                                        hediffPostUseDelayMultipliers[hediffResource].Add(compResourseSettings.postUseDelayMultiplier);
                                    }
                                    else
                                    {
                                        hediffPostUseDelayMultipliers[hediffResource] = new List<float> { compResourseSettings.postUseDelayMultiplier };
                                    }
                                }
                            }
                        }

                        comp.postUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + verbPostUseDelay.Average()) * verbPostUseDelayMultipliers.Average()), comp.Props.disablePostUse);
                        Log.Message($"Adding delay {comp.postUseDelayTicks[__instance]} for verb: {__instance}");
                        foreach (var hediffData in hediffPostUse)
                        {
                            if (hediffPostUse.TryGetValue(hediffData.Key, out List<int> hediffPostUseList))
                            {
                                int newDelayTicks;
                                if (hediffPostUseDelayMultipliers.TryGetValue(hediffData.Key, out List<float> hediffPostUseMultipliers))
                                {
                                    newDelayTicks = (int)(hediffPostUseList.Average() * hediffPostUseMultipliers.Average());
                                    Log.Message($"newDelayTicks: {newDelayTicks}, with multipliers for {hediffData.Key}");
                                }
                                else
                                {
                                    newDelayTicks = (int)(hediffPostUseList.Average());
                                    Log.Message($"newDelayTicks: {newDelayTicks} for {hediffData.Key}");
                                }
                                if (hediffData.Key.CanHaveDelay(newDelayTicks))
                                {
                                    hediffData.Key.AddDelay(newDelayTicks);
                                    Log.Message($"Adding delay {newDelayTicks} for {hediffData.Key}");

                                }
                                else
                                {
                                    Log.Message($"{hediffData.Key} can't have new delay in {newDelayTicks}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}