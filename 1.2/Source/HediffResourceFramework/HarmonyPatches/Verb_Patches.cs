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
                        var postUseDelayMultipliers = new List<float>();
                        var postUseDelay = new List<int>();

                        foreach (var option in verbProps.resourceSettings)
                        {
                            var resourseSettings = comp.Props.resourceSettings.FirstOrDefault(x => x.hediff == option.hediff);
                            if (resourseSettings != null)
                            {
                                postUseDelayMultipliers.Add(resourseSettings.postUseDelayMultiplier);
                                postUseDelay.Add(option.postUseDelay);
                                Log.Message("Adding postUseDelayMultiplier: " + resourseSettings.postUseDelayMultiplier);
                                Log.Message("Adding postUseDelay: " + option.postUseDelay);
                            }

                            var hediffResource = HediffResourceUtils.AdjustResourceAmount(__instance.CasterPawn, option.hediff, option.resourcePerUse, option.addHediffIfMissing);
                            if (option.postUseDelay != 0)
                            {
                                var newDelay = (int)(option.postUseDelay * resourseSettings.postUseDelayMultiplier);
                                if (hediffResource != null && hediffResource.CanHaveDelay(newDelay))
                                {
                                    hediffResource.AddDelay(newDelay);
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