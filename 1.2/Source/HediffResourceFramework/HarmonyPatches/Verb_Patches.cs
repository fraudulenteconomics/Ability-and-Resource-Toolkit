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
        private static void Prefix(Verb __instance)
        {
            Log.Message("Patch_TryCastNextBurstShot - Postfix - if (__instance.Available() && __instance.CasterIsPawn && __instance.EquipmentSource != null) - 1", true);
            if (__instance.Available() && __instance.CasterIsPawn && __instance.EquipmentSource != null)
            {
                Log.Message("Patch_TryCastNextBurstShot - Postfix - var comp = __instance.EquipmentSource.GetComp<CompWeaponAdjustHediffs>(); - 2", true);
                var comp = __instance.EquipmentSource.GetComp<CompAdjustHediffs>();
                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (comp != null) - 3", true);
                if (comp != null)
                {
                    if (comp.postUseDelayTicks is null)
                    {
                        comp.postUseDelayTicks = new Dictionary<Verb, VerbDisable>();
                    }

                    var verbProps = __instance.verbProps as VerbResourceProps;
                    Log.Message("Patch_TryCastNextBurstShot - Postfix - if (verbProps != null && verbProps.resourceSettings != null) - 7", true);
                    if (verbProps != null && verbProps.resourceSettings != null)
                    {
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var postUseDelayMultipliers = new List<float>(); - 8", true);
                        var postUseDelayMultipliers = new List<float>();
                        Log.Message("Patch_TryCastNextBurstShot - Postfix - var postUseDelay = new List<int>(); - 9", true);
                        var postUseDelay = new List<int>();

                        Log.Message("Patch_TryCastNextBurstShot - Postfix - foreach (var option in verbProps.resourceSettings) - 10", true);
                        foreach (var option in verbProps.resourceSettings)
                        {
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - var resourseSettings = comp.Props.resourceSettings.FirstOrDefault(x => x.hediff == option.hediff); - 11", true);
                            var resourseSettings = comp.Props.resourceSettings.FirstOrDefault(x => x.hediff == option.hediff);
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - if (resourseSettings != null) - 12", true);
                            if (resourseSettings != null)
                            {
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - postUseDelayMultipliers.Add(resourseSettings.postUseDelayMultiplier); - 13", true);
                                postUseDelayMultipliers.Add(resourseSettings.postUseDelayMultiplier);
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - postUseDelay.Add(option.postUseDelay); - 14", true);
                                postUseDelay.Add(option.postUseDelay);
                            }
                            Log.Message("Should adjust: " + option.hediff + " - " + option.resourcePerUse);
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing); - 16", true);
                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            Log.Message("Patch_TryCastNextBurstShot - Postfix - if (option.postUseDelay != 0) - 17", true);
                            if (option.postUseDelay != 0)
                            {
                                Log.Message("Patch_TryCastNextBurstShot - Postfix - if (hediffResource != null && hediffResource.CanHaveDelay(option.postUseDelay)) - 18", true);
                                if (hediffResource != null && hediffResource.CanHaveDelay(option.postUseDelay))
                                {
                                    Log.Message("Patch_TryCastNextBurstShot - Postfix - hediffResource.AddDelay(option.postUseDelay); - 19", true);
                                    hediffResource.AddDelay(option.postUseDelay);
                                }
                            }
                        }

                        comp.postUseDelayTicks[__instance] = new VerbDisable((int)((Find.TickManager.TicksGame + (int)postUseDelay.Average()) * postUseDelayMultipliers.Average()), comp.Props.disablePostUse);
                    }
                }
            }
        }
    }
}